export type UserRole = 'Admin' | 'Cashier'

export interface LoginRequest {
  login: string
  password: string
}

export interface LoginResponse {
  token: string
  expiresAt: string
}

export interface CurrentUserResponse {
  id: number
  login: string
  fullName: string
  role: UserRole
  groupId: number | null
  groupName: string | null
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface ApiErrorBody {
  error?: string
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export type AttemptKind = 0 | 1
export type AttemptSource = 0 | 1
export type AttemptStatus = 0 | 1 | 2 | 3
