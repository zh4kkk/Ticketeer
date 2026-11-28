<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import AppAlert from '@/components/AppAlert.vue'
import AppButton from '@/components/AppButton.vue'
import { AuthProfileError, useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const login = ref('')
const password = ref('')
const isSubmitting = ref(false)
const errorMessage = ref('')
const isRetryingSession = ref(false)

const canSubmit = computed(() => login.value.trim().length > 0 && password.value.length > 0)

function messageFor(error: unknown): string {
  if (error instanceof AuthProfileError) return error.message
  if (error instanceof ApiError) {
    if (error.status === 401) return 'Неверный логин или пароль.'
    if (error.status === 0) return error.message
    return error.message
  }
  if (error instanceof Error) return error.message
  return 'Не удалось выполнить вход. Попробуйте ещё раз.'
}

function safeRedirect(): string {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : ''
  const home = auth.isAdmin ? '/admin' : '/cashier'
  if (!redirect || redirect.startsWith('//') || /^[a-z][a-z\d+.-]*:/i.test(redirect)) return home
  let resolved
  try {
    resolved = router.resolve(redirect)
  } catch {
    return home
  }
  const roles = resolved.meta.roles
  const isAllowed =
    resolved.meta.requiresAuth === true &&
    resolved.name !== 'login' &&
    resolved.name !== 'not-found' &&
    Boolean(auth.user && roles?.includes(auth.user.role))
  return isAllowed ? resolved.fullPath : home
}

async function retrySession(): Promise<void> {
  if (isSubmitting.value || isRetryingSession.value) return
  isRetryingSession.value = true
  errorMessage.value = ''
  try {
    await auth.retrySession()
    if (auth.isAuthenticated) await router.replace(safeRedirect())
  } catch (error) {
    errorMessage.value = messageFor(error)
  } finally {
    isRetryingSession.value = false
  }
}

async function submit(): Promise<void> {
  if (!canSubmit.value || isSubmitting.value || isRetryingSession.value) return
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
  <main class="grid min-h-screen place-items-center bg-canvas px-4 py-10">
    <section class="w-full max-w-md rounded-app border border-line bg-surface p-6 shadow-sm sm:p-8">
      <div class="mb-8">
        <p class="mb-2 text-sm font-semibold uppercase tracking-widest text-brand">Ticketeer</p>
        <h1 class="text-2xl font-bold text-ink">Вход в приложение</h1>
        <p class="mt-2 text-sm leading-6 text-muted">
          Обучение и проверка знаний железнодорожных билетных кассиров.
        </p>
      </div>
      <AppAlert
        v-if="route.query.reason === 'expired'"
        message="Срок действия сессии истёк. Войдите снова."
        variant="info"
        class="mb-4"
      />
      <AppAlert
        v-else-if="route.query.reason === 'unauthorized'"
        message="Сессия больше недействительна. Войдите снова."
        variant="info"
        class="mb-4"
      />
      <div v-if="auth.initializationError" class="mb-4 space-y-3">
        <AppAlert :message="auth.initializationError" variant="info" />
        <AppButton
          variant="secondary"
          :loading="isRetryingSession"
          :disabled="isSubmitting || isRetryingSession"
          @click="retrySession"
          >Повторить проверку сессии</AppButton
        >
      </div>
      <AppAlert v-if="errorMessage" :message="errorMessage" class="mb-4" />
      <form class="space-y-5" @submit.prevent="submit">
        <div>
          <label for="login" class="mb-1.5 block text-sm font-medium text-ink">Логин</label>
          <input
            id="login"
            v-model="login"
            name="login"
            type="text"
            autocomplete="username"
            maxlength="100"
            required
            class="app-input"
            :disabled="isSubmitting || isRetryingSession"
          />
        </div>
        <div>
          <label for="password" class="mb-1.5 block text-sm font-medium text-ink">Пароль</label>
          <input
            id="password"
            v-model="password"
            name="password"
            type="password"
            autocomplete="current-password"
            required
            class="app-input"
            :disabled="isSubmitting || isRetryingSession"
          />
        </div>
        <AppButton
          type="submit"
          :loading="isSubmitting"
          :disabled="!canSubmit || isRetryingSession"
          class="w-full"
          >Войти</AppButton
        >
      </form>
    </section>
  </main>
</template>
