<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { startRandomTraining } from '@/api/attempts'
import { getCatalogTopics } from '@/api/catalog'
import { ApiAbortError, ApiError } from '@/api/http'
import AppButton from '@/components/AppButton.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import type { CatalogTopicResponse } from '@/types/catalog'
import { parsePositiveInt } from '@/utils/routeQuery'
import { getSessionGeneration } from '@/api/session'

const route = useRoute()
const router = useRouter()
const topics = ref<CatalogTopicResponse[]>([])
const selectedTopicId = ref(parsePositiveInt(route.query.topicId)?.toString() ?? '')
const questionCount = ref<number | string>(1)
const loading = ref(true)
const starting = ref(false)
const error = ref('')
const topicsError = ref('')
let controller: AbortController | null = null
let topicsRequestId = 0
let startRequestId = 0
let destroyed = false

const selectedTopic = computed(() =>
  topics.value.find((topic) => String(topic.id) === selectedTopicId.value),
)
const maxQuestions = computed(() => selectedTopic.value?.questionCount ?? 0)

function normalizeCount(): void {
  const max = maxQuestions.value
  const value = Number(questionCount.value)
  questionCount.value =
    max > 0 && Number.isFinite(value) ? Math.min(Math.max(1, Math.trunc(value)), max) : 1
}

async function loadTopics(): Promise<void> {
  controller?.abort()
  const requestId = ++topicsRequestId
  const currentController = new AbortController()
  controller = currentController
  loading.value = true
  topicsError.value = ''
  try {
    const response = await getCatalogTopics({ signal: currentController.signal })
    if (requestId !== topicsRequestId || destroyed) return
    topics.value = response
    if (!selectedTopic.value && topics.value.length && topics.value[0])
      selectedTopicId.value = String(topics.value[0].id)
    normalizeCount()
  } catch (e) {
    if (!(e instanceof ApiAbortError) && requestId === topicsRequestId && !destroyed)
      topicsError.value = e instanceof ApiError ? e.message : 'Не удалось загрузить темы.'
  } finally {
    if (!currentController.signal.aborted) loading.value = false
  }
}

function selectTopic(): void {
  normalizeCount()
}

async function start(): Promise<void> {
  const topicId = parsePositiveInt(selectedTopicId.value)
  const count = Number(questionCount.value)
  if (starting.value) return
  if (
    !selectedTopic.value ||
    topicId === undefined ||
    !Number.isSafeInteger(count) ||
    count < 1 ||
    count > selectedTopic.value.questionCount
  ) {
    error.value = 'Выберите тему и укажите целое количество вопросов от 1 до доступного числа.'
    return
  }
  const operation = ++startRequestId
  const operationSession = getSessionGeneration()
  starting.value = true
  error.value = ''
  try {
    const response = await startRandomTraining({ topicId, questionCount: count })
    if (destroyed || operation !== startRequestId || operationSession !== getSessionGeneration())
      return
    await router.push({ name: 'cashier-attempt', params: { id: response.id } })
  } catch (e) {
    if (!destroyed && operation === startRequestId && operationSession === getSessionGeneration())
      error.value =
        e instanceof ApiError
          ? e.message
          : 'Не удалось начать тренировку. Попытка могла быть создана; проверьте незавершённые попытки.'
  } finally {
    starting.value = false
  }
}

onMounted(() => void loadTopics())
onBeforeUnmount(() => {
  destroyed = true
  topicsRequestId += 1
  startRequestId += 1
  controller?.abort()
})
</script>

<template>
  <section class="mx-auto max-w-2xl space-y-6">
    <div>
      <p class="text-sm font-semibold uppercase tracking-wider text-brand">Практика</p>
      <h1 class="mt-2 text-3xl font-bold text-ink">Случайная тренировка</h1>
      <p class="mt-2 text-muted">Выберите тему и количество вопросов для новой тренировки.</p>
    </div>
    <LoadingState v-if="loading" />
    <ErrorState v-else-if="topicsError" :message="topicsError" :retry="loadTopics" />
    <form
      v-else
      class="space-y-5 rounded-app border border-line bg-surface p-5 shadow-sm"
      @submit.prevent="start"
    >
      <label class="block text-sm font-semibold text-ink" for="random-topic">Тема</label>
      <select
        id="random-topic"
        v-model="selectedTopicId"
        class="app-input"
        :disabled="starting"
        @change="selectTopic"
      >
        <option value="" disabled>Выберите тему</option>
        <option v-for="topic in topics" :key="topic.id" :value="String(topic.id)">
          {{ topic.name }}
        </option>
      </select>
      <p v-if="selectedTopic" class="text-sm text-muted">
        Доступно вопросов: {{ selectedTopic.questionCount }}
      </p>
      <label class="block text-sm font-semibold text-ink" for="random-count"
        >Количество вопросов</label
      >
      <input
        id="random-count"
        v-model.number="questionCount"
        class="app-input"
        type="number"
        min="1"
        :max="maxQuestions || 1"
        :disabled="!selectedTopic || starting"
        @change="normalizeCount"
      />
      <p v-if="error" class="text-sm text-danger">{{ error }}</p>
      <AppButton type="submit" :disabled="starting || !selectedTopic || maxQuestions < 1">
        {{ starting ? 'Запуск…' : 'Начать тренировку' }}
      </AppButton>
    </form>
  </section>
</template>
