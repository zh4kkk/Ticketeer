import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getCurrentUser, login as loginRequest } from '@/api/auth'
import { ApiError } from '@/api/http'
import { clearSession, isSessionExpired, readSession, writeSession } from '@/api/session'
import type { CurrentUserResponse, LoginRequest } from '@/types/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const expiresAt = ref<string | null>(null)
  const user = ref<CurrentUserResponse | null>(null)
  const isInitializing = ref(true)
  const initializationError = ref<string | null>(null)
  let expiryTimer: ReturnType<typeof setTimeout> | undefined
  let generation = 0
  let restorePromise: Promise<void> | null = null

  const isAuthenticated = computed(
    () => Boolean(token.value && expiresAt.value && user.value && !isSessionExpired(expiresAt.value)),
  )
  const isAdmin = computed(() => isAuthenticated.value && user.value?.role === 'Admin')
  const isCashier = computed(() => isAuthenticated.value && user.value?.role === 'Cashier')

  function clearExpiryTimer(): void {
    if (expiryTimer !== undefined) clearTimeout(expiryTimer)
    expiryTimer = undefined
  }

  function clearState(clearStored = true): void {
    clearExpiryTimer()
    token.value = null
    expiresAt.value = null
    user.value = null
    if (clearStored) clearSession()
  }

  function scheduleExpiry(sessionGeneration: number, value: string): void {
    clearExpiryTimer()
    const delay = Date.parse(value) - Date.now()
    if (!Number.isFinite(delay) || delay <= 0) {
      if (sessionGeneration === generation) clearState()
      return
    }
    expiryTimer = setTimeout(() => {
      if (sessionGeneration === generation) clearState()
    }, delay)
  }

  function supportedUser(value: CurrentUserResponse): boolean {
    return value.role === 'Admin' || value.role === 'Cashier'
  }

  async function restoreSession(): Promise<void> {
    if (restorePromise) return restorePromise
    restorePromise = (async () => {
      isInitializing.value = true
      initializationError.value = null
      const stored = readSession()
      if (!stored || isSessionExpired(stored.expiresAt)) {
        clearState()
        isInitializing.value = false
        return
      }

      const sessionGeneration = generation
      token.value = stored.token
      expiresAt.value = stored.expiresAt
      try {
        const profile = await getCurrentUser()
        if (sessionGeneration !== generation) return
        if (!supportedUser(profile)) throw new ApiError('Сервер вернул неподдерживаемую роль.', 401)
        user.value = profile
        scheduleExpiry(sessionGeneration, stored.expiresAt)
      } catch (error) {
        if (sessionGeneration !== generation) return
        if (error instanceof ApiError && (error.status === 401 || error.status === 403)) {
          clearState()
        } else {
          token.value = null
          expiresAt.value = null
          user.value = null
          initializationError.value =
            error instanceof ApiError ? error.message : 'Не удалось проверить сохранённую сессию.'
        }
      } finally {
        if (sessionGeneration === generation) isInitializing.value = false
      }
    })().finally(() => {
      restorePromise = null
    })
    return restorePromise
  }

  async function login(credentials: LoginRequest): Promise<CurrentUserResponse> {
    const sessionGeneration = ++generation
    clearState()
    initializationError.value = null
    const response = await loginRequest(credentials)
    if (sessionGeneration !== generation) throw new ApiError('Сессия была изменена.', 0)
    if (isSessionExpired(response.expiresAt)) throw new ApiError('Сервер вернул уже истёкший токен.', 0)
    writeSession({ token: response.token, expiresAt: response.expiresAt })
    token.value = response.token
    expiresAt.value = response.expiresAt
    try {
      const profile = await getCurrentUser()
      if (sessionGeneration !== generation) throw new ApiError('Сессия была изменена.', 0)
      if (!supportedUser(profile)) throw new ApiError('Сервер вернул неподдерживаемую роль.', 401)
      user.value = profile
      scheduleExpiry(sessionGeneration, response.expiresAt)
      return profile
    } catch (error) {
      if (sessionGeneration === generation) clearState()
      throw error
    }
  }

  function logout(): void {
    generation += 1
    clearState()
    initializationError.value = null
    isInitializing.value = false
  }

  function markUnauthorized(): void {
    generation += 1
    clearState()
  }

  return {
    token,
    expiresAt,
    user,
    isInitializing,
    initializationError,
    isAuthenticated,
    isAdmin,
    isCashier,
    login,
    logout,
    restoreSession,
    markUnauthorized,
  }
})
