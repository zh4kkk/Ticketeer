import { readSession } from './session'
import type { ApiErrorBody } from '@/types/api'

export class ApiError extends Error {
  readonly status: number
  readonly validationErrors: Record<string, string[]>

  constructor(message: string, status: number, validationErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.validationErrors = validationErrors
  }
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

async function readBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type') ?? ''
  if (response.status === 204 || !contentType.includes('json')) return null
  try {
    return await response.json()
  } catch {
    return null
  }
}

function errorFromResponse(status: number, body: unknown): ApiError {
  const payload = isObject(body) ? (body as ApiErrorBody) : {}
  const validationErrors = payload.errors ?? {}
  const message =
    payload.error ??
    payload.detail ??
    payload.title ??
    (status >= 500 ? 'Сервер временно недоступен.' : `Не удалось выполнить запрос (HTTP ${status}).`)
  return new ApiError(message, status, validationErrors)
}

export async function request<T>(
  url: string,
  options: RequestInit & { json?: unknown } = {},
): Promise<T> {
  const { json, headers: providedHeaders, ...init } = options
  const headers = new Headers(providedHeaders)
  if (json !== undefined) {
    headers.set('Content-Type', 'application/json')
    init.body = JSON.stringify(json)
  }
  const session = readSession()
  if (session?.token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${session.token}`)
  }

  let response: Response
  try {
    response = await fetch(url, { ...init, headers })
  } catch {
    throw new ApiError('Не удалось связаться с сервером. Проверьте подключение и повторите попытку.', 0)
  }

  const body = await readBody(response)
  if (!response.ok) throw errorFromResponse(response.status, body)
  if (response.status === 204 || body === null) return undefined as T
  return body as T
}

export const api = {
  get: <T>(url: string, signal?: AbortSignal) => request<T>(url, { method: 'GET', signal }),
  post: <T>(url: string, json?: unknown, signal?: AbortSignal) =>
    request<T>(url, { method: 'POST', json, signal }),
  put: <T>(url: string, json?: unknown, signal?: AbortSignal) =>
    request<T>(url, { method: 'PUT', json, signal }),
  delete: <T>(url: string, signal?: AbortSignal) => request<T>(url, { method: 'DELETE', signal }),
}
