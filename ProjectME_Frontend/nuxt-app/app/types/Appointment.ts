export interface Appointment {
  id: string
  user_id: string
  title: string
  description?: string
  start_time: Date
  end_time: Date
  location?: string
  project_id?: string
  task_id?: string
  color?: string
}

export type CreateAppointmentDto = Omit<Appointment, 'id' | 'user_id'>

export type UpdateAppointmentDto = Partial<Omit<Appointment, 'id'>>
