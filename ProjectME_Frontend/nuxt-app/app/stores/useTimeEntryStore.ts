import { defineStore } from 'pinia'
import { useAuthFetch } from '~/composables/useAuthFetch'
import type { CreateTimeEntryDto, TimeEntry, UpdateTimeEntryDto } from '~/types/TimeEntry'

export const useTimeEntryStore = defineStore('timeEntries', () => {
  const timeEntries = ref<TimeEntry[]>([])
  const loading = ref(false)
  const error = ref<Error | null>(null)

  const isCreating = ref(false)
  const isUpdating = ref(false)
  const isDeleting = ref(false)

  const fetchTimeEntries = async () => {
    loading.value = true
    error.value = null

    try {
      const { data, error: fetchError } = await useAuthFetch<TimeEntry[]>('/time-entries')

      if (fetchError.value) {
        error.value = fetchError.value
        return
      }

      timeEntries.value = data.value || []
    } catch (e: unknown) {
      error.value = e as Error
      console.error('Fetch time entries failed:', e)
    } finally {
      loading.value = false
    }
  }

  const createTimeEntry = async (payload: CreateTimeEntryDto) => {
    isCreating.value = true
    error.value = null

    // Optimistic Update: Erstelle temporären TimeEntry mit generierter ID
    const tempId = `temp-${Date.now()}`
    const optimisticEntry: TimeEntry = {
      ...payload,
      id: tempId,
      created_at: new Date()
    } as TimeEntry

    // Optimistisch hinzufügen
    timeEntries.value = [...timeEntries.value, optimisticEntry]

    try {
      const { data: newEntry, error: fetchError } = await useAuthFetch<TimeEntry>('/time-entries', {
        method: 'POST',
        body: payload
      })

      if (fetchError.value) {
        // Rollback: Entferne optimistischen TimeEntry
        timeEntries.value = timeEntries.value.filter(e => e.id !== tempId)
        error.value = fetchError.value
        return
      }

      if (newEntry.value) {
        // Ersetze optimistischen TimeEntry mit Server-Response
        const index = timeEntries.value.findIndex(e => e.id === tempId)
        if (index !== -1) {
          timeEntries.value[index] = newEntry.value
        }
      }

      return newEntry.value
    } catch (e: unknown) {
      // Rollback: Entferne optimistischen TimeEntry
      timeEntries.value = timeEntries.value.filter(e => e.id !== tempId)
      error.value = e as Error
      return
    } finally {
      isCreating.value = false
    }
  }

  const updateTimeEntry = async (id: string, updates: UpdateTimeEntryDto) => {
    isUpdating.value = true
    error.value = null

    const index = timeEntries.value.findIndex(e => e.id === id)
    if (index === -1) {
      isUpdating.value = false
      return
    }

    // Speichere ursprünglichen Zustand für Rollback
    const originalEntry = { ...timeEntries.value[index] }

    // Optimistic Update: Wende Updates sofort an
    const { id: _, ...updatesWithoutId } = updates as TimeEntry
    timeEntries.value[index] = { ...timeEntries.value[index], ...updatesWithoutId, id }

    try {
      const { data: updatedEntry, error: fetchError } = await useAuthFetch<TimeEntry>(`/time-entries/${id}`, {
        method: 'PUT',
        body: updates
      })

      if (fetchError.value) {
        // Rollback: Stelle ursprünglichen Zustand wieder her
        timeEntries.value[index] = originalEntry as TimeEntry
        error.value = fetchError.value
        return
      }

      if (updatedEntry.value) {
        // Ersetze mit Server-Response
        timeEntries.value[index] = updatedEntry.value
      }

      return updatedEntry.value
    } catch (e: unknown) {
      // Rollback: Stelle ursprünglichen Zustand wieder her
      timeEntries.value[index] = originalEntry as TimeEntry
      error.value = e as Error
      return
    } finally {
      isUpdating.value = false
    }
  }

  const deleteTimeEntry = async (id: string) => {
    isDeleting.value = true
    error.value = null

    const index = timeEntries.value.findIndex(e => e.id === id)
    if (index === -1) {
      isDeleting.value = false
      return
    }

    // Speichere TimeEntry für Rollback
    const deletedEntry = timeEntries.value[index] as TimeEntry

    // Optimistic Update: Entferne TimeEntry sofort
    timeEntries.value = timeEntries.value.filter(e => e.id !== id)

    try {
      const { error: fetchError } = await useAuthFetch(`/time-entries/${id}`, {
        method: 'DELETE'
      })

      if (fetchError.value) {
        // Rollback: Füge TimeEntry wieder hinzu
        timeEntries.value = [
          ...timeEntries.value.slice(0, index),
          deletedEntry,
          ...timeEntries.value.slice(index)
        ]
        error.value = fetchError.value
        return
      }
    } catch (e: unknown) {
      // Rollback: Füge TimeEntry wieder hinzu
      timeEntries.value = [
        ...timeEntries.value.slice(0, index),
        deletedEntry,
        ...timeEntries.value.slice(index)
      ]
      error.value = e as Error
      return
    } finally {
      isDeleting.value = false
    }
  }

  return {
    timeEntries: readonly(timeEntries),
    loading: readonly(loading),
    error: readonly(error),
    isCreating: readonly(isCreating),
    isUpdating: readonly(isUpdating),
    isDeleting: readonly(isDeleting),
    fetchTimeEntries,
    createTimeEntry,
    updateTimeEntry,
    deleteTimeEntry
  }
})
