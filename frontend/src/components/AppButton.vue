<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'button' | 'submit' | 'reset'
    loading?: boolean
    disabled?: boolean
    variant?: 'primary' | 'secondary'
  }>(),
  { type: 'button', variant: 'primary' },
)
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    class="inline-flex items-center justify-center gap-2 rounded-app px-4 py-2.5 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand"
    :class="variant === 'primary' ? 'bg-brand text-white hover:bg-brand-hover' : 'border border-line bg-surface text-ink hover:bg-canvas'"
  >
    <span v-if="loading" class="app-spinner h-4 w-4" aria-hidden="true" />
    <slot />
  </button>
</template>
