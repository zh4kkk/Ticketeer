<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getCatalogExam } from '@/api/catalog'
import { ApiAbortError, ApiError } from '@/api/http'
import AppButton from '@/components/AppButton.vue'
import EmptyState from '@/components/EmptyState.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import type { CatalogExamResponse } from '@/types/catalog'
const route = useRoute()
const router = useRouter()
const item = ref<CatalogExamResponse | null>(null)
const loading = ref(false)
const error = ref('')
let controller: AbortController | null = null
let requestId = 0
const id = () => {
  const n = Number(route.params.id)
  return Number.isInteger(n) && n > 0 ? n : undefined
}
const formatTime = (s: number) =>
  s < 60 ? `${s} сек.` : `${Math.floor(s / 60)} мин.${s % 60 ? ` ${s % 60} сек.` : ''}`
const message = (e: unknown) =>
  e instanceof ApiError && e.status === 404
    ? 'Экзамен не найден.'
    : e instanceof ApiError
      ? e.message
      : 'Не удалось загрузить экзамен.'
async function load() {
  controller?.abort()
  controller = new AbortController()
  const request = ++requestId
  const value = id()
  item.value = null
  error.value = ''
  if (!value) {
    loading.value = false
    error.value = 'Некорректный идентификатор экзамена.'
    return
  }
  loading.value = true
  try {
    const response = await getCatalogExam(value, controller.signal)
    if (request !== requestId) return
    item.value = response
  } catch (e) {
    if (e instanceof ApiAbortError || request !== requestId) return
    error.value = message(e)
  } finally {
    if (request === requestId) loading.value = false
  }
}
watch(() => route.params.id, load, { immediate: true })
onBeforeUnmount(() => {
  requestId += 1
  controller?.abort()
})
</script>
<template>
  <section class="space-y-6">
    <RouterLink
      class="text-sm font-semibold text-secondary hover:underline"
      :to="{ name: 'cashier-exams', query: route.query }"
      >← Вернуться к экзаменам</RouterLink
    ><LoadingState v-if="loading" /><ErrorState
      v-else-if="error"
      :message="error"
      :retry="load"
    /><EmptyState v-else-if="!item" message="Материал недоступен." />
    <article v-else class="rounded-app border border-line bg-surface p-6 shadow-sm">
      <p class="text-sm font-semibold uppercase tracking-wider text-brand">
        Подготовленный экзамен
      </p>
      <h1 class="mt-2 text-3xl font-bold text-ink">{{ item.title }}</h1>
      <p v-if="item.description" class="mt-5 leading-7 text-muted">{{ item.description }}</p>
      <dl class="mt-6 grid gap-4 sm:grid-cols-3">
        <div>
          <dt class="text-sm text-muted">Вопросов</dt>
          <dd class="mt-1 text-lg font-semibold text-ink">{{ item.questionCount }}</dd>
        </div>
        <div>
          <dt class="text-sm text-muted">Ограничение времени</dt>
          <dd class="mt-1 text-lg font-semibold text-ink">
            {{ formatTime(item.timeLimitSeconds) }}
          </dd>
        </div>
        <div>
          <dt class="text-sm text-muted">Допустимо ошибок</dt>
          <dd class="mt-1 text-lg font-semibold text-ink">{{ item.maxWrongAnswers }}</dd>
        </div>
      </dl>
      <p class="mt-7 rounded-app bg-secondary-soft p-4 text-sm text-secondary">
        Состав вопросов и правильные ответы доступны только в процессе прохождения экзамена.
      </p>
      <AppButton
        variant="secondary"
        class="mt-8"
        @click="router.push({ name: 'cashier-exams', query: route.query })"
        >К списку экзаменов</AppButton
      >
    </article>
  </section>
</template>
