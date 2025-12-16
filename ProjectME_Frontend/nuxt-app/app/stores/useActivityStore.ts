import { defineStore } from 'pinia'
import { useAuthFetch } from '~/composables/useAuthFetch'
import type { Activity, CreateActivityDto, UpdateActivityDto } from '~/types/Activity'

export const useActivityStore = defineStore('activities', () => {
  const activities = ref<Activity[]>([])
  const loading = ref(false)
  const error = ref<Error | null>(null)

  const isCreating = ref(false)
  const isUpdating = ref(false)
  const isDeleting = ref(false)

  const getActivitiesByProject = (projectId: string) =>
    activities.value.filter(activity => activity.project_id === projectId)

  const getActivitiesByTask = (taskId: string) =>
    activities.value.filter(activity => activity.task_id === taskId)

  const getActivitiesByEntity = (entityType: Activity['entity_type'], entityId: string) =>
    activities.value.filter(activity => activity.entity_type === entityType && activity.entity_id === entityId)

  const fetchActivities = async () => {
    loading.value = true
    error.value = null

    try {
      const { data, error: fetchError } = await useAuthFetch<Activity[]>('/activities')

      if (fetchError.value) {
        error.value = fetchError.value
        return
      }

      activities.value = data.value || []
    } catch (e: unknown) {
      error.value = e as Error
      console.error('Fetch activities failed:', e)
    } finally {
      loading.value = false
    }
  }

  const createActivity = async (payload: CreateActivityDto) => {
    isCreating.value = true
    error.value = null

    // Optimistic Update: Erstelle temporäre Activity mit generierter ID
    const tempId = `temp-${Date.now()}`
    const optimisticActivity: Activity = {
      ...payload,
      id: tempId,
      created_at: new Date()
    } as Activity

    // Optimistisch hinzufügen
    activities.value = [...activities.value, optimisticActivity]

    try {
      const { data: newActivity, error: fetchError } = await useAuthFetch<Activity>('/activities', {
        method: 'POST',
        body: payload
      })

      if (fetchError.value) {
        // Rollback: Entferne optimistische Activity
        activities.value = activities.value.filter(a => a.id !== tempId)
        error.value = fetchError.value
        return
      }

      if (newActivity.value) {
        // Ersetze optimistische Activity mit Server-Response
        const index = activities.value.findIndex(a => a.id === tempId)
        if (index !== -1) {
          activities.value[index] = newActivity.value
        }
      }

      return newActivity.value
    } catch (e: unknown) {
      // Rollback: Entferne optimistische Activity
      activities.value = activities.value.filter(a => a.id !== tempId)
      error.value = e as Error
      return
    } finally {
      isCreating.value = false
    }
  }

  const updateActivity = async (id: string, updates: UpdateActivityDto) => {
    isUpdating.value = true
    error.value = null

    const index = activities.value.findIndex(a => a.id === id)
    if (index === -1) {
      isUpdating.value = false
      return
    }

    // Speichere ursprünglichen Zustand für Rollback
    const originalActivity = { ...activities.value[index] }

    // Optimistic Update: Wende Updates sofort an
    const { id: _, ...updatesWithoutId } = updates as Activity
    activities.value[index] = { ...activities.value[index], ...updatesWithoutId, id }

    try {
      const { data: updatedActivity, error: fetchError } = await useAuthFetch<Activity>(`/activities/${id}`, {
        method: 'PUT',
        body: updates
      })

      if (fetchError.value) {
        // Rollback: Stelle ursprünglichen Zustand wieder her
        activities.value[index] = originalActivity as Activity
        error.value = fetchError.value
        return
      }

      if (updatedActivity.value) {
        // Ersetze mit Server-Response
        activities.value[index] = updatedActivity.value
      }

      return updatedActivity.value
    } catch (e: unknown) {
      // Rollback: Stelle ursprünglichen Zustand wieder her
      activities.value[index] = originalActivity as Activity
      error.value = e as Error
      return
    } finally {
      isUpdating.value = false
    }
  }

  const deleteActivity = async (id: string) => {
    isDeleting.value = true

    try {
      const { error: fetchError } = await useAuthFetch(`/activities/${id}`, {
        method: 'DELETE'
      })

      if (fetchError.value) {
        error.value = fetchError.value
        return
      }

      activities.value = activities.value.filter(activity => activity.id !== id)
    } catch (e: unknown) {
      error.value = e as Error
      return
    } finally {
      isDeleting.value = false
    }
  }

  return {
    activities: readonly(activities),
    loading: readonly(loading),
    error: readonly(error),
    isCreating: readonly(isCreating),
    isUpdating: readonly(isUpdating),
    isDeleting: readonly(isDeleting),
    getActivitiesByProject,
    getActivitiesByTask,
    getActivitiesByEntity,
    fetchActivities,
    createActivity,
    updateActivity,
    deleteActivity
  }
})
