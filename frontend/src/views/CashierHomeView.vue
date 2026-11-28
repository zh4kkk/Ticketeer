<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getCatalogTopics } from '@/api/catalog'
import { ApiAbortError, ApiError } from '@/api/http'
import EmptyState from '@/components/EmptyState.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import { useAuthStore } from '@/stores/auth'
import type { CatalogTopicResponse } from '@/types/catalog'
const auth = useAuthStore()
const router = useRouter()
const topics = ref<CatalogTopicResponse[]>([])
const search = ref('')
const loading = ref(false)
const error = ref('')
let controller: AbortController | null = null
let requestId = 0
async function loadTopics(): Promise<void> {
  controller?.abort()
  controller = new AbortController()
  const id = ++requestId
  loading.value = true
  error.value = ''
  try {
    const response = await getCatalogTopics({ signal: controller.signal })
    if (id !== requestId) return
    topics.value = response
  } catch (e) {
    if (e instanceof ApiAbortError || id !== requestId) return
    error.value = e instanceof ApiError ? e.message : 'Не удалось загрузить темы.'
  } finally {
    if (id === requestId) loading.value = false
  }
}
function searchQuestions(): void {
  const value = search.value.trim().slice(0, 200)
  void router.push({ name: 'cashier-questions', query: value ? { search: value } : {} })
}
onMounted(loadTopics)
onBeforeUnmount(() => {
  requestId += 1
  controller?.abort()
})
</script>

<template>
  <section class="space-y-8">
    <div>
      <p class="text-sm font-semibold uppercase tracking-wider text-brand">Рабочее место кассира</p>
      <h1 class="mt-2 text-3xl font-bold text-ink">Здравствуйте, {{ auth.user?.fullName }}</h1>
      <p v-if="auth.user?.groupName" class="mt-2 text-muted">
        Группа: <span class="font-medium text-ink">{{ auth.user.groupName }}</span>
      </p>
    </div>
    <form
      class="rounded-app border border-line bg-surface p-5 shadow-sm"
      @submit.prevent="searchQuestions"
    >
      <label for="home-search" class="mb-2 block text-sm font-semibold text-ink"
        >Найти учебный вопрос</label
      >
      <div class="flex flex-col gap-3 sm:flex-row">
        <input
          id="home-search"
          v-model="search"
          class="app-input"
          maxlength="200"
          placeholder="Введите текст вопроса"
        /><button
          type="submit"
          class="rounded-app bg-brand px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover"
        >
          Искать вопросы
        </button>
      </div>
    </form>
    <div class="flex flex-wrap gap-3">
      <RouterLink
        class="rounded-app bg-brand px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover"
        :to="{ name: 'cashier-random-training' }"
        >Случайная тренировка</RouterLink
      >
      <RouterLink
        class="rounded-app border border-line bg-surface px-4 py-2.5 text-sm font-semibold text-ink hover:bg-canvas"
        :to="{ name: 'cashier-exams' }"
        >Каталог экзаменов</RouterLink
      >
    </div>
    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <RouterLink
        v-for="link in [
          { to: '/cashier/topics', label: 'Темы', text: 'Обзор учебных разделов' },
          { to: '/cashier/tickets', label: 'Билеты', text: 'Подготовленные тренировки' },
          { to: '/cashier/exams', label: 'Экзамены', text: 'Правила и ограничения' },
          { to: '/cashier/questions', label: 'Вопросы', text: 'Поиск по базе вопросов' },
        ]"
        :key="link.to"
        :to="link.to"
        class="rounded-app border border-line bg-surface p-5 shadow-sm transition hover:border-brand"
        ><h2 class="font-semibold text-ink">{{ link.label }}</h2>
        <p class="mt-2 text-sm text-muted">{{ link.text }}</p></RouterLink
      >
    </div>
    <div>
      <div class="flex items-end justify-between gap-3">
        <div>
          <h2 class="text-xl font-bold text-ink">Тематические разделы</h2>
          <p class="mt-1 text-sm text-muted">Актуальные разделы из учебного каталога.</p>
        </div>
        <RouterLink
          class="text-sm font-semibold text-secondary hover:underline"
          to="/cashier/topics"
          >Все темы</RouterLink
        >
      </div>
      <LoadingState v-if="loading" /><ErrorState
        v-else-if="error"
        :message="error"
        :retry="loadTopics"
      /><EmptyState v-else-if="topics.length === 0" message="Темы пока недоступны." />
      <div v-else class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <RouterLink
          v-for="topic in topics.slice(0, 6)"
          :key="topic.id"
          :to="{ name: 'cashier-questions', query: { topicId: String(topic.id) } }"
          class="rounded-app border border-line bg-surface p-4 hover:border-brand"
          ><h3 class="font-semibold text-ink">{{ topic.name }}</h3>
          <p class="mt-1 text-sm text-muted">Вопросов: {{ topic.questionCount }}</p></RouterLink
        >
      </div>
    </div>
  </section>
</template>
