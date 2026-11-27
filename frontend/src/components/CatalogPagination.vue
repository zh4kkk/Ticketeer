<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ page: number; pageSize: number; totalCount: number; disabled?: boolean }>()
const emit = defineEmits<{ change: [page: number] }>()
const totalPages = computed(() => Math.max(1, Math.ceil(props.totalCount / props.pageSize)))
const canPrevious = computed(() => props.page > 1 && !props.disabled)
const canNext = computed(() => props.page < totalPages.value && !props.disabled)
</script>

<template>
  <nav v-if="totalCount > 0" aria-label="Пагинация" class="flex flex-wrap items-center justify-between gap-3 border-t border-line pt-4 text-sm text-muted">
    <span>Страница {{ Math.min(page, totalPages) }} из {{ totalPages }} · всего {{ totalCount }}</span>
    <div class="flex gap-2">
      <button type="button" class="rounded-app border border-line bg-surface px-3 py-2 font-medium text-ink hover:bg-canvas disabled:cursor-not-allowed disabled:opacity-50" :disabled="!canPrevious" @click="emit('change', page - 1)">Назад</button>
      <button type="button" class="rounded-app border border-line bg-surface px-3 py-2 font-medium text-ink hover:bg-canvas disabled:cursor-not-allowed disabled:opacity-50" :disabled="!canNext" @click="emit('change', page + 1)">Вперёд</button>
    </div>
  </nav>
</template>
