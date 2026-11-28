import { api, ApiError } from './http'
import type { PagedResponse } from '@/types/api'
import type {
  AttemptHistoryItem,
  AttemptResponse,
  StartRandomTrainingRequest,
  SubmitAttemptAnswerRequest,
} from '@/types/attempts'

const INT32_MAX = 2_147_483_647

function validId(id: number): number {
  if (!Number.isSafeInteger(id) || id <= 0 || id > INT32_MAX)
    throw new ApiError('Некорректный идентификатор попытки.', 400)
  return id
}

export function startTrainingTicket(ticketId: number): Promise<AttemptResponse> {
  return api.post<AttemptResponse>(`/api/attempts/training/tickets/${validId(ticketId)}`)
}

export function startRandomTraining(request: StartRandomTrainingRequest): Promise<AttemptResponse> {
  return api.post<AttemptResponse>('/api/attempts/training/random', request)
}

export function startExam(examId: number): Promise<AttemptResponse> {
  return api.post<AttemptResponse>(`/api/attempts/exams/${validId(examId)}`)
}

export function startRandomExam(): Promise<AttemptResponse> {
  return api.post<AttemptResponse>('/api/attempts/exams/random')
}

export function getAttempt(id: number, signal?: AbortSignal): Promise<AttemptResponse> {
  return api.get<AttemptResponse>(`/api/attempts/${validId(id)}`, { signal })
}

export function submitAttemptAnswer(
  id: number,
  questionId: number,
  request: SubmitAttemptAnswerRequest,
): Promise<AttemptResponse> {
  return api.post<AttemptResponse>(
    `/api/attempts/${validId(id)}/questions/${validId(questionId)}/answer`,
    request,
  )
}

export function getInProgressAttempts(
  signal?: AbortSignal,
): Promise<PagedResponse<AttemptHistoryItem>> {
  return api.get<PagedResponse<AttemptHistoryItem>>('/api/attempts?page=1&pageSize=20&status=0', {
    signal,
  })
}
