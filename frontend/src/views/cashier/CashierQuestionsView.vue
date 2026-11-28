<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getCatalogQuestions, getCatalogTopics } from '@/api/catalog'
import { ApiAbortError, ApiError } from '@/api/http'
import EmptyState from '@/components/EmptyState.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import Pagination from '@/components/CatalogPagination.vue'
import type { CatalogQuestionListResponse, CatalogTopicResponse } from '@/types/catalog'
import type { PagedResponse } from '@/types/api'
import { parsePage, parsePositiveInt, parseSearch } from '@/utils/routeQuery'
const route = useRoute()
const router = useRouter()
const pageSize = 20
const search = ref('')
const topicId = ref('')
const topics = ref<CatalogTopicResponse[]>([])
const result = ref<PagedResponse<CatalogQuestionListResponse> | null>(null)
const loading = ref(false)
const error = ref('')
let controller: AbortController | null = null
let topicController: AbortController | null = null
let topicRequestId = 0
let requestId = 0
const topicLoadError = ref('')
const topicsLoading = ref(false)
const str = (k: string) => parseSearch(route.query[k])
const topic = () => {
  return parsePositiveInt(route.query.topicId)
}
const page = () => {
  return parsePage(route.query.page, pageSize)
}
const msg = (e: unknown) => (e instanceof ApiError ? e.message : 'Не удалось загрузить вопросы.')
async function load() {
  controller?.abort()
  controller = new AbortController()
  const id = ++requestId
  loading.value = true
  error.value = ''
  try {
    const p = page()
    const response = await getCatalogQuestions({
      search: str('search'),
      topicId: topic(),
      page: p,
      pageSize,
      signal: controller.signal,
    })
    if (id !== requestId) return
    result.value = response
    const max = Math.max(1, Math.ceil(response.totalCount / pageSize))
    if (p > max) void router.replace({ query: { ...route.query, page: String(max) } })
  } catch (e) {
    if (e instanceof ApiAbortError || id !== requestId) return
    error.value = msg(e)
    result.value = null
  } finally {
    if (id === requestId) loading.value = false
  }
}
function apply() {
  const q: Record<string, string> = {}
  const s = search.value.trim().slice(0, 200)
  if (s) q.search = s
  if (topicId.value) q.topicId = topicId.value
  void router.push({ name: 'cashier-questions', query: q })
}
function changePage(p: number) {
  if (result.value && p >= 1 && p <= Math.ceil(result.value.totalCount / pageSize))
    void router.push({ query: { ...route.query, page: String(p) } })
}
function reset() {
  search.value = ''
  topicId.value = ''
  apply()
}
watch(
  () => route.query,
  () => {
    search.value = str('search')
    topicId.value = topic()?.toString() || ''
    void load()
  },
  { immediate: true },
)
async function loadTopics(): Promise<void> {
  topicController?.abort()
  topicController = new AbortController()
  const currentTopicRequest = ++topicRequestId
  topicsLoading.value = true
  topicLoadError.value = ''
  try {
    const response = await getCatalogTopics({ signal: topicController.signal })
    if (currentTopicRequest !== topicRequestId) return
    topics.value = response
  } catch (e) {
    if (!(e instanceof ApiAbortError) && currentTopicRequest === topicRequestId)
      topicLoadError.value = 'Не удалось загрузить темы. Повторите попытку.'
  } finally {
    if (currentTopicRequest === topicRequestId) topicsLoading.value = false
  }
}
onMounted(() => void loadTopics())
onBeforeUnmount(() => {
  controller?.abort()
  topicController?.abort()
  topicRequestId += 1
  requestId += 1
})
</script>
<template>
  <section class="space-y-6">
    <div>
      <p class="text-sm font-semibold uppercase tracking-wider text-brand">Учебные материалы</p>
      <h1 class="mt-2 text-3xl font-bold text-ink">Вопросы</h1>
    </div>
    <form
      class="grid gap-3 rounded-app border border-line bg-surface p-4 sm:grid-cols-[2fr_1fr_auto]"
      @submit.prevent="apply"
    >
      <label class="sr-only" for="question-search">Поиск вопросов</label
      ><input
        id="question-search"
        v-model="search"
        class="app-input"
        maxlength="200"
        placeholder="Поиск по тексту вопроса"
      /><select
        v-model="topicId"
        class="app-input"
        aria-label="Тема"
        :disabled="topicsLoading || !!topicLoadError"
      >
        <option value="">Все темы</option>
        <option v-for="topicItem in topics" :key="topicItem.id" :value="String(topicItem.id)">
          {{ topicItem.name }}
        </option></select
      ><button
        class="rounded-app bg-brand px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover"
        type="submit"
      >
        Применить</button
      ><button
        v-if="search || topicId"
        class="sm:col-span-3 justify-self-start text-sm font-semibold text-secondary hover:underline"
        type="button"
        @click="reset"
      >
        Сбросить фильтры
      </button>
    </form>
    <div
      v-if="topicLoadError"
      class="rounded-app border border-red-200 bg-red-50 p-3 text-sm text-red-800"
    >
      {{ topicLoadError }}
      <button class="ml-2 font-semibold underline" type="button" @click="loadTopics">
        Повторить
      </button>
    </div>
    <LoadingState v-if="loading" /><ErrorState
      v-else-if="error"
      :message="error"
      :retry="load"
    /><EmptyState
      v-else-if="!result || result.items.length === 0"
      message="Вопросы по заданным условиям не найдены."
    />
    <div v-else class="space-y-3">
      <RouterLink
        v-for="question in result.items"
        :key="question.id"
        class="block rounded-app border border-line bg-surface p-5 shadow-sm transition hover:border-brand"
        :to="{ name: 'cashier-question-detail', params: { id: question.id }, query: route.query }"
        ><p class="text-base font-medium leading-6 text-ink">{{ question.text }}</p>
        <p class="mt-2 text-sm text-secondary">{{ question.topicName }}</p></RouterLink
      ><Pagination
        :page="result.page"
        :page-size="result.pageSize"
        :total-count="result.totalCount"
        @change="changePage"
      />
    </div>
  </section>
</template>
