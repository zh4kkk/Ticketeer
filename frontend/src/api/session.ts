const SESSION_KEY = 'ticketeer.session'
const LEGACY_TOKEN_KEY = 'ticketeer.session.token'
const LEGACY_EXPIRES_AT_KEY = 'ticketeer.session.expiresAt'

export interface StoredSession {
  token: string
  expiresAt: string
}

let sessionGeneration = 0

export function setSessionGeneration(value: number): void {
  sessionGeneration = value
}

export function getSessionGeneration(): number {
  return sessionGeneration
}

export class SessionStorageError extends Error {
  constructor(message = 'Не удалось сохранить сессию в браузере.') {
    super(message)
    this.name = 'SessionStorageError'
  }
}

function isValidSession(value: unknown): value is StoredSession {
  if (typeof value !== 'object' || value === null) return false
  const candidate = value as Record<string, unknown>
  return typeof candidate.token === 'string' && candidate.token.trim().length > 0 && typeof candidate.expiresAt === 'string' && Number.isFinite(Date.parse(candidate.expiresAt))
}

export function readSession(): StoredSession | null {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY)
    if (raw) {
      const parsed: unknown = JSON.parse(raw)
      return isValidSession(parsed) ? parsed : null
    }
    const token = sessionStorage.getItem(LEGACY_TOKEN_KEY)
    const expiresAt = sessionStorage.getItem(LEGACY_EXPIRES_AT_KEY)
    if (!token || !expiresAt) return null
    const legacy = { token, expiresAt }
    return isValidSession(legacy) ? legacy : null
  } catch {
    return null
  }
}

export function writeSession(session: StoredSession): void {
  if (!isValidSession(session)) throw new SessionStorageError('Сервер вернул некорректные данные сессии.')
  try {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(session))
    sessionStorage.removeItem(LEGACY_TOKEN_KEY)
    sessionStorage.removeItem(LEGACY_EXPIRES_AT_KEY)
  } catch {
    try { sessionStorage.removeItem(SESSION_KEY) } catch { /* Storage may be unavailable. */ }
    try { sessionStorage.removeItem(LEGACY_TOKEN_KEY) } catch { /* Storage may be unavailable. */ }
    try { sessionStorage.removeItem(LEGACY_EXPIRES_AT_KEY) } catch { /* Storage may be unavailable. */ }
    throw new SessionStorageError()
  }
}

export function clearSession(): void {
  try {
    sessionStorage.removeItem(SESSION_KEY)
    sessionStorage.removeItem(LEGACY_TOKEN_KEY)
    sessionStorage.removeItem(LEGACY_EXPIRES_AT_KEY)
  } catch {
    // Logout must still clear reactive state when browser storage is unavailable.
  }
}

export function isSessionExpired(expiresAt: string): boolean {
  const timestamp = Date.parse(expiresAt)
  return !Number.isFinite(timestamp) || timestamp <= Date.now()
}
