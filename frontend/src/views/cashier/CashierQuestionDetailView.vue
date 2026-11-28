<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getCatalogQuestion } from '@/api/catalog'
import { ApiAbortError, ApiError } from '@/api/http'
import AppButton from '@/components/AppButton.vue'
import EmptyState from '@/components/EmptyState.vue'
import ErrorState from '@/components/ErrorState.vue'
import LoadingState from '@/components/LoadingState.vue'
import type { CatalogQuestionResponse } from '@/types/catalog'
import { parsePositiveInt } from '@/utils/routeQuery'
const route = useRoute()
const router = useRouter()
const item = ref<CatalogQuestionResponse | null>(null)
const loading = ref(false)
const error = ref('')
let controller: AbortController | null = null
let requestId = 0
const id = () => {
  return parsePositiveInt(route.params.id)
}
const message = (e: unknown) =>
  e instanceof ApiError && e.status === 404
    ? 'Вопрос не найден.'
    : e instanceof ApiError
      ? e.message
      : 'Не удалось загрузить вопрос.'
async function load() {
  controller?.abort()
  controller = new AbortController()
  const request = ++requestId
  const value = id()
  item.value = null
  error.value = ''
  if (!value) {
    loading.value = false
    error.value = 'Некорректный идентификатор вопроса.'
    return
  }
  loading.value = true
  try {
    const response = await getCatalogQuestion(value, controller.signal)
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
      :to="{ name: 'cashier-questions', query: route.query }"
      >← Вернуться к вопросам</RouterLink
    ><LoadingState v-if="loading" /><ErrorState
      v-else-if="error"
      :message="error"
      :retry="load"
    /><EmptyState v-else-if="!item" message="Материал недоступен." />
    <article v-else class="rounded-app border border-line bg-surface p-6 shadow-sm">
      <p class="text-sm font-semibold uppercase tracking-wider text-brand">Учебный вопрос</p>
      <p class="mt-2 text-sm font-medium text-secondary">{{ item.topicName }}</p>
      <h1 class="mt-4 text-2xl font-bold leading-8 text-ink">{{ item.text }}</h1>
      <div class="mt-8 space-y-3">
        <h2 class="font-semibold text-ink">Варианты ответа</h2>
        <div
          v-for="option in item.answerOptions"
          :key="option.position"
          class="rounded-app border border-line bg-canvas p-4"
        >
          <span class="mr-2 font-semibold text-secondary">{{ option.position }}.</span
          >{{ option.text }}
        </div>
      </div>
      <p class="mt-6 text-sm text-muted">
        Правильность ответа определяется только во время прохождения попытки.
      </p>
      <AppButton
        variant="secondary"
        class="mt-4"
        @click="router.push({ name: 'cashier-questions', query: route.query })"
        >К списку вопросов</AppButton
      >
    </article>
  </section>
</template>
