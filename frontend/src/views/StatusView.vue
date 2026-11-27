<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import AppButton from '@/components/AppButton.vue'

const props = defineProps<{ code: 403 | 404 }>()
const auth = useAuthStore()
const title = computed(() => (props.code === 403 ? 'Доступ запрещён' : 'Страница не найдена'))
const description = computed(() =>
  props.code === 403 ? 'У вашей роли нет доступа к этому разделу.' : 'Проверьте адрес или вернитесь на главную страницу.',
)
const home = computed(() => (auth.isAdmin ? '/admin' : auth.isCashier ? '/cashier' : '/login'))
</script>

<template>
  <main class="grid min-h-screen place-items-center bg-slate-50 px-4">
    <section class="text-center">
      <p class="text-sm font-bold uppercase tracking-widest text-brand">Ошибка {{ code }}</p>
      <h1 class="mt-3 text-3xl font-bold text-ink">{{ title }}</h1>
      <p class="mt-3 text-muted">{{ description }}</p>
      <RouterLink :to="home" class="mt-6 inline-block"><AppButton>Вернуться на главную</AppButton></RouterLink>
    </section>
  </main>
</template>
