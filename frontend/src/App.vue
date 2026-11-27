<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const ready = ref(false)

onMounted(async () => {
  await auth.restoreSession()
  ready.value = true
})
</script>

<template>
  <div v-if="!ready" class="grid min-h-screen place-items-center bg-slate-50" aria-live="polite">
    <div class="text-center"><div class="app-spinner mx-auto" aria-hidden="true" /><p class="mt-3 text-sm text-muted">Проверяем сессию…</p></div>
  </div>
  <RouterView v-else />
</template>
