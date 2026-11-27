<script setup lang="ts">
defineProps<{ message: string; variant?: 'error' | 'info' | 'success' }>()
</script>

<template>
  <div
    role="alert"
    class="rounded-app border px-4 py-3 text-sm"
    :class="{
      'border-red-200 bg-red-50 text-danger': variant !== 'success' && variant !== 'info',
      'border-secondary/25 bg-secondary-soft text-secondary': variant === 'info',
      'border-green-200 bg-green-50 text-success': variant === 'success',
    }"
  >
    {{ message }}
  </div>
</template>
