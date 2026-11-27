<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{ code: 403 | 404 }>()
const auth = useAuthStore()
const title = computed(() => (props.code === 403 ? 'Доступ запрещён' : 'Страница не найдена'))
const description = computed(() =>
  props.code === 403 ? 'У вашей роли нет доступа к этому разделу.' : 'Проверьте адрес или вернитесь на главную страницу.',
)
const home = computed(() => (auth.isAdmin ? '/admin' : auth.isCashier ? '/cashier' : '/login'))
</script>

<template>
  <main class="grid min-h-screen place-items-center bg-canvas px-4">
    <section class="text-center">
      <p class="text-sm font-bold uppercase tracking-widest text-brand">Ошибка {{ code }}</p>
      <h1 class="mt-3 text-3xl font-bold text-ink">{{ title }}</h1>
      <p class="mt-3 text-muted">{{ description }}</p>
      <RouterLink :to="home" class="mt-6 inline-flex rounded-app bg-brand px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand">Вернуться на главную</RouterLink>
    </section>
  </main>
</template>
