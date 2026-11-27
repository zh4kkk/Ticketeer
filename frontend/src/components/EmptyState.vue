<script setup lang="ts">
withDefaults(defineProps<{ title?: string; message: string }>(), { title: 'Ничего не найдено' })
</script>
<template>
  <div class="rounded-app border border-dashed border-line bg-surface px-5 py-8 text-center"><h2 class="font-semibold text-ink">{{ title }}</h2><p class="mt-2 text-sm text-muted">{{ message }}</p></div>
</template>
