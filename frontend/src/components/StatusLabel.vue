<script setup lang="ts">
defineProps<{ completed: boolean }>()
</script>
<template>
  <span class="inline-flex rounded-full px-2.5 py-1 text-xs font-semibold" :class="completed ? 'bg-green-50 text-success' : 'bg-canvas text-muted'">{{ completed ? 'Пройден' : 'Не пройден' }}</span>
</template>
