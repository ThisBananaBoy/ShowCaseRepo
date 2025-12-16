import { defineStore } from 'pinia'
import { useAuthFetch } from '~/composables/useAuthFetch'
import { StatusTypes } from '~/types/Project'
import type { CreateProjectDto, Project, UpdateProjectDto } from '~/types/Project'

export const useProjectStore = defineStore('projects', () => {
  const projects = ref<Project[]>([])
  const loading = ref(false)
  const error = ref<Error | null>(null)

  const isCreating = ref(false)
  const isUpdating = ref(false)
  const isDeleting = ref(false)

  const defaultStatusOrder: StatusTypes[] = [
    StatusTypes.ACTIVE,
    StatusTypes.PAUSED,
    StatusTypes.COMPLETED,
    StatusTypes.ARCHIVED
  ]

  // Lightweight Getter-Funktionen (keine computed), damit nur bei Bedarf gefiltert/sortiert wird

  const activeProjects = computed(() =>
    projects.value.filter(p => p.status === StatusTypes.ACTIVE)
  )

  const pausedProjects = computed(() =>
    projects.value.filter(p => p.status === StatusTypes.PAUSED)
  )

  const completedProjects = computed(() =>
    projects.value.filter(p => p.status === StatusTypes.COMPLETED)
  )

  const archivedProjects = computed(() =>
    projects.value.filter(p => p.status === StatusTypes.ARCHIVED)
  )



  const getProjectById = (id: string) =>
    projects.value.find(project => project.id === id)

  const getProjectsSortedByStatus = (order: StatusTypes[] = defaultStatusOrder) => {
    const orderMap = new Map(order.map((status, index) => [status, index]))
    return [...projects.value].sort((a, b) => {
      const orderA = orderMap.get(a.status) ?? Number.MAX_SAFE_INTEGER
      const orderB = orderMap.get(b.status) ?? Number.MAX_SAFE_INTEGER
      return orderA - orderB
    })
  }

  const fetchProjects = async () => {
    loading.value = true
    error.value = null

    try {
      const { data, error: fetchError } = await useAuthFetch<Project[]>('/projects')

      if (fetchError.value) {
        error.value = fetchError.value
        return
      }

      projects.value = data.value || []
    } catch (e: unknown) {
      error.value = e as Error
      console.error('Fetch projects failed:', e)
    } finally {
      loading.value = false
    }
  }

  const createProject = async (payload: CreateProjectDto) => {
    isCreating.value = true
    error.value = null

    // Optimistic Update: Erstelle temporäres Project mit generierter ID
    const tempId = `temp-${Date.now()}`
    const optimisticProject: Project = {
      ...payload,
      user_id: '',
      id: tempId,
      deadlines: [],
      milestones: []
    } as Project

    // Optimistisch hinzufügen
    projects.value = [...projects.value, optimisticProject]

    try {
      const { data: newProject, error: fetchError } = await useAuthFetch<Project>('/projects', {
        method: 'POST',
        body: payload
      })

      if (fetchError.value) {
        // Rollback: Entferne optimistisches Project
        projects.value = projects.value.filter(p => p.id !== tempId)
        error.value = fetchError.value
        return
      }

      if (newProject.value) {
        // Ersetze optimistisches Project mit Server-Response
        const index = projects.value.findIndex(p => p.id === tempId)
        if (index !== -1) {
          projects.value[index] = newProject.value
        }
      }

      return newProject.value
    } catch (e: unknown) {
      // Rollback: Entferne optimistisches Project
      projects.value = projects.value.filter(p => p.id !== tempId)
      error.value = e as Error
      return
    } finally {
      isCreating.value = false
    }
  }

  const updateProject = async (id: string, updates: UpdateProjectDto) => {
    isUpdating.value = true
    error.value = null

    const index = projects.value.findIndex(p => p.id === id)
    if (index === -1) {
      isUpdating.value = false
      return
    }

    // Speichere ursprünglichen Zustand für Rollback
    const originalProject = { ...projects.value[index] }

    // Optimistic Update: Wende Updates sofort an
    const { id: _, ...updatesWithoutId } = updates as Project
    projects.value[index] = { ...projects.value[index], ...updatesWithoutId, id }

    try {
      const { data: updatedProject, error: fetchError } = await useAuthFetch<Project>(`/projects/${id}`, {
        method: 'PUT',
        body: updates
      })

      if (fetchError.value) {
        // Rollback: Stelle ursprünglichen Zustand wieder her
        projects.value[index] = originalProject as Project
        error.value = fetchError.value
        return
      }

      if (updatedProject.value) {
        // Ersetze mit Server-Response
        projects.value[index] = updatedProject.value
      }

      return updatedProject.value
    } catch (e: unknown) {
      // Rollback: Stelle ursprünglichen Zustand wieder her
      projects.value[index] = originalProject as Project
      error.value = e as Error
      return
    } finally {
      isUpdating.value = false
    }
  }

  const deleteProject = async (id: string) => {
    isDeleting.value = true
    error.value = null

    const index = projects.value.findIndex(p => p.id === id)
    if (index === -1) {
      isDeleting.value = false
      return
    }

    // Speichere Project für Rollback
    const deletedProject = projects.value[index] as Project

    // Optimistic Update: Entferne Project sofort
    projects.value = projects.value.filter(p => p.id !== id)

    try {
      const { error: fetchError } = await useAuthFetch(`/projects/${id}`, {
        method: 'DELETE'
      })

      if (fetchError.value) {
        // Rollback: Füge Project wieder hinzu
        projects.value = [
          ...projects.value.slice(0, index),
          deletedProject,
          ...projects.value.slice(index)
        ]
        error.value = fetchError.value
        return
      }
    } catch (e: unknown) {
      // Rollback: Füge Project wieder hinzu
      projects.value = [
        ...projects.value.slice(0, index),
        deletedProject,
        ...projects.value.slice(index)
      ]
      error.value = e as Error
      return
    } finally {
      isDeleting.value = false
    }
  }

  return {
    projects: readonly(projects),
    loading: readonly(loading),
    error: readonly(error),
    isCreating: readonly(isCreating),
    isUpdating: readonly(isUpdating),
    isDeleting: readonly(isDeleting),
    activeProjects,
    pausedProjects,
    completedProjects,
    archivedProjects,
    getProjectById,
    getProjectsSortedByStatus,
    fetchProjects,
    createProject,
    updateProject,
    deleteProject
  }
})
