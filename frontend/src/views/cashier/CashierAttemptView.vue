<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getAttempt, submitAttemptAnswer } from '@/api/attempts'
import { ApiAbortError, ApiError } from '@/api/http'
import AppButton from '@/components/AppButton.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import type { AttemptQuestionResponse, AttemptResponse } from '@/types/attempts'
import { AttemptKindValue, AttemptStatusValue } from '@/types/attempts'
import { parsePositiveInt } from '@/utils/routeQuery'

const route = useRoute()
const router = useRouter()
const attempt = ref<AttemptResponse | null>(null)
const currentIndex = ref(0)
const selectedOption = ref<number | null>(null)
const loading = ref(false)
const submitting = ref(false)
const error = ref('')
const syncMessage = ref('')
const now = ref(Date.now())
let controller: AbortController | null = null
let requestId = 0
let stateVersion = 0
let timer: ReturnType<typeof setInterval> | undefined
let timeoutRefreshes = 0

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

function selectFirstUnanswered(response: AttemptResponse): void {
  const index = response.questions.findIndex((question) => !question.answeredAt)
  currentIndex.value = index >= 0 ? index : 0
  syncSelection()
}

async function load(): Promise<void> {
  controller?.abort()
  const id = ++requestId
  const version = stateVersion
  const value = attemptId.value
  attempt.value = null
  error.value = ''
  if (!value) {
    loading.value = false
    error.value = 'Некорректный идентификатор попытки.'
    return
  }
  controller = new AbortController()
  loading.value = true
  try {
    const response = await getAttempt(value, controller.signal)
    if (id !== requestId || version !== stateVersion) return
    attempt.value = response
    timeoutRefreshes = 0
    selectFirstUnanswered(response)
    syncMessage.value = ''
  } catch (e) {
    if (e instanceof ApiAbortError || id !== requestId) return
    error.value =
      e instanceof ApiError && e.status === 404
        ? 'Попытка не найдена.'
        : e instanceof ApiError
          ? e.message
          : 'Не удалось загрузить попытку.'
  } finally {
    if (id === requestId) loading.value = false
  }
}

function goTo(index: number): void {
  if (!attempt.value || index < 0 || index >= attempt.value.questions.length) return
  currentIndex.value = index
  syncSelection()
}

async function refreshAfterUncertain(): Promise<void> {
  const value = attemptId.value
  if (!value) return
  const response = await getAttempt(value)
  if (value !== attemptId.value) return
  attempt.value = response
  const questionIndex = response.questions.findIndex(
    (question) => question.id === currentQuestion.value?.id,
  )
  if (questionIndex >= 0) currentIndex.value = questionIndex
  syncSelection()
}

async function submit(): Promise<void> {
  const value = attemptId.value
  const question = currentQuestion.value
  if (!value || !question || selectedOption.value === null || !canAnswer.value) return
  submitting.value = true
  stateVersion += 1
  syncMessage.value = ''
  const questionId = question.id
  try {
    const response = await submitAttemptAnswer(value, questionId, {
      selectedOptionPosition: selectedOption.value,
    })
    if (value !== attemptId.value || questionId !== currentQuestion.value?.id) return
    attempt.value = response
    const updatedIndex = response.questions.findIndex((item) => item.id === questionId)
    if (updatedIndex >= 0) currentIndex.value = updatedIndex
    syncSelection()
  } catch (e) {
    if (e instanceof ApiError && [0, 409].includes(e.status)) {
      syncMessage.value = 'Проверяем состояние ответа на сервере…'
      try {
        await refreshAfterUncertain()
        syncMessage.value = 'Состояние попытки обновлено.'
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

function startTimer(): void {
  timer = setInterval(() => {
    now.value = Date.now()
    if (
      remainingSeconds.value === 0 &&
      attempt.value?.status === AttemptStatusValue.InProgress &&
      timeoutRefreshes < 3
    ) {
      timeoutRefreshes += 1
      void refreshAfterUncertain().catch(() => {
        syncMessage.value = 'Время истекло. Проверьте состояние попытки на сервере.'
      })
    } else if (
      remainingSeconds.value === 0 &&
      attempt.value?.status === AttemptStatusValue.InProgress
    ) {
      syncMessage.value = 'Время истекло. Сервер ещё не подтвердил завершение попытки.'
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
    attempt.value?.status === AttemptStatusValue.InProgress
  )
    void load()
}

watch(attemptId, () => {
  requestId += 1
  stopTimer()
  void load()
})
onMounted(() => {
  void load()
  startTimer()
  document.addEventListener('visibilitychange', handleVisibility)
})
onBeforeUnmount(() => {
  requestId += 1
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
          {{ attempt.source === 0 ? 'Подготовленный вариант' : 'Случайная подборка' }}
        </p>
      </div>
      <div
        v-if="isExam && formattedRemaining"
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
            :disabled="currentIndex === 0"
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
            :disabled="currentIndex >= attempt.questions.length - 1"
            @click="goTo(currentIndex + 1)"
            >Следующий вопрос</AppButton
          >
        </div>
      </article>
      <div
        v-if="isFinished"
        class="rounded-app border p-4"
        :class="
          attempt.status === 1
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
