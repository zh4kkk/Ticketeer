<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
const auth = useAuthStore()
</script>

<template>
  <section>
    <p class="text-sm font-semibold uppercase tracking-wider text-brand">Административная панель</p>
    <h1 class="mt-2 text-3xl font-bold text-ink">Здравствуйте, {{ auth.user?.fullName }}</h1>
    <div class="mt-8 rounded-app border border-line bg-surface p-6 shadow-sm">
      <h2 class="text-lg font-semibold">Рабочее пространство администратора</h2>
      <p class="mt-2 text-sm leading-6 text-muted">
        Здесь появятся управление пользователями, группами и учебными материалами. Разделы будут
        добавлены по мере реализации.
      </p>
      <p class="mt-5 text-sm text-muted">
        Роль: <span class="font-medium text-ink">Администратор</span>
      </p>
    </div>
  </section>
</template>
