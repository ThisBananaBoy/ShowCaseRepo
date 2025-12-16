export interface TimeEntry {
  id: string
  project_id?: string
  task_id?: string
  user_id: string
  start_time: Date
  end_time?: Date
  created_at: Date
}



export type CreateTimeEntryDto = Omit<TimeEntry, 'id' | 'created_at' | 'user_id'>

export type UpdateTimeEntryDto = Partial<Omit<TimeEntry, 'id' | 'created_at'>>
