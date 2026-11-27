import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { getCurrentUser, login as loginRequest } from '@/api/auth'
import { ApiError } from '@/api/http'
import { clearSession, isSessionExpired, readSession, SessionStorageError, setSessionGeneration, writeSession } from '@/api/session'
import type { CurrentUserResponse, LoginRequest } from '@/types/api'

const MAX_TIMEOUT = 2_147_000_000

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const expiresAt = ref<string | null>(null)
  const user = ref<CurrentUserResponse | null>(null)
  const isInitializing = ref(true)
  const isInitialized = ref(false)
  const initializationError = ref<string | null>(null)
  const sessionEndReason = ref<'expired' | 'unauthorized' | null>(null)
  let expiryTimer: ReturnType<typeof setTimeout> | undefined
  let generation = 0
  let initializePromise: Promise<void> | null = null
  setSessionGeneration(generation)

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

  function endSession(reason: 'expired' | 'unauthorized'): void {
    generation += 1
    setSessionGeneration(generation)
    clearState()
    sessionEndReason.value = reason
  }

  function scheduleExpiry(sessionGeneration: number, value: string): void {
    clearExpiryTimer()
    const timestamp = Date.parse(value)
    if (!Number.isFinite(timestamp)) {
      endSession('expired')
      return
    }
    const remaining = timestamp - Date.now()
    if (remaining <= 0) {
      if (sessionGeneration === generation) endSession('expired')
      return
    }
    expiryTimer = setTimeout(() => {
      if (sessionGeneration !== generation) return
      if (isSessionExpired(value)) endSession('expired')
      else scheduleExpiry(sessionGeneration, value)
    }, Math.min(remaining, MAX_TIMEOUT))
  }

  function supportedUser(value: CurrentUserResponse): boolean {
    return value.role === 'Admin' || value.role === 'Cashier'
  }

  async function initializeSession(force = false): Promise<void> {
    if (isInitialized.value && !force) return
    if (initializePromise) return initializePromise
    initializePromise = (async () => {
      const operationGeneration = generation
      isInitializing.value = true
      initializationError.value = null
      sessionEndReason.value = null
      const stored = readSession()
      if (!stored || isSessionExpired(stored.expiresAt)) {
        clearState()
        if (stored) sessionEndReason.value = 'expired'
        isInitialized.value = true
        isInitializing.value = false
        return
      }

      token.value = stored.token
      expiresAt.value = stored.expiresAt
      try {
        const profile = await getCurrentUser()
        if (operationGeneration !== generation) return
        if (!supportedUser(profile)) throw new ApiError('Сервер вернул неподдерживаемую роль.', 401)
        user.value = profile
        scheduleExpiry(operationGeneration, stored.expiresAt)
      } catch (error) {
        if (operationGeneration !== generation) return
        if (error instanceof ApiError && error.status === 401) {
          endSession('unauthorized')
        } else if (error instanceof ApiError && (error.kind === 'network' || error.status === 403 || error.status >= 500)) {
          user.value = null
          initializationError.value = error.status === 403
            ? 'Сервер временно запретил проверку сессии. Повторите попытку позже.'
            : 'Не удалось проверить сессию. Проверьте подключение и повторите попытку.'
        } else if (error instanceof ApiError && error.kind === 'parse') {
          clearState()
          initializationError.value = error.message
        } else {
          clearState()
          initializationError.value = error instanceof Error ? error.message : 'Не удалось восстановить сессию.'
        }
      } finally {
        isInitialized.value = true
        isInitializing.value = false
      }
    })().finally(() => {
      initializePromise = null
    })
    return initializePromise
  }

  async function retrySession(): Promise<void> {
    await initializeSession(true)
  }

  async function login(credentials: LoginRequest): Promise<CurrentUserResponse> {
    const sessionGeneration = ++generation
    setSessionGeneration(generation)
    clearState()
    initializationError.value = null
    sessionEndReason.value = null
    const response = await loginRequest(credentials)
    if (sessionGeneration !== generation) throw new ApiError('Сессия была изменена.', 0)
    if (isSessionExpired(response.expiresAt)) throw new ApiError('Сервер вернул уже истёкший токен.', 0)
    try {
      writeSession({ token: response.token, expiresAt: response.expiresAt })
      token.value = response.token
      expiresAt.value = response.expiresAt
      const profile = await getCurrentUser()
      if (sessionGeneration !== generation) throw new ApiError('Сессия была изменена.', 0)
      if (!supportedUser(profile)) throw new ApiError('Сервер вернул неподдерживаемую роль.', 401)
      user.value = profile
      isInitialized.value = true
      scheduleExpiry(sessionGeneration, response.expiresAt)
      return profile
    } catch (error) {
      if (sessionGeneration === generation) clearState()
      if (error instanceof SessionStorageError) throw error
      throw error
    }
  }

  function logout(): void {
    generation += 1
    setSessionGeneration(generation)
    clearState()
    initializationError.value = null
    sessionEndReason.value = null
    isInitialized.value = true
    isInitializing.value = false
  }

  function handleUnauthorized(requestToken: string, requestGeneration: number): void {
    if (!token.value || requestToken !== token.value || requestGeneration !== generation) return
    endSession('unauthorized')
  }

  return {
    token,
    expiresAt,
    user,
    isInitializing,
    isInitialized,
    initializationError,
    sessionEndReason,
    isAuthenticated,
    isAdmin,
    isCashier,
    login,
    logout,
    initializeSession,
    retrySession,
    handleUnauthorized,
  }
})
