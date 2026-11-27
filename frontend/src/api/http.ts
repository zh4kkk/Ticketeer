import { getSessionGeneration, readSession } from './session'
import type { ApiErrorBody } from '@/types/api'

export type ResponseType = 'json' | 'empty'

export interface RequestOptions extends RequestInit {
  json?: unknown
  responseType?: ResponseType
  auth?: boolean
}

export interface UnauthorizedRequest {
  token: string
  generation: number
  url: string
}

export class ApiError extends Error {
  readonly status: number
  readonly validationErrors: Record<string, string[]>
  readonly kind: 'http' | 'network' | 'parse'

  constructor(
    message: string,
    status: number,
    validationErrors: Record<string, string[]> = {},
    kind: 'http' | 'network' | 'parse' = status === 0 ? 'network' : 'http',
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.validationErrors = validationErrors
    this.kind = kind
  }
}

export class ApiAbortError extends Error {
  constructor() {
    super('Запрос был отменён.')
    this.name = 'ApiAbortError'
  }
}

let unauthorizedHandler: ((request: UnauthorizedRequest) => void) | null = null

export function registerUnauthorizedHandler(handler: (request: UnauthorizedRequest) => void): void {
  unauthorizedHandler = handler
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function normalizeApiUrl(input: string): string {
  if (!input || input.includes('\\') || input.startsWith('//') || /^[a-z][a-z\d+.-]*:/i.test(input)) {
    throw new ApiError('HTTP-клиент принимает только относительные адреса API.', 0)
  }
  const origin = globalThis.location?.origin ?? 'http://localhost'
  let url: URL
  try {
    url = new URL(input, origin)
  } catch {
    throw new ApiError('Некорректный адрес API.', 0)
  }
  if (url.origin !== origin || (url.pathname !== '/api' && !url.pathname.startsWith('/api/'))) {
    throw new ApiError('HTTP-клиент может обращаться только к API текущего приложения.', 0)
  }
  return `${url.pathname}${url.search}`
}

interface ParsedBody {
  empty: boolean
  value: unknown
}

async function parseBody(response: Response): Promise<ParsedBody> {
  const text = await response.text()
  if (!text.trim()) return { empty: true, value: null }
  try {
    return { empty: false, value: JSON.parse(text) }
  } catch {
    return { empty: false, value: null }
  }
}

function errorFromResponse(status: number, body: ParsedBody): ApiError {
  const payload = isObject(body.value) ? (body.value as ApiErrorBody) : {}
  const validationErrors = payload.errors ?? {}
  const message =
    payload.error ??
    payload.detail ??
    payload.title ??
    (status >= 500 ? 'Сервер временно недоступен.' : `Не удалось выполнить запрос (HTTP ${status}).`)
  return new ApiError(message, status, validationErrors)
}

function isAbortError(error: unknown): boolean {
  return isObject(error) && error.name === 'AbortError'
}

export async function request<T>(url: string, options?: RequestOptions): Promise<T>
export async function request(url: string, options: RequestOptions & { responseType: 'empty' }): Promise<void>
export async function request<T>(url: string, options: RequestOptions = {}): Promise<T | void> {
  const normalizedUrl = normalizeApiUrl(url)
  const { json, headers: providedHeaders, responseType = 'json', auth = true, ...init } = options
  const headers = new Headers(providedHeaders)
  if (json !== undefined) {
    headers.set('Content-Type', 'application/json')
    init.body = JSON.stringify(json)
  }

  const session = auth ? readSession() : null
  const requestGeneration = getSessionGeneration()
  headers.delete('Authorization')
  if (session?.token) headers.set('Authorization', `Bearer ${session.token}`)

  let response: Response
  try {
    response = await fetch(normalizedUrl, { ...init, headers })
  } catch (error) {
    if (isAbortError(error)) throw new ApiAbortError()
    throw new ApiError('Не удалось связаться с сервером. Проверьте подключение и повторите попытку.', 0)
  }

  let body: ParsedBody
  try {
    body = await parseBody(response)
  } catch {
    throw new ApiError('Не удалось прочитать ответ сервера.', response.status, {}, 'parse')
  }
  if (!response.ok) {
    if (response.status === 401 && session?.token) unauthorizedHandler?.({ token: session.token, generation: requestGeneration, url: normalizedUrl })
    throw errorFromResponse(response.status, body)
  }
  if (responseType === 'empty') return
  if (response.status === 204 || body.empty) {
    throw new ApiError('Сервер вернул пустой ответ вместо JSON.', response.status, {}, 'parse')
  }
  if (body.value === null) throw new ApiError('Сервер вернул некорректный JSON.', response.status, {}, 'parse')
  return body.value as T
}

export const api = {
  get: <T>(url: string, options: Omit<RequestOptions, 'method' | 'json'> = {}) =>
    request<T>(url, { ...options, method: 'GET' }),
  post: <T>(url: string, json?: unknown, options: Omit<RequestOptions, 'method' | 'json'> = {}) =>
    request<T>(url, { ...options, method: 'POST', json }),
  put: <T>(url: string, json?: unknown, options: Omit<RequestOptions, 'method' | 'json'> = {}) =>
    request<T>(url, { ...options, method: 'PUT', json }),
  delete: (url: string, options: Omit<RequestOptions, 'method' | 'json'> = {}) =>
    request(url, { ...options, method: 'DELETE', responseType: options.responseType ?? 'empty' }),
}
