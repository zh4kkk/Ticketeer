const TOKEN_KEY = 'ticketeer.session.token'
const EXPIRES_AT_KEY = 'ticketeer.session.expiresAt'

export interface StoredSession {
  token: string
  expiresAt: string
}

export function readSession(): StoredSession | null {
  try {
    const token = sessionStorage.getItem(TOKEN_KEY)
    const expiresAt = sessionStorage.getItem(EXPIRES_AT_KEY)
    if (!token || !expiresAt) return null
    return { token, expiresAt }
  } catch {
    return null
  }
}

export function writeSession(session: StoredSession): void {
  sessionStorage.setItem(TOKEN_KEY, session.token)
  sessionStorage.setItem(EXPIRES_AT_KEY, session.expiresAt)
}

export function clearSession(): void {
  sessionStorage.removeItem(TOKEN_KEY)
  sessionStorage.removeItem(EXPIRES_AT_KEY)
}

export function isSessionExpired(expiresAt: string): boolean {
  const timestamp = Date.parse(expiresAt)
  return !Number.isFinite(timestamp) || timestamp <= Date.now()
}
