export interface CatalogTopicResponse {
  id: number
  name: string
  description: string | null
  questionCount: number
}

export interface CatalogTopicReference {
  id: number
  name: string
}

export interface CatalogTicketListResponse {
  id: number
  title: string
  description: string | null
  questionCount: number
  isCompleted: boolean
}

export interface CatalogTicketResponse extends CatalogTicketListResponse {
  topics: CatalogTopicReference[]
}

export interface CatalogExamResponse {
  id: number
  title: string
  description: string | null
  questionCount: number
  timeLimitSeconds: number
  maxWrongAnswers: number
}

export interface CatalogQuestionListResponse {
  id: number
  topicId: number
  topicName: string
  text: string
}

export interface CatalogAnswerOptionResponse {
  position: number
  text: string
}

export interface CatalogQuestionResponse extends CatalogQuestionListResponse {
  answerOptions: CatalogAnswerOptionResponse[]
}

export interface CatalogListOptions {
  search?: string
  page?: number
  pageSize?: number
  signal?: AbortSignal
}

export interface TicketListOptions extends CatalogListOptions {
  topicId?: number
  isCompleted?: boolean
}

export interface QuestionListOptions extends CatalogListOptions {
  topicId?: number
}
