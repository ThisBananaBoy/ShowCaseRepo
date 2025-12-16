import { defineStore } from 'pinia'
import { useAuthFetch } from '~/composables/useAuthFetch'
import type { Appointment, CreateAppointmentDto, UpdateAppointmentDto } from '~/types/Appointment'

export const useAppointmentStore = defineStore('appointments', () => {
  const appointments = ref<Appointment[]>([])
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

  const getAppointmentsByDate = (day: Date) => {
    const target = normalizeDayKey(day)
    if (target === null) return []
    return appointments.value
      .filter(apt => normalizeDayKey(apt.start_time) === target)
      .sort((a, b) => new Date(a.start_time).getTime() - new Date(b.start_time).getTime())
  }

  const getTodayAppointments = () => getAppointmentsByDate(new Date())

  const getAppointmentsByProject = (projectId: string) =>
    appointments.value.filter(apt => apt.project_id === projectId)

  const patchLocalAppointment = (id: string, partial: Partial<Appointment>) => {
    const index = appointments.value.findIndex(appointment => appointment.id === id)
    if (index !== -1) {
      const current = appointments.value[index]
      if (!current) return
      appointments.value[index] = {
        ...current,
        ...partial,
        id: current.id,
        title: partial.title ?? current.title,
        start_time: partial.start_time ?? current.start_time,
        end_time: partial.end_time ?? current.end_time
      }
    }
  }

  const fetchAppointments = async () => {
    loading.value = true
    error.value = null

    try {
      const { data, error: fetchError } = await useAuthFetch<Appointment[]>('/appointments')

      if (fetchError.value) {
        error.value = fetchError.value
        return
      }

      appointments.value = data.value || []
    } catch (e: unknown) {
      error.value = e as Error
      console.error('Fetch appointments failed:', e)
    } finally {
      loading.value = false
    }
  }

  const createAppointment = async (payload: CreateAppointmentDto) => {
    isCreating.value = true
    error.value = null

    // Optimistic Update: Erstelle temporäres Appointment mit generierter ID
    const tempId = `temp-${Date.now()}`
    const optimisticAppointment: Appointment = {
      ...payload,
      id: tempId
    } as Appointment

    // Optimistisch hinzufügen
    appointments.value = [...appointments.value, optimisticAppointment]

    try {
      const { data: newAppointment, error: fetchError } = await useAuthFetch<Appointment>('/appointments', {
        method: 'POST',
        body: payload
      })

      if (fetchError.value) {
        // Rollback: Entferne optimistisches Appointment
        appointments.value = appointments.value.filter(a => a.id !== tempId)
        error.value = fetchError.value
        return
      }

      if (newAppointment.value) {
        // Ersetze optimistisches Appointment mit Server-Response
        const index = appointments.value.findIndex(a => a.id === tempId)
        if (index !== -1) {
          appointments.value[index] = newAppointment.value
        }
      }

      return newAppointment.value
    } catch (e: unknown) {
      // Rollback: Entferne optimistisches Appointment
      appointments.value = appointments.value.filter(a => a.id !== tempId)
      error.value = e as Error
      return
    } finally {
      isCreating.value = false
    }
  }

  const updateAppointment = async (id: string, updates: UpdateAppointmentDto) => {
    isUpdating.value = true
    error.value = null

    const index = appointments.value.findIndex(a => a.id === id)
    if (index === -1) {
      isUpdating.value = false
      return
    }

    // Speichere ursprünglichen Zustand für Rollback
    const originalAppointment = { ...appointments.value[index] }

    // Optimistic Update: Wende Updates sofort an
    const { id: _, ...updatesWithoutId } = updates as Appointment
    appointments.value[index] = { ...appointments.value[index], ...updatesWithoutId, id }

    try {
      const { data: updatedAppointment, error: fetchError } = await useAuthFetch<Appointment>(`/appointments/${id}`, {
        method: 'PUT',
        body: updates
      })

      if (fetchError.value) {
        // Rollback: Stelle ursprünglichen Zustand wieder her
        appointments.value[index] = originalAppointment as Appointment
        error.value = fetchError.value
        return
      }

      if (updatedAppointment.value) {
        // Ersetze mit Server-Response
        appointments.value[index] = updatedAppointment.value
      }

      return updatedAppointment.value
    } catch (e: unknown) {
      // Rollback: Stelle ursprünglichen Zustand wieder her
      appointments.value[index] = originalAppointment as Appointment
      error.value = e as Error
      return
    } finally {
      isUpdating.value = false
    }
  }

  const deleteAppointment = async (id: string) => {
    isDeleting.value = true
    error.value = null

    const index = appointments.value.findIndex(a => a.id === id)
    if (index === -1) {
      isDeleting.value = false
      return
    }

    // Speichere Appointment für Rollback
    const deletedAppointment = appointments.value[index] as Appointment

    // Optimistic Update: Entferne Appointment sofort
    appointments.value = appointments.value.filter(a => a.id !== id)

    try {
      const { error: fetchError } = await useAuthFetch(`/appointments/${id}`, {
        method: 'DELETE'
      })

      if (fetchError.value) {
        // Rollback: Füge Appointment wieder hinzu
        appointments.value = [
          ...appointments.value.slice(0, index),
          deletedAppointment,
          ...appointments.value.slice(index)
        ]
        error.value = fetchError.value
        return
      }
    } catch (e: unknown) {
      // Rollback: Füge Appointment wieder hinzu
      appointments.value = [
        ...appointments.value.slice(0, index),
        deletedAppointment,
        ...appointments.value.slice(index)
      ]
      error.value = e as Error
      return
    } finally {
      isDeleting.value = false
    }
  }

  return {
    appointments: readonly(appointments),
    loading: readonly(loading),
    error: readonly(error),
    isCreating: readonly(isCreating),
    isUpdating: readonly(isUpdating),
    isDeleting: readonly(isDeleting),
    getAppointmentsByDate,
    getTodayAppointments,
    getAppointmentsByProject,
    patchLocalAppointment,
    fetchAppointments,
    createAppointment,
    updateAppointment,
    deleteAppointment
  }
})
