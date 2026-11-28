<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getAttempt, submitAttemptAnswer } from '@/api/attempts'
import { ApiAbortError, ApiError } from '@/api/http'
import AppButton from '@/components/AppButton.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import type { AttemptQuestionResponse, AttemptResponse } from '@/types/attempts'
import { AttemptKindValue, AttemptSourceValue, AttemptStatusValue } from '@/types/attempts'
import { parsePositiveInt } from '@/utils/routeQuery'
import { getSessionGeneration } from '@/api/session'

const route = useRoute()
const router = useRouter()
const attempt = ref<AttemptResponse | null>(null)
const currentIndex = ref(0)
const selectedOption = ref<number | null>(null)
const loading = ref(false)
const submitting = ref(false)
const synchronizing = ref(false)
const uncertainAnswer = ref(false)
const needsManualCheck = ref(false)
const error = ref('')
const syncMessage = ref('')
const now = ref(Date.now())
let controller: AbortController | null = null
let requestId = 0
let stateVersion = 0
let timer: ReturnType<typeof setInterval> | undefined
let timeoutRefreshes = 0
let loadPromise: Promise<void> | null = null
let destroyed = false

const attemptId = computed(() => parsePositiveInt(route.params.id))
const currentQuestion = computed<AttemptQuestionResponse | null>(
  () => attempt.value?.questions[currentIndex.value] ?? null,
)
const isExam = computed(() => attempt.value?.kind === AttemptKindValue.Exam)
const isFinished = computed(
  () =>
    attempt.value?.status !== undefined && attempt.value.status !== AttemptStatusValue.InProgress,
)
const answeredCount = computed(
  () => attempt.value?.questions.filter((question) => question.answeredAt).length ?? 0,
)
const deadline = computed(() => {
  if (!attempt.value?.timeLimitSeconds) return null
  return Date.parse(attempt.value.startedAt) + attempt.value.timeLimitSeconds * 1000
})
const remainingSeconds = computed(() =>
  deadline.value ? Math.max(0, Math.ceil((deadline.value - now.value) / 1000)) : null,
)
const formattedRemaining = computed(() => {
  if (remainingSeconds.value === null) return ''
  const minutes = Math.floor(remainingSeconds.value / 60)
  return `${minutes}:${String(remainingSeconds.value % 60).padStart(2, '0')}`
})
const canAnswer = computed(
  () =>
    !!attempt.value &&
    !isFinished.value &&
    !submitting.value &&
    !synchronizing.value &&
    !uncertainAnswer.value &&
    !!currentQuestion.value &&
    remainingSeconds.value !== 0,
)

function statusLabel(): string {
  if (!attempt.value) return ''
  if (attempt.value.status === AttemptStatusValue.Completed) return 'Попытка завершена'
  if (attempt.value.status === AttemptStatusValue.Failed) return 'Экзамен не сдан'
  if (attempt.value.status === AttemptStatusValue.TimedOut) return 'Время экзамена истекло'
  return ''
}

function syncSelection(): void {
  selectedOption.value = currentQuestion.value?.selectedOptionPosition ?? null
}

function applyAttempt(response: AttemptResponse, preferredQuestionId?: number): void {
  const previousQuestionId = preferredQuestionId ?? currentQuestion.value?.id
  attempt.value = response
  const preferredIndex = previousQuestionId
    ? response.questions.findIndex((question) => question.id === previousQuestionId)
    : -1
  if (preferredIndex >= 0) currentIndex.value = preferredIndex
  else if (response.status === AttemptStatusValue.InProgress) selectFirstUnanswered(response)
  else currentIndex.value = Math.min(currentIndex.value, Math.max(0, response.questions.length - 1))
  const selected = response.questions[currentIndex.value]
  if (selected?.answeredAt || selectedOption.value === null || !selected) syncSelection()
  now.value = Date.now()
  updateTimer()
}

function selectFirstUnanswered(response: AttemptResponse): void {
  const index = response.questions.findIndex((question) => !question.answeredAt)
  currentIndex.value = index >= 0 ? index : 0
  syncSelection()
}

