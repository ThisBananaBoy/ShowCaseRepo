<script setup lang="ts">

import { storeToRefs } from 'pinia'
import { useAppointmentStore } from '~/stores/useAppointmentStore'
import { useProjectStore } from '~/stores/useProjectStore'
import { useTaskStore } from '~/stores/useTaskStore'
import type { Task } from '~/types/Task'

const projectStore = useProjectStore()
const taskStore = useTaskStore()
const appointmentStore = useAppointmentStore()

const { projects } = storeToRefs(projectStore)
const { tasks } = storeToRefs(taskStore)
const { appointments } = storeToRefs(appointmentStore)

onMounted(async () => {
  const promises: Promise<unknown>[] = []

  if (!projects.value.length) {
    promises.push(projectStore.fetchProjects())
  }

  if (!tasks.value.length) {
    promises.push(taskStore.fetchTasks())
  }

  if (!appointments.value.length) {
    promises.push(appointmentStore.fetchAppointments())
  }

  await Promise.all(promises)
})


// Projekt-Farben (für Timeline)
const projectColors = [
  'bg-blue-500',
  'bg-purple-500',
  'bg-pink-500',
  'bg-orange-500',
  'bg-teal-500',
  'bg-indigo-500',
  'bg-rose-500',
  'bg-amber-500',
]

const projectBorderColors = [
  'border-blue-500/30',
  'border-purple-500/30',
  'border-pink-500/30',
  'border-orange-500/30',
  'border-teal-500/30',
  'border-indigo-500/30',
  'border-rose-500/30',
  'border-amber-500/30',
]

function getProjectColor(projectId: string): string {
  const projectIndex = projects.value.findIndex((p) => p.id === projectId)
  if (projectIndex === -1) return 'bg-neutral'
  return projectColors[projectIndex % projectColors.length]!
}

function getProjectBorderColor(projectId: string): string {
  const projectIndex = projects.value.findIndex((p) => p.id === projectId)
  if (projectIndex === -1) return 'border-muted-foreground/20'
  return projectBorderColors[projectIndex % projectBorderColors.length]!
}

// Heutige Appointments
const todayAppointments = computed(() => appointmentStore.getTodayAppointments())

// Tasks mit Zeit
const tasksWithTime = computed(() =>
  tasks.value.filter((task: Task) => {
    if (!task.start_time || !task.end_time) return false

    const start = new Date(task.start_time)
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    const tomorrow = new Date(today)
    tomorrow.setDate(today.getDate() + 1)

    return start >= today && start < tomorrow
  })
)

function getProjectProgress(projectId: string) {
  const projectTasks = tasks.value.filter((task: Task) => task.project_id === projectId)
  if (!projectTasks.length) return 0
  const completed = projectTasks.filter((task: Task) => task.completed_at).length
  return Math.round((completed / projectTasks.length) * 100)
}

function formatDate(date?: Date) {
  if (!date) return '-'
  return new Date(date).toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}
</script>

<template>
  <UDashboardPanel id="dashboard" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar title="Dashboard">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex-1 flex overflow-hidden min-h-0">
        <!-- Timeline (links) -->
        <div class="w-96 border-r border-border flex flex-col overflow-y-auto shrink-0">
          <Timeline
            :appointments="todayAppointments"
            :tasks-with-time="tasksWithTime"
            :get-project-color="getProjectColor"
            :get-project-border-color="getProjectBorderColor"
          />
        </div>

        <!-- Hauptinhalt (rechts) -->
        <div class="flex-1 overflow-y-auto min-h-0">
          <div class="p-6 space-y-6 w-full">
            <!-- Header -->
            <div>
              <h2 class="text-2xl font-bold">Willkommen zurück</h2>
              <p class="text-muted-foreground mt-1">
                Hier ist eine Übersicht deiner aktiven Projekte
              </p>
            </div>

            <!-- Aktive Projekte -->
            <div>
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold">Aktive Projekte</h3>
            <UButton
              icon="i-lucide-plus"
              size="sm"
              variant="outline"
              @click="$router.push('/projects')"
            >
              Alle Projekte
            </UButton>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <UCard
              v-for="project in projects"
              :key="project.id"
              :ui="{ body: 'p-5' }"
              class="cursor-pointer hover:bg-muted/50 transition-colors"
              @click="$router.push(`/projects/${project.id}`)"
            >
              <div class="space-y-4">
                <div class="flex items-start justify-between">
                  <div class="flex-1">
                    <h4 class="font-semibold text-base">
                      {{ project.name }}
                    </h4>
                    <p class="text-sm text-muted-foreground mt-1 line-clamp-2">
                      {{ project.description }}
                    </p>
                  </div>
                  <UBadge
                    label="Aktiv"
                    color="success"
                    size="xs"
                    variant="subtle"
                  />
                </div>

                <!-- Progress -->
                <div class="space-y-2">
                  <div class="flex justify-between text-xs">
                    <span class="text-muted-foreground">Fortschritt</span>
                    <span class="font-medium">{{ getProjectProgress(project.id) }}%</span>
                  </div>
                  <UProgress
                    :value="getProjectProgress(project.id)"
                    size="sm"
                  />
                </div>

                <!-- Dates -->
                <div class="flex items-center justify-between text-xs text-muted-foreground">
                  <div class="flex items-center gap-1">
                    <UIcon name="i-lucide-calendar" class="size-3" />
                    <span>Start: {{ formatDate(project.start_date) }}</span>
                  </div>
                  <div class="flex items-center gap-1">
                    <UIcon name="i-lucide-flag" class="size-3" />
                    <span>{{ formatDate(project.last_deadline_date) }}</span>
                  </div>
                </div>
              </div>
            </UCard>
          </div>

          <div
            v-if="projects.length === 0"
            class="text-center py-16 border-2 border-dashed rounded-lg"
          >
            <UIcon name="i-lucide-folder-kanban" class="size-16 mx-auto mb-4 text-muted-foreground/50" />
            <h3 class="text-lg font-semibold mb-2">
              Keine aktiven Projekte
            </h3>
            <p class="text-muted-foreground mb-4">
              Erstelle dein erstes Projekt
            </p>
            <UButton
              icon="i-lucide-plus"
              @click="$router.push('/projects')"
            >
              Neues Projekt
            </UButton>
          </div>
        </div>
          </div>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
