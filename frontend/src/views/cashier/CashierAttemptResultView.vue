<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getAttempt } from '@/api/attempts'
import { ApiAbortError, ApiError } from '@/api/http'
import AppButton from '@/components/AppButton.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import type { AttemptResponse } from '@/types/attempts'
import { AttemptKindValue, AttemptStatusValue } from '@/types/attempts'
import { parsePositiveInt } from '@/utils/routeQuery'

const route = useRoute()
const router = useRouter()
const attempt = ref<AttemptResponse | null>(null)
const loading = ref(false)
const error = ref('')
let controller: AbortController | null = null
let requestId = 0

const id = computed(() => parsePositiveInt(route.params.id))
const answeredCount = computed(
  () => attempt.value?.questions.filter((question) => question.answeredAt).length ?? 0,
)
const duration = computed(() => {
  if (!attempt.value?.finishedAt) return null
  const seconds = Math.max(
    0,
    Math.round((Date.parse(attempt.value.finishedAt) - Date.parse(attempt.value.startedAt)) / 1000),
  )
  return `${Math.floor(seconds / 60)} мин. ${seconds % 60} сек.`
})
const statusText = computed(() => {
  if (!attempt.value) return ''
  if (
    attempt.value.kind === AttemptKindValue.Training &&
    attempt.value.status === AttemptStatusValue.Completed
  )
    return 'Тренировка завершена'
  if (attempt.value.status === AttemptStatusValue.Completed) return 'Экзамен сдан'
  if (attempt.value.status === AttemptStatusValue.TimedOut) return 'Время экзамена истекло'
  if (attempt.value.status === AttemptStatusValue.Failed) return 'Экзамен не сдан'
  return 'Попытка ещё продолжается'
})

async function load(): Promise<void> {
  controller?.abort()
  const currentRequest = ++requestId
  attempt.value = null
  error.value = ''
  if (!id.value) {
    error.value = 'Некорректный идентификатор попытки.'
    loading.value = false
    return
  }
  controller = new AbortController()
  loading.value = true
  try {
    const response = await getAttempt(id.value, controller.signal)
    if (currentRequest !== requestId) return
    if (response.status === AttemptStatusValue.InProgress) {
      await router.replace({ name: 'cashier-attempt', params: { id: response.id } })
      return
    }
    attempt.value = response
  } catch (e) {
    if (e instanceof ApiAbortError || currentRequest !== requestId) return
    error.value =
      e instanceof ApiError && e.status === 404
        ? 'Попытка не найдена.'
        : e instanceof ApiError
          ? e.message
          : 'Не удалось загрузить результат.'
  } finally {
    if (currentRequest === requestId) loading.value = false
  }
}

watch(id, () => void load())
onMounted(() => void load())
onBeforeUnmount(() => {
  requestId += 1
  controller?.abort()
})
</script>

<template>
  <section class="mx-auto max-w-3xl space-y-6">
    <LoadingState v-if="loading" />
    <ErrorState v-else-if="error" :message="error" :retry="load" />
    <template v-else-if="attempt">
      <div>
        <p class="text-sm font-semibold uppercase tracking-wider text-brand">
          {{ attempt.kind === 0 ? 'Тренировка' : 'Экзамен' }}
        </p>
        <h1 class="mt-2 text-3xl font-bold text-ink">{{ statusText }}</h1>
        <p class="mt-2 text-muted">
          {{ attempt.source === 0 ? 'Подготовленный вариант' : 'Случайная подборка' }}
        </p>
      </div>
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-app border border-line bg-surface p-4">
          <p class="text-sm text-muted">Вопросов</p>
          <p class="mt-1 text-2xl font-bold text-ink">{{ attempt.questionCount }}</p>
        </div>
        <div class="rounded-app border border-line bg-surface p-4">
          <p class="text-sm text-muted">Отвечено</p>
          <p class="mt-1 text-2xl font-bold text-ink">{{ answeredCount }}</p>
        </div>
        <div class="rounded-app border border-line bg-surface p-4">
          <p class="text-sm text-muted">Правильных</p>
          <p class="mt-1 text-2xl font-bold text-success">{{ attempt.correctAnswers }}</p>
        </div>
        <div class="rounded-app border border-line bg-surface p-4">
          <p class="text-sm text-muted">Ошибок</p>
          <p class="mt-1 text-2xl font-bold text-danger">{{ attempt.wrongAnswers }}</p>
        </div>
      </div>
      <div class="rounded-app border border-line bg-surface p-5 text-sm text-muted shadow-sm">
        <p>Без ответа: {{ attempt.questionCount - answeredCount }}</p>
        <p class="mt-2">Начало: {{ new Date(attempt.startedAt).toLocaleString('ru-RU') }}</p>
        <p v-if="attempt.finishedAt" class="mt-2">
          Завершение: {{ new Date(attempt.finishedAt).toLocaleString('ru-RU') }}
        </p>
        <p v-if="duration" class="mt-2">Продолжительность: {{ duration }}</p>
      </div>
      <div class="flex flex-wrap gap-3">
        <AppButton variant="secondary" @click="router.push({ name: 'cashier-home' })"
          >На главную</AppButton
        ><AppButton
          variant="secondary"
          @click="router.push({ name: attempt.kind === 0 ? 'cashier-tickets' : 'cashier-exams' })"
          >К каталогу</AppButton
        >
      </div>
    </template>
  </section>
</template>
