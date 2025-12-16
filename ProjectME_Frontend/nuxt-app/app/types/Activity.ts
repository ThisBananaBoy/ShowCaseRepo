export interface Activity {
  id: string
  project_id?: string
  task_id?: string
  milestone_id?: string
  user_id: string
  activity_type: 'task_created' | 'task_completed' | 'milestone_completed' | 'project_updated' | 'time_logged'
  entity_type: 'task' | 'milestone' | 'project' | 'time_entry'
  entity_id: string
  description: string
  metadata_json?: string
  created_at: Date
}

export type CreateActivityDto = Omit<Activity, 'id' | 'created_at' | 'user_id'>
export type UpdateActivityDto = Partial<Omit<Activity, 'id' | 'created_at'>>
