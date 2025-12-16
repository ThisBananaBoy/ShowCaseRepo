export interface RecurringTask {
  id: string
  user_id: string
  name: string
  assigned_dates: Date[]
  created_at: Date
}

export type CreateRecurringTaskDto = Omit<RecurringTask, 'id' | 'user_id'>

export type UpdateRecurringTaskDto = Partial<Omit<RecurringTask, 'id'>>