async function load(background = false): Promise<void> {
  if (loadPromise) return loadPromise
  const id = ++requestId
  const version = stateVersion
  const value = attemptId.value
  const operationSession = getSessionGeneration()
  if (!background) {
    attempt.value = null
    error.value = ''
  }
  if (!value) {
    loading.value = false
    error.value = 'Некорректный идентификатор попытки.'
    return
  }
  const currentController = new AbortController()
  controller = currentController
  if (!background) loading.value = true
  loadPromise = (async () => {
    try {
      const response = await getAttempt(value, currentController.signal)
      if (
        destroyed ||
        id !== requestId ||
        version !== stateVersion ||
        operationSession !== getSessionGeneration() ||
        value !== attemptId.value
      )
        return
      applyAttempt(response)
      timeoutRefreshes = 0
      syncMessage.value = ''
    } catch (e) {
      if (
        e instanceof ApiAbortError ||
        id !== requestId ||
        version !== stateVersion ||
        operationSession !== getSessionGeneration()
      )
        return
      error.value =
        e instanceof ApiError && e.status === 404
          ? 'Попытка не найдена.'
          : e instanceof ApiError
            ? e.message
            : 'Не удалось загрузить попытку.'
    } finally {
      if (id === requestId && !background) loading.value = false
    }
  })().finally(() => {
    loadPromise = null
  })
  return loadPromise
}

function goTo(index: number): void {
  if (!attempt.value || index < 0 || index >= attempt.value.questions.length) return
  currentIndex.value = index
  syncSelection()
}

async function refreshAfterUncertain(): Promise<boolean> {
  const value = attemptId.value
  if (!value || synchronizing.value || submitting.value) return false
  synchronizing.value = true
  const operationSession = getSessionGeneration()
  try {
    const response = await getAttempt(value)
    if (destroyed || value !== attemptId.value || operationSession !== getSessionGeneration())
      return false
    const question = response.questions.find((item) => item.id === pendingQuestionId)
    applyAttempt(response, currentQuestion.value?.id ?? pendingQuestionId ?? undefined)
    if (
      (pendingQuestionId !== null && question?.answeredAt) ||
      response.status !== AttemptStatusValue.InProgress
    ) {
      uncertainAnswer.value = false
      needsManualCheck.value = false
      if (question?.answeredAt) pendingQuestionId = null
    } else {
      if (pendingQuestionId === null) {
        uncertainAnswer.value = true
        needsManualCheck.value = true
      } else {
        uncertainAnswer.value = false
        needsManualCheck.value = false
      }
    }
    return true
  } finally {
    synchronizing.value = false
  }
}

let pendingQuestionId: number | null = null

async function submit(): Promise<void> {
  const value = attemptId.value
  const question = currentQuestion.value
  if (
    !value ||
    !question ||
    question.answeredAt ||
    selectedOption.value === null ||
    !canAnswer.value
  )
    return
  submitting.value = true
  stateVersion += 1
  syncMessage.value = ''
  const questionId = question.id
  const submittedPosition = selectedOption.value
  pendingQuestionId = questionId
  const operationSession = getSessionGeneration()
  try {
    const response = await submitAttemptAnswer(value, questionId, {
      selectedOptionPosition: submittedPosition,
    })
    if (value !== attemptId.value || operationSession !== getSessionGeneration() || destroyed)
      return
    applyAttempt(response, questionId)
    pendingQuestionId = null
    uncertainAnswer.value = false
    needsManualCheck.value = false
  } catch (e) {
    if (e instanceof ApiError && (e.status === 0 || e.status === 409 || e.status >= 500)) {
      uncertainAnswer.value = true
      needsManualCheck.value = true
      syncMessage.value = 'Проверяем состояние ответа на сервере…'
      try {
        if (await refreshAfterUncertain()) syncMessage.value = 'Состояние попытки обновлено.'
      } catch {
        syncMessage.value = 'Не удалось уточнить состояние ответа. Повторите проверку позже.'
      }
    } else if (!(e instanceof ApiAbortError)) {
      error.value = e instanceof ApiError ? e.message : 'Не удалось отправить ответ.'
    }
  } finally {
    submitting.value = false
  }
}

