import type { AttemptKind, AttemptSource, AttemptStatus, PagedResponse } from './api'

export interface StartRandomTrainingRequest {
  topicId: number
  questionCount: number
}

export interface SubmitAttemptAnswerRequest {
  selectedOptionPosition: number
}

export interface AttemptOptionResponse {
  position: number
  text: string
  isCorrect?: boolean
}

export interface AttemptQuestionResponse {
  id: number
  questionId: number | null
  position: number
  questionText: string
  options: AttemptOptionResponse[]
  selectedOptionPosition: number | null
  isCorrect?: boolean
  answeredAt: string | null
}

export interface AttemptResponse {
  id: number
  kind: AttemptKind
  source: AttemptSource
  status: AttemptStatus
  startedAt: string
  finishedAt: string | null
  questionCount: number
  timeLimitSeconds: number | null
  correctAnswers: number
  wrongAnswers: number
  isPassed: boolean | null
  questions: AttemptQuestionResponse[]
}

export interface AttemptHistoryItem {
  id: number
  kind: AttemptKind
  source: AttemptSource
  status: AttemptStatus
  ticketId: number | null
  ticketTitle: string | null
  examId: number | null
  examTitle: string | null
  startedAt: string
  finishedAt: string | null
  questionCount: number
  correctAnswers: number
  wrongAnswers: number
  isPassed: boolean | null
}

export type AttemptHistoryResponse = PagedResponse<AttemptHistoryItem>

export const AttemptKindValue = { Training: 0, Exam: 1 } as const
export const AttemptSourceValue = { Prepared: 0, Random: 1 } as const
export const AttemptStatusValue = { InProgress: 0, Completed: 1, Failed: 2, TimedOut: 3 } as const
