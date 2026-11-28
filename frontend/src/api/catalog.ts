import { api, ApiError } from './http'
import type { PagedResponse } from '@/types/api'
import type {
  CatalogExamResponse,
  CatalogQuestionListResponse,
  CatalogQuestionResponse,
  CatalogTicketListResponse,
  CatalogTicketResponse,
  CatalogTopicResponse,
  CatalogListOptions,
  QuestionListOptions,
  TicketListOptions,
} from '@/types/catalog'

function validId(id: number): number {
  if (!Number.isSafeInteger(id) || id <= 0 || id > 2_147_483_647)
    throw new ApiError('Некорректный идентификатор материала.', 400)
  return id
}

function searchValue(value: string | undefined): string | undefined {
  const normalized = value?.trim().slice(0, 200)
  return normalized || undefined
}

function queryString(values: Record<string, string | number | boolean | undefined>): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  const query = params.toString()
  return query ? `?${query}` : ''
}

export function getCatalogTopics(
  options: Pick<CatalogListOptions, 'search' | 'signal'> = {},
): Promise<CatalogTopicResponse[]> {
  return api.get<CatalogTopicResponse[]>(
    `/api/catalog/topics${queryString({ search: searchValue(options.search) })}`,
    { signal: options.signal },
  )
}

export function getCatalogTickets(
  options: TicketListOptions = {},
): Promise<PagedResponse<CatalogTicketListResponse>> {
  return api.get<PagedResponse<CatalogTicketListResponse>>(
    `/api/catalog/tickets${queryString({
      search: searchValue(options.search),
      topicId: options.topicId,
      isCompleted: options.isCompleted,
      page: options.page,
      pageSize: options.pageSize,
    })}`,
    { signal: options.signal },
  )
}

export function getCatalogTicket(id: number, signal?: AbortSignal): Promise<CatalogTicketResponse> {
  return api.get<CatalogTicketResponse>(`/api/catalog/tickets/${validId(id)}`, { signal })
}

export function getCatalogExams(
  options: CatalogListOptions = {},
): Promise<PagedResponse<CatalogExamResponse>> {
  return api.get<PagedResponse<CatalogExamResponse>>(
    `/api/catalog/exams${queryString({
      search: searchValue(options.search),
      page: options.page,
      pageSize: options.pageSize,
    })}`,
    { signal: options.signal },
  )
}

export function getCatalogExam(id: number, signal?: AbortSignal): Promise<CatalogExamResponse> {
  return api.get<CatalogExamResponse>(`/api/catalog/exams/${validId(id)}`, { signal })
}

export function getCatalogQuestions(
  options: QuestionListOptions = {},
): Promise<PagedResponse<CatalogQuestionListResponse>> {
  return api.get<PagedResponse<CatalogQuestionListResponse>>(
    `/api/catalog/questions${queryString({
      search: searchValue(options.search),
      topicId: options.topicId,
      page: options.page,
      pageSize: options.pageSize,
    })}`,
    { signal: options.signal },
  )
}

export function getCatalogQuestion(
  id: number,
  signal?: AbortSignal,
): Promise<CatalogQuestionResponse> {
  return api.get<CatalogQuestionResponse>(`/api/catalog/questions/${validId(id)}`, { signal })
}
