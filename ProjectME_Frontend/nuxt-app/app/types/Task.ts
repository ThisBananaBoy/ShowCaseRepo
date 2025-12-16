export interface Task {
  id: string
  user_id: string
  project_id?: string
  milestone_id?: string
  assigned_user_id?: string
  name: string
  status?: 'todo' | 'in_progress' | 'completed'
  priority?: number
  start_time?: Date
  end_time?: Date
  due_date?: Date
  completed_at?: Date
}

export type CreateTaskDto = Omit<Task, 'id' | 'completed_at' | 'user_id'>
export type UpdateTaskDto = Partial<Omit<Task, 'id'>>