function updateTimer(): void {
  stopTimer()
  if (
    attempt.value?.kind !== AttemptKindValue.Exam ||
    attempt.value.status !== AttemptStatusValue.InProgress
  )
    return
  timer = setInterval(() => {
    now.value = Date.now()
    if (
      remainingSeconds.value === 0 &&
      !synchronizing.value &&
      !submitting.value &&
      timeoutRefreshes < 3
    ) {
      timeoutRefreshes += 1
      void refreshAfterUncertain().catch(() => {
        needsManualCheck.value = true
        uncertainAnswer.value = true
        syncMessage.value = 'Время истекло. Проверьте состояние попытки на сервере.'
      })
    } else if (
      remainingSeconds.value === 0 &&
      attempt.value?.status === AttemptStatusValue.InProgress
    ) {
      needsManualCheck.value = true
      uncertainAnswer.value = true
      syncMessage.value = 'Время истекло. Проверьте состояние попытки на сервере.'
    }
  }, 1000)
}

function stopTimer(): void {
  if (timer) clearInterval(timer)
  timer = undefined
}

function handleVisibility(): void {
  now.value = Date.now()
  if (
    document.visibilityState === 'visible' &&
    attempt.value?.status === AttemptStatusValue.InProgress &&
    !submitting.value &&
    !synchronizing.value
  )
    void load(true)
}

watch(attemptId, () => {
  requestId += 1
  stateVersion += 1
  controller?.abort()
  attempt.value = null
  now.value = Date.now()
  stopTimer()
  void nextTick(() => {
    if (loadPromise) {
      void loadPromise.finally(() => {
        if (!destroyed) void load()
      })
    } else {
      void load()
    }
  })
})
onMounted(() => {
  void load()
  document.addEventListener('visibilitychange', handleVisibility)
})
onBeforeUnmount(() => {
  destroyed = true
  requestId += 1
  stateVersion += 1
  controller?.abort()
  stopTimer()
  document.removeEventListener('visibilitychange', handleVisibility)
})
</script>

