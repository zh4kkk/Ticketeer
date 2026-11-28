const INT32_MAX = 2_147_483_647
const DEFAULT_PAGE_SIZE = 20

export function queryString(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

export function parsePositiveInt(value: unknown, fallback?: number): number | undefined {
  if (typeof value !== 'string' || !/^\d+$/.test(value)) return fallback
  const parsed = Number(value)
  return Number.isSafeInteger(parsed) && parsed > 0 && parsed <= INT32_MAX ? parsed : fallback
}

export function parsePage(value: unknown, pageSize = DEFAULT_PAGE_SIZE): number {
  const parsed = parsePositiveInt(value)
  const maxPage = Math.floor(INT32_MAX / pageSize) + 1
  return parsed !== undefined && parsed <= maxPage ? parsed : 1
}

export function parseBoolean(value: unknown): boolean | undefined {
  if (value === 'true') return true
  if (value === 'false') return false
  return undefined
}

export function parseSearch(value: unknown): string {
  return queryString(value).trim().slice(0, 200)
}
