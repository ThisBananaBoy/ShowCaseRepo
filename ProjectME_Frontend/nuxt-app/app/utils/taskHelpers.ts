import { getWeek } from 'date-fns'
import { de } from 'date-fns/locale'
import type { Task } from '~/types/Task'

export function isTaskCompleted(task: Task): boolean {
  return !!task.completed_at || task.status === 'completed'
}

export function isTaskToday(task: Task): boolean {
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  
  if (task.start_time) {
    const startDate = new Date(task.start_time)
    startDate.setHours(0, 0, 0, 0)
    if (startDate.getTime() === today.getTime()) return true
  }
  
  if (task.due_date) {
    const dueDate = new Date(task.due_date)
    dueDate.setHours(0, 0, 0, 0)
    if (dueDate.getTime() === today.getTime()) return true
  }
  
  return false
}

export function getTaskWeek(task: Task): number | undefined {
  if (!task.due_date) return undefined
  const date = task.due_date instanceof Date ? task.due_date : new Date(task.due_date)
  return getWeek(date, { locale: de, weekStartsOn: 1 })
}

export function getTaskTodayStatus(task: Task): 'open' | 'completed' | undefined {
  if (!isTaskToday(task)) return undefined
  
  if (task.completed_at) {
    const completedDate = new Date(task.completed_at)
    completedDate.setHours(0, 0, 0, 0)
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    
    if (completedDate.getTime() === today.getTime()) {
      return 'completed'
    }
  }
  
  return 'open'
}

