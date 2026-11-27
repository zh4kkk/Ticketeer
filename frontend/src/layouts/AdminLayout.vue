<script setup lang="ts">
import AppHeader from '@/components/AppHeader.vue'
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-ink">
    <AppHeader title="Административная панель" />
    <div class="mx-auto flex max-w-7xl flex-col gap-6 px-4 py-6 sm:px-6 lg:flex-row lg:px-8">
      <nav aria-label="Навигация администратора" class="flex gap-2 overflow-x-auto lg:w-52 lg:flex-col">
        <RouterLink class="nav-link" to="/admin">Главная</RouterLink>
      </nav>
      <main class="min-w-0 flex-1"><RouterView /></main>
    </div>
  </div>
</template>

<style scoped>
.nav-link { @apply whitespace-nowrap rounded-app px-3 py-2 text-sm font-medium text-muted hover:bg-blue-50 hover:text-brand; }
.router-link-exact-active { @apply bg-blue-50 text-brand; }
</style>
