<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getCatalogTopics } from '@/api/catalog'
import { ApiAbortError, ApiError } from '@/api/http'
import EmptyState from '@/components/EmptyState.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import type { CatalogTopicResponse } from '@/types/catalog'
import { parseSearch } from '@/utils/routeQuery'

const route = useRoute()
const router = useRouter()
const search = ref('')
const items = ref<CatalogTopicResponse[]>([])
const loading = ref(false)
const error = ref('')
let controller: AbortController | null = null
let requestId = 0
function currentSearch(): string {
  return parseSearch(route.query.search)
}
function message(errorValue: unknown): string {
  return errorValue instanceof ApiError
    ? errorValue.status === 404
      ? 'Темы не найдены.'
      : errorValue.message
    : 'Не удалось загрузить темы.'
}
async function load(): Promise<void> {
  controller?.abort()
  controller = new AbortController()
  const id = ++requestId
  loading.value = true
  error.value = ''
  try {
    const response = await getCatalogTopics({ search: currentSearch(), signal: controller.signal })
    if (id !== requestId) return
    items.value = response
  } catch (e) {
    if (e instanceof ApiAbortError || id !== requestId) return
    error.value = message(e)
    items.value = []
  } finally {
    if (id === requestId) loading.value = false
  }
}
function applySearch(): void {
  const value = search.value.trim().slice(0, 200)
  void router.push({ name: 'cashier-topics', query: value ? { search: value } : {} })
}
function resetSearch(): void {
  search.value = ''
  applySearch()
}
watch(
  () => route.query.search,
  () => {
    search.value = currentSearch()
    void load()
  },
  { immediate: true },
)
onBeforeUnmount(() => {
  requestId += 1
  controller?.abort()
})
</script>
<template>
  <section class="space-y-6">
    <div>
      <p class="text-sm font-semibold uppercase tracking-wider text-brand">Учебные материалы</p>
      <h1 class="mt-2 text-3xl font-bold text-ink">Тематические разделы</h1>
      <p class="mt-2 text-muted">Выберите тему, чтобы перейти к связанным вопросам или билетам.</p>
    </div>
    <form class="flex flex-col gap-3 sm:flex-row" @submit.prevent="applySearch">
      <label class="sr-only" for="topic-search">Поиск тем</label
      ><input
        id="topic-search"
        v-model="search"
        class="app-input"
        maxlength="200"
        placeholder="Найти тему"
      /><button
        class="rounded-app bg-brand px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover"
        type="submit"
      >
        Найти</button
      ><button
        v-if="search"
        class="rounded-app border border-line bg-surface px-4 py-2.5 text-sm font-semibold text-ink hover:bg-canvas"
        type="button"
        @click="resetSearch"
      >
        Сбросить
      </button>
    </form>
    <LoadingState v-if="loading" /><ErrorState
      v-else-if="error"
      :message="error"
      :retry="load"
    /><EmptyState v-else-if="items.length === 0" message="Попробуйте изменить поисковый запрос." />
    <div v-else class="grid gap-4 md:grid-cols-2">
      <article
        v-for="topic in items"
        :key="topic.id"
        class="rounded-app border border-line bg-surface p-5 shadow-sm"
      >
        <h2 class="text-lg font-semibold text-ink">{{ topic.name }}</h2>
        <p v-if="topic.description" class="mt-2 text-sm leading-6 text-muted">
          {{ topic.description }}
        </p>
        <p class="mt-3 text-sm text-muted">Вопросов: {{ topic.questionCount }}</p>
        <div class="mt-4 flex flex-wrap gap-2">
          <RouterLink
            class="text-sm font-semibold text-secondary hover:underline"
            :to="{ name: 'cashier-questions', query: { topicId: String(topic.id) } }"
            >Вопросы темы</RouterLink
          ><RouterLink
            class="text-sm font-semibold text-secondary hover:underline"
            :to="{ name: 'cashier-tickets', query: { topicId: String(topic.id) } }"
            >Билеты темы</RouterLink
          >
        </div>
      </article>
    </div>
  </section>
</template>