<template>
  <section class="space-y-6">
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p class="text-sm font-semibold uppercase tracking-wider text-brand">
          {{ isExam ? 'Экзамен' : 'Тренировка' }}
        </p>
        <h1 class="mt-2 text-3xl font-bold text-ink">Прохождение</h1>
        <p v-if="attempt" class="mt-2 text-sm text-muted">
          {{
            attempt.source === AttemptSourceValue.Prepared
              ? 'Подготовленный вариант'
              : 'Случайная подборка'
          }}
        </p>
      </div>
      <div
        v-if="isExam && !isFinished && formattedRemaining"
        class="rounded-app bg-secondary-soft px-4 py-3 text-lg font-bold text-secondary"
        aria-live="polite"
      >
        Осталось {{ formattedRemaining }}
      </div>
    </div>
    <LoadingState v-if="loading" />
    <ErrorState v-else-if="error" :message="error" :retry="load" />
    <template v-else-if="attempt">
      <div class="rounded-app border border-line bg-surface p-4 shadow-sm">
        <div class="flex flex-wrap justify-between gap-2 text-sm font-semibold text-ink">
          <span>Вопрос {{ currentIndex + 1 }} из {{ attempt.questionCount }}</span
          ><span>Отвечено: {{ answeredCount }}/{{ attempt.questionCount }}</span
          ><span
            >Правильных: {{ attempt.correctAnswers }} · Ошибок: {{ attempt.wrongAnswers }}</span
          >
        </div>
        <div class="mt-3 h-2 overflow-hidden rounded-full bg-canvas">
          <div
            class="h-full bg-brand transition-all"
            :style="{ width: `${(answeredCount / attempt.questionCount) * 100}%` }"
          />
        </div>
      </div>
      <div class="flex flex-wrap gap-2" aria-label="Навигация по вопросам">
        <button
          v-for="(question, index) in attempt.questions"
          :key="question.id"
          type="button"
          class="h-9 w-9 rounded-full border text-sm font-semibold focus-visible:outline-2 focus-visible:outline-brand"
          :class="
            index === currentIndex
              ? 'border-brand bg-brand text-white'
              : question.answeredAt
                ? 'border-success bg-success-soft text-success'
                : 'border-line bg-surface text-ink'
          "
          :disabled="submitting || synchronizing"
          @click="goTo(index)"
        >
          {{ index + 1 }}
        </button>
      </div>
      <article
        v-if="currentQuestion"
        class="rounded-app border border-line bg-surface p-5 shadow-sm sm:p-7"
      >
        <h2 class="text-xl font-semibold leading-8 text-ink">{{ currentQuestion.questionText }}</h2>
        <fieldset class="mt-6 space-y-3" :disabled="!canAnswer">
          <legend class="sr-only">Варианты ответа</legend>
          <label
            v-for="option in currentQuestion.options"
            :key="option.position"
            class="flex cursor-pointer gap-3 rounded-app border p-4 transition"
            :class="
              option.isCorrect === true
                ? 'border-success bg-success-soft'
                : option.isCorrect === false &&
                    option.position === currentQuestion.selectedOptionPosition
                  ? 'border-danger bg-danger-soft'
                  : option.position === currentQuestion.selectedOptionPosition
                    ? 'border-brand bg-brand-soft'
                    : 'border-line bg-canvas'
            "
          >
            <input
              v-model="selectedOption"
              class="mt-1"
              type="radio"
              :name="`attempt-question-${currentQuestion.id}`"
              :value="option.position"
              :disabled="!!currentQuestion.answeredAt"
            />
            <span class="min-w-0 text-ink"
              ><span class="font-semibold">{{ option.position }}.</span> {{ option.text
              }}<span
                v-if="
                  currentQuestion.answeredAt &&
                  option.position === currentQuestion.selectedOptionPosition
                "
                class="mt-1 block text-sm font-semibold"
                >Ваш ответ</span
              ><span
                v-if="currentQuestion.answeredAt && option.isCorrect === true"
                class="mt-1 block text-sm font-semibold text-success"
                >Правильный ответ</span
              ></span
            >
          </label>
        </fieldset>
        <p
          v-if="currentQuestion.answeredAt"
          class="mt-4 text-sm font-semibold"
          :class="currentQuestion.isCorrect ? 'text-success' : 'text-danger'"
        >
          {{ currentQuestion.isCorrect ? 'Ответ правильный.' : 'Ответ неправильный.' }}
        </p>
        <p v-if="syncMessage" class="mt-4 text-sm text-warning">{{ syncMessage }}</p>
        <div class="mt-6 flex flex-wrap justify-between gap-3">
          <AppButton
            variant="secondary"
            :disabled="currentIndex === 0 || submitting || synchronizing"
            @click="goTo(currentIndex - 1)"
            >Назад</AppButton
          ><AppButton
            v-if="!currentQuestion.answeredAt"
            :disabled="!canAnswer || selectedOption === null"
            :loading="submitting"
            @click="submit"
            >Ответить</AppButton
          ><AppButton
            v-else
            :disabled="currentIndex >= attempt.questions.length - 1 || submitting || synchronizing"
            @click="goTo(currentIndex + 1)"
            >Следующий вопрос</AppButton
          >
        </div>
      </article>
      <div
        v-if="needsManualCheck"
        class="rounded-app border border-warning bg-warning-soft p-4 text-sm text-warning"
      >
        <p>Состояние последнего ответа не удалось подтвердить автоматически.</p>
        <AppButton
          class="mt-3"
          variant="secondary"
          :loading="synchronizing"
          @click="refreshAfterUncertain"
          >Проверить состояние попытки</AppButton
        >
      </div>
      <div
        v-if="isFinished"
        class="rounded-app border p-4"
        :class="
          attempt.status === AttemptStatusValue.Completed
            ? 'border-success bg-success-soft text-success'
            : 'border-danger bg-danger-soft text-danger'
        "
      >
        <p class="font-semibold">{{ statusLabel() }}</p>
        <AppButton
          class="mt-3"
          variant="secondary"
          @click="router.push({ name: 'cashier-attempt-result', params: { id: attempt.id } })"
          >Посмотреть результат</AppButton
        >
      </div>
    </template>
  </section>
</template>
