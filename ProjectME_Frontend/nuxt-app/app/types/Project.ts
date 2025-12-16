
export enum StatusTypes {
  ACTIVE = 'active',
  PAUSED = 'paused',
  COMPLETED = 'completed',
  ARCHIVED = 'archived'
}

export enum DeadlineType {
  PROJECT = 'project',
  MILESTONE = 'milestone',
  TASK = 'task'
}

export interface Deadline {
  id: string
  deadline_date: Date
  reason?: string
  created_at: Date
  type: DeadlineType

  project_id?: string
  milestone_id?: string
  task_id?: string

  owner_name?: string  // Name des Projects/Milestone/Task
}
export interface Milestone {
  id: string
  project_id: string
  name: string
  description: string
  deadlines: readonly Deadline[]
  start_date?: Date
  last_deadline_date?: Date
  completed_at?: Date
}


export interface Project {
  id: string
  user_id: string
  name: string
  description: string
  status: StatusTypes
  start_date: Date
  last_deadline_date?: Date
  completed_at?: Date
  deadlines: readonly Deadline[]
  milestones: readonly Milestone[]
}

export type CreateProjectDto = Omit<Project, 'id' | 'completed_at' | 'project_activities' | 'time_entries' | 'tasks' | 'milestones' | 'user_id' | 'deadlines'>

export type UpdateProjectDto = Partial<Omit<Project, 'id'>>
