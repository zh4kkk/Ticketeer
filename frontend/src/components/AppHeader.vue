<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AppButton from './AppButton.vue'

defineProps<{ title: string }>()
const auth = useAuthStore()
const router = useRouter()

function logout(): void {
  auth.logout()
  void router.push({ name: 'login' })
}
</script>

<template>
  <header class="border-b border-line bg-surface">
    <div class="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-4 sm:px-6 lg:px-8">
      <div>
        <RouterLink class="text-lg font-bold text-ink" :to="auth.isAdmin ? '/admin' : '/cashier'">Ticketeer</RouterLink>
        <p class="hidden text-xs text-muted sm:block">{{ title }}</p>
      </div>
      <div class="flex items-center gap-3">
        <span class="hidden text-sm text-muted sm:block">{{ auth.user?.fullName }}</span>
        <AppButton variant="secondary" @click="logout">Выйти</AppButton>
      </div>
    </div>
  </header>
</template>
