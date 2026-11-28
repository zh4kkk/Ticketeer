import { ApiError, api } from './http'
import type { CurrentUserResponse, LoginRequest, LoginResponse, UserRole } from '@/types/api'

function isRole(value: unknown): value is UserRole {
  return value === 'Admin' || value === 'Cashier'
}

function isLoginResponse(value: unknown): value is LoginResponse {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as Record<string, unknown>).token === 'string' &&
    typeof (value as Record<string, unknown>).expiresAt === 'string'
  )
}

function isCurrentUser(value: unknown): value is CurrentUserResponse {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as Record<string, unknown>
  return (
    typeof candidate.id === 'number' &&
    typeof candidate.login === 'string' &&
    typeof candidate.fullName === 'string' &&
    isRole(candidate.role) &&
    (typeof candidate.groupId === 'number' || candidate.groupId === null) &&
    (typeof candidate.groupName === 'string' || candidate.groupName === null)
  )
}

export function login(request: LoginRequest): Promise<LoginResponse> {
  return api.post<unknown>('/api/auth/login', request, { auth: false }).then((value) => {
    if (!isLoginResponse(value))
      throw new ApiError('Сервер вернул некорректный ответ входа.', 0, {}, 'parse')
    return value
  })
}

export function getCurrentUser(): Promise<CurrentUserResponse> {
  return api.get<unknown>('/api/auth/me', { auth: true }).then((value) => {
    if (!isCurrentUser(value))
      throw new ApiError('Сервер вернул некорректный профиль пользователя.', 0, {}, 'parse')
    return value
  })
}
