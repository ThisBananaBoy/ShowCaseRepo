import { defineStore } from 'pinia'
import { useAuthFetch } from '~/composables/useAuthFetch'
import type { CreateRecurringTaskDto, RecurringTask, UpdateRecurringTaskDto } from '~/types/RecurringTask'

export const useRecurringTaskStore = defineStore('recurringTasks', () => {
  const recurringTasks = ref<RecurringTask[]>([])
  const loading = ref(false)
  const error = ref<Error | null>(null)

  const isCreating = ref(false)
  const isUpdating = ref(false)
  const isDeleting = ref(false)

  const normalizeDayKey = (value: Date | string) => {
    const date = value instanceof Date ? new Date(value) : new Date(value)
    if (isNaN(date.getTime())) return null
    date.setHours(0, 0, 0, 0)
    return date.getTime()
  }

  const getAllRecurringTasks = () => recurringTasks.value
  const getActiveRecurringTasks = () => recurringTasks.value

  const getRecurringTasksByDate = (day: Date) => {
    const target = normalizeDayKey(day)
    if (target === null) return []
    return recurringTasks.value.filter(task =>
      task.assigned_dates.some(date => normalizeDayKey(date) === target)
    )
  }

  const getRecurringTasksByProject = (_projectId: string) => recurringTasks.value

  const fetchRecurringTasks = async () => {
    loading.value = true
    error.value = null

    try {
      const { data, error: fetchError } = await useAuthFetch<RecurringTask[]>('/recurring-tasks')

      if (fetchError.value) {
        error.value = fetchError.value
        return
      }

      recurringTasks.value = data.value || []
    } catch (e: unknown) {
      error.value = e as Error
      console.error('Fetch recurring tasks failed:', e)
    } finally {
      loading.value = false
    }
  }

  const createRecurringTask = async (payload: CreateRecurringTaskDto) => {
    isCreating.value = true
    error.value = null

    // Optimistic Update: Erstelle temporäre RecurringTask mit generierter ID
    const tempId = `temp-${Date.now()}`
    const optimisticTask: RecurringTask = {
      ...payload,
      id: tempId,
      created_at: new Date()
    } as RecurringTask

    // Optimistisch hinzufügen
    recurringTasks.value = [...recurringTasks.value, optimisticTask]

    try {
      const { data: newTask, error: fetchError } = await useAuthFetch<RecurringTask>('/recurring-tasks', {
        method: 'POST',
        body: payload
      })

      if (fetchError.value) {
        // Rollback: Entferne optimistische RecurringTask
        recurringTasks.value = recurringTasks.value.filter(t => t.id !== tempId)
        error.value = fetchError.value
        return
      }

      if (newTask.value) {
        // Ersetze optimistische RecurringTask mit Server-Response
        const index = recurringTasks.value.findIndex(t => t.id === tempId)
        if (index !== -1) {
          recurringTasks.value[index] = newTask.value
        }
      }

      return newTask.value
    } catch (e: unknown) {
      // Rollback: Entferne optimistische RecurringTask
      recurringTasks.value = recurringTasks.value.filter(t => t.id !== tempId)
      error.value = e as Error
      return
    } finally {
      isCreating.value = false
    }
  }

  const updateRecurringTask = async (id: string, updates: UpdateRecurringTaskDto) => {
    isUpdating.value = true
    error.value = null

    const index = recurringTasks.value.findIndex(t => t.id === id)
    if (index === -1) {
      isUpdating.value = false
      return
    }

    // Speichere ursprünglichen Zustand für Rollback
    const originalTask = { ...recurringTasks.value[index] }

    // Optimistic Update: Wende Updates sofort an
    const { id: _, ...updatesWithoutId } = updates as RecurringTask
    recurringTasks.value[index] = { ...recurringTasks.value[index], ...updatesWithoutId, id }

    try {
      const { data: updatedTask, error: fetchError } = await useAuthFetch<RecurringTask>(`/recurring-tasks/${id}`, {
        method: 'PUT',
        body: updates
      })

      if (fetchError.value) {
        // Rollback: Stelle ursprünglichen Zustand wieder her
        recurringTasks.value[index] = originalTask as RecurringTask
        error.value = fetchError.value
        return
      }

      if (updatedTask.value) {
        // Ersetze mit Server-Response
        recurringTasks.value[index] = updatedTask.value
      }

      return updatedTask.value
    } catch (e: unknown) {
      // Rollback: Stelle ursprünglichen Zustand wieder her
      recurringTasks.value[index] = originalTask as RecurringTask
      error.value = e as Error
      return
    } finally {
      isUpdating.value = false
    }
  }

  const deleteRecurringTask = async (id: string) => {
    isDeleting.value = true
    error.value = null

    const index = recurringTasks.value.findIndex(t => t.id === id)
    if (index === -1) {
      isDeleting.value = false
      return
    }

    // Speichere RecurringTask für Rollback
    const deletedTask = recurringTasks.value[index] as RecurringTask

    // Optimistic Update: Entferne RecurringTask sofort
    recurringTasks.value = recurringTasks.value.filter(t => t.id !== id)

    try {
      const { error: fetchError } = await useAuthFetch(`/recurring-tasks/${id}`, {
        method: 'DELETE'
      })

      if (fetchError.value) {
        // Rollback: Füge RecurringTask wieder hinzu
        recurringTasks.value = [
          ...recurringTasks.value.slice(0, index),
          deletedTask,
          ...recurringTasks.value.slice(index)
        ]
        error.value = fetchError.value
        return
      }
    } catch (e: unknown) {
      // Rollback: Füge RecurringTask wieder hinzu
      recurringTasks.value = [
        ...recurringTasks.value.slice(0, index),
        deletedTask,
        ...recurringTasks.value.slice(index)
      ]
      error.value = e as Error
      return
    } finally {
      isDeleting.value = false
    }
  }

  return {
    recurringTasks: readonly(recurringTasks),
    loading: readonly(loading),
    error: readonly(error),
    isCreating: readonly(isCreating),
    isUpdating: readonly(isUpdating),
    isDeleting: readonly(isDeleting),
    getAllRecurringTasks,
    getActiveRecurringTasks,
    getRecurringTasksByDate,
    getRecurringTasksByProject,
    fetchRecurringTasks,
    createRecurringTask,
    updateRecurringTask,
    deleteRecurringTask
  }
})
