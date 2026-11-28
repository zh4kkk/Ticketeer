<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getCatalogTicket } from '@/api/catalog'
import { startTrainingTicket } from '@/api/attempts'
import { ApiAbortError, ApiError } from '@/api/http'
import AppButton from '@/components/AppButton.vue'
import EmptyState from '@/components/EmptyState.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import StatusLabel from '@/components/StatusLabel.vue'
import type { CatalogTicketResponse } from '@/types/catalog'
import { parsePositiveInt } from '@/utils/routeQuery'
import { getSessionGeneration } from '@/api/session'
const route = useRoute()
const router = useRouter()
const item = ref<CatalogTicketResponse | null>(null)
const loading = ref(false)
const error = ref('')
const startError = ref('')
const starting = ref(false)
let controller: AbortController | null = null
let requestId = 0
let startRequestId = 0
let destroyed = false
const id = () => {
  return parsePositiveInt(route.params.id)
}
const message = (e: unknown) =>
  e instanceof ApiError && e.status === 404
    ? 'Билет не найден.'
    : e instanceof ApiError
      ? e.message
      : 'Не удалось загрузить билет.'
async function load() {
  controller?.abort()
  controller = new AbortController()
  const request = ++requestId
  const value = id()
  item.value = null
  error.value = ''
  if (!value) {
    loading.value = false
    error.value = 'Некорректный идентификатор билета.'
    return
  }
  loading.value = true
  try {
    const response = await getCatalogTicket(value, controller.signal)
    if (request !== requestId) return
    item.value = response
  } catch (e) {
    if (e instanceof ApiAbortError || request !== requestId) return
    error.value = message(e)
  } finally {
    if (request === requestId) loading.value = false
  }
}
watch(
  () => route.params.id,
  () => {
    startRequestId += 1
    void load()
  },
  { immediate: true },
)
onBeforeUnmount(() => {
  destroyed = true
  startRequestId += 1
  requestId += 1
  controller?.abort()
})
async function startTraining(): Promise<void> {
  if (!item.value || starting.value) return
  const operation = ++startRequestId
  const operationSession = getSessionGeneration()
  const ticketId = item.value.id
  starting.value = true
  startError.value = ''
  try {
    const response = await startTrainingTicket(ticketId)
    if (
      destroyed ||
      operation !== startRequestId ||
      operationSession !== getSessionGeneration() ||
      id() !== ticketId
    )
      return
    await router.push({ name: 'cashier-attempt', params: { id: response.id } })
  } catch (e) {
    if (!destroyed && operation === startRequestId && operationSession === getSessionGeneration())
      startError.value =
        e instanceof ApiError
          ? e.message
          : 'Не удалось начать тренировку. Попытка могла быть создана; проверьте незавершённые попытки.'
  } finally {
    starting.value = false
  }
}
</script>
<template>
  <section class="space-y-6">
    <RouterLink
      class="text-sm font-semibold text-secondary hover:underline"
      :to="{ name: 'cashier-tickets', query: route.query }"
      >← Вернуться к билетам</RouterLink
    ><LoadingState v-if="loading" /><ErrorState
      v-else-if="error"
      :message="error"
      :retry="load"
    /><EmptyState v-else-if="!item" message="Материал недоступен." />
    <article v-else class="rounded-app border border-line bg-surface p-6 shadow-sm">
      <div class="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p class="text-sm font-semibold uppercase tracking-wider text-brand">
            Тренировочный билет
          </p>
          <h1 class="mt-2 text-3xl font-bold text-ink">{{ item.title }}</h1>
        </div>
        <StatusLabel :completed="item.isCompleted" />
      </div>
      <p v-if="item.description" class="mt-5 leading-7 text-muted">{{ item.description }}</p>
      <p class="mt-4 text-sm text-muted">Вопросов: {{ item.questionCount }}</p>
      <div v-if="item.topics.length" class="mt-6">
        <h2 class="font-semibold text-ink">Темы</h2>
        <div class="mt-3 flex flex-wrap gap-2">
          <RouterLink
            v-for="topic in item.topics"
            :key="topic.id"
            class="rounded-full bg-secondary-soft px-3 py-1.5 text-sm font-medium text-secondary hover:underline"
            :to="{ name: 'cashier-questions', query: { topicId: String(topic.id) } }"
            >{{ topic.name }}</RouterLink
          >
        </div>
      </div>
      <div class="mt-8 flex flex-wrap gap-3">
        <AppButton variant="primary" :disabled="starting" @click="startTraining">
          {{ starting ? 'Запуск…' : item.isCompleted ? 'Пройти ещё раз' : 'Начать тренировку' }}
        </AppButton>
        <AppButton
          variant="secondary"
          @click="router.push({ name: 'cashier-tickets', query: route.query })"
          >К списку билетов</AppButton
        >
      </div>
      <p v-if="startError" class="mt-3 text-sm text-danger">{{ startError }}</p>
    </article>
  </section>
</template>
