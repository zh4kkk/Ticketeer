<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import AppAlert from '@/components/AppAlert.vue'
import AppButton from '@/components/AppButton.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const login = ref('')
const password = ref('')
const isSubmitting = ref(false)
const errorMessage = ref('')

const canSubmit = computed(() => login.value.trim().length > 0 && password.value.length > 0)

function messageFor(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401) return 'Неверный логин или пароль.'
    if (error.status === 0) return error.message
    return error.message
  }
  return 'Не удалось выполнить вход. Попробуйте ещё раз.'
}

function safeRedirect(): string {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : ''
  const allowedPrefix = auth.isAdmin ? '/admin' : '/cashier'
  return redirect.startsWith(allowedPrefix) && !redirect.startsWith('//') ? redirect : allowedPrefix
}

async function submit(): Promise<void> {
  if (!canSubmit.value || isSubmitting.value) return
  isSubmitting.value = true
  errorMessage.value = ''
  try {
    await auth.login({ login: login.value.trim(), password: password.value })
    await router.replace(safeRedirect())
  } catch (error) {
    errorMessage.value = messageFor(error)
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <main class="grid min-h-screen place-items-center bg-slate-50 px-4 py-10">
    <section class="w-full max-w-md rounded-app border border-line bg-surface p-6 shadow-sm sm:p-8">
      <div class="mb-8">
        <p class="mb-2 text-sm font-semibold uppercase tracking-widest text-brand">Ticketeer</p>
        <h1 class="text-2xl font-bold text-ink">Вход в приложение</h1>
        <p class="mt-2 text-sm leading-6 text-muted">Обучение и проверка знаний железнодорожных билетных кассиров.</p>
      </div>
      <AppAlert v-if="auth.initializationError" :message="auth.initializationError" variant="info" class="mb-4" />
      <AppAlert v-if="errorMessage" :message="errorMessage" class="mb-4" />
      <form class="space-y-5" @submit.prevent="submit">
        <div>
          <label for="login" class="mb-1.5 block text-sm font-medium text-ink">Логин</label>
          <input id="login" v-model="login" name="login" type="text" autocomplete="username" maxlength="100" required class="app-input" :disabled="isSubmitting" />
        </div>
        <div>
          <label for="password" class="mb-1.5 block text-sm font-medium text-ink">Пароль</label>
          <input id="password" v-model="password" name="password" type="password" autocomplete="current-password" required class="app-input" :disabled="isSubmitting" />
        </div>
        <AppButton type="submit" :loading="isSubmitting" :disabled="!canSubmit" class="w-full">Войти</AppButton>
      </form>
    </section>
  </main>
</template>
