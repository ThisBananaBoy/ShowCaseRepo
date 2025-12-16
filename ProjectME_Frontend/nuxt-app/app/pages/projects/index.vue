<script setup lang="ts">
import type { Project } from '~/types/Project'
import { StatusTypes } from '~/types/Project'
import { useProjectStore } from '~/stores/useProjectStore'
import { useTaskStore } from '~/stores/useTaskStore'
import { useTimeEntryStore } from '~/stores/useTimeEntryStore'
import { storeToRefs } from 'pinia'
import { isTaskCompleted } from '~/utils/taskHelpers'

const projectStore = useProjectStore()
const taskStore = useTaskStore()
const timeEntryStore = useTimeEntryStore()

const { projects } = storeToRefs(projectStore)
const { tasks } = storeToRefs(taskStore)
const { timeEntries } = storeToRefs(timeEntryStore)
const projectsList = computed(() => projects.value.slice())
const timeEntriesList = computed(() => timeEntries.value.slice())

const tabs = [
  {
    label: "Übersicht",
    value: "overview",
    icon: "i-lucide-kanban-square",
  },
];

const selectedTab = ref("overview");

const { openSlideover } = useSlideover();

const getProjectProgress = (projectId: string) => {
  const related = taskStore.getTasksByProject(projectId)
  if (!related.length) return 0
  const completed = related.filter(t => isTaskCompleted(t)).length
  return Math.round((completed / related.length) * 100)
}



onMounted(() => {
  if (!projects.value.length) projectStore.fetchProjects()
  if (!tasks.value.length) taskStore.fetchTasks()
  if (!timeEntries.value.length) timeEntryStore.fetchTimeEntries()
})

// Drag & Drop
const draggedProject = ref<Project | null>(null)
const dragOverColumn = ref<StatusTypes | null>(null)

function onDragStart(event: DragEvent, project: Project) {
  draggedProject.value = project
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
  }
}

function onDragOver(event: DragEvent, status: StatusTypes) {
  event.preventDefault()
  dragOverColumn.value = status
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'move'
  }
}

function onDragLeave() {
  dragOverColumn.value = null
}

function onDrop(event: DragEvent, newStatus: StatusTypes.ACTIVE | StatusTypes.PAUSED | StatusTypes.COMPLETED) {
  event.preventDefault()

  if (draggedProject.value && draggedProject.value.status !== newStatus) {
    draggedProject.value.status = newStatus
  }

  draggedProject.value = null
  dragOverColumn.value = null
}

function getStatusColor(status: string) {
  switch (status) {
    case "active":
      return "success";
    case "paused":
      return "warning";
    case "completed":
      return "info";
    case "archived":
      return "neutral";
    default:
      return "neutral";
  }
}

function formatDate(date?: Date) {
  if (!date) return "-";
  return new Date(date).toLocaleDateString("de-DE", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}
</script>

<template>
  <UDashboardPanel id="projects" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar title="Projekte">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>

        <template #right>
          <UButton
            icon="i-lucide-plus"
            size="md"
            class="rounded-full"
            @click="openSlideover('add-project')"
          >
            Neues Projekt
          </UButton>
        </template>
      </UDashboardNavbar>

      <UDashboardToolbar>
        <template #left>
          <UTabs
            v-model="selectedTab"
            :items="tabs"
            size="md"
            class="pt-2"
          />
        </template>
      </UDashboardToolbar>
    </template>

    <template #body>
      <div class="flex-1 overflow-y-auto w-full min-h-0">
        <!-- Übersicht (Kanban) -->
        <div v-if="selectedTab === 'overview'" class="p-6">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- Aktive Projekte -->
          <div
            class="min-h-[400px]"
            @dragover="onDragOver($event, StatusTypes.ACTIVE)"
            @dragleave="onDragLeave"
            @drop="onDrop($event, StatusTypes.ACTIVE)"
          >
            <div class="flex items-center justify-between mb-4">
              <h3
                class="font-semibold text-sm uppercase tracking-wide text-muted-foreground"
              >
                Aktiv
              </h3>
              <UBadge
                :label="projectStore.activeProjects.length.toString()"
                variant="subtle"
              />
            </div>
            <div
              class="space-y-3 p-2 rounded-lg transition-colors"
              :class="dragOverColumn === StatusTypes.ACTIVE ? 'bg-primary/10 ring-2 ring-primary' : ''"
            >
              <UCard
                v-for="project in projectStore.activeProjects"
                :key="project.id"
                :ui="{ body: 'p-4' }"
                draggable="true"
                class="cursor-move hover:bg-muted/50 transition-colors"
                @dragstart="onDragStart($event, project)"
                @click.stop="$router.push(`/projects/${project.id}`)"
              >
                <div class="space-y-3">
                  <div class="flex items-start justify-between">
                    <h4 class="font-semibold">
                      {{ project.name }}
                    </h4>
                    <UBadge
                      :label="project.status"
                      :color="getStatusColor(project.status)"
                      size="xs"
                      variant="subtle"
                    />
                  </div>

                  <p class="text-sm text-muted-foreground line-clamp-2">
                    {{ project.description }}
                  </p>

                  <!-- Progress -->
                  <div class="space-y-1">
                    <div class="flex justify-between text-xs">
                      <span class="text-muted-foreground">Fortschritt</span>
                      <span class="font-medium"
                        >{{ getProjectProgress(project.id) }}%</span
                      >
                    </div>
                    <UProgress
                      :value="getProjectProgress(project.id)"
                      size="sm"
                    />
                  </div>

                  <!-- Dates -->
                  <div
                    class="flex items-center gap-4 text-xs text-muted-foreground"
                  >
                    <div class="flex items-center gap-1">
                      <UIcon name="i-lucide-calendar" class="size-3" />
                      {{ formatDate(project.start_date) }}
                    </div>
                    <div class="flex items-center gap-1">
                      <UIcon name="i-lucide-flag" class="size-3" />
                      {{ formatDate(project.last_deadline_date) }}
                    </div>
                  </div>
                </div>
              </UCard>

              <div
                v-if="projectStore.activeProjects.length === 0"
                class="text-center py-8 text-muted-foreground text-sm"
              >
                Keine aktiven Projekte
              </div>
            </div>
          </div>

          <!-- Pausierte Projekte -->
          <div
            class="min-h-[400px]"
            @dragover="onDragOver($event, StatusTypes.PAUSED)"
            @dragleave="onDragLeave"
            @drop="onDrop($event, StatusTypes.PAUSED)"
          >
            <div class="flex items-center justify-between mb-4">
              <h3
                class="font-semibold text-sm uppercase tracking-wide text-muted-foreground"
              >
                Pausiert
              </h3>
              <UBadge
                :label="projectStore.pausedProjects.length.toString()"
                variant="subtle"
              />
            </div>
            <div
              class="space-y-3 p-2 rounded-lg transition-colors"
              :class="dragOverColumn === StatusTypes.PAUSED ? 'bg-primary/10 ring-2 ring-primary' : ''"
            >
              <UCard
                v-for="project in projectStore.pausedProjects"
                :key="project.id"
                :ui="{ body: 'p-4' }"
                draggable="true"
                class="cursor-move hover:bg-muted/50 transition-colors"
                @dragstart="onDragStart($event, project)"
                @click.stop="$router.push(`/projects/${project.id}`)"
              >
                <div class="space-y-3">
                  <div class="flex items-start justify-between">
                    <h4 class="font-semibold">{{ project.name }}</h4>
                    <UBadge
                      :label="project.status"
                      :color="getStatusColor(project.status)"
                      size="xs"
                      variant="subtle"
                    />
                  </div>

                  <p class="text-sm text-muted-foreground line-clamp-2">
                    {{ project.description }}
                  </p>

                  <!-- Progress -->
                  <div class="space-y-1">
                    <div class="flex justify-between text-xs">
                      <span class="text-muted-foreground">Fortschritt</span>
                      <span class="font-medium"
                        >{{ getProjectProgress(project.id) }}%</span
                      >
                    </div>
                    <UProgress
                      :value="getProjectProgress(project.id)"
                      size="sm"
                      color="warning"
                    />
                  </div>
                </div>
              </UCard>

              <div
                v-if="projectStore.pausedProjects.length === 0"
                class="text-center py-8 text-muted-foreground text-sm"
              >
                Keine pausierten Projekte
              </div>
            </div>
          </div>

          <!-- Abgeschlossene Projekte -->
          <div
            class="min-h-[400px]"
            @dragover="onDragOver($event, StatusTypes.COMPLETED)"
            @dragleave="onDragLeave"
            @drop="onDrop($event, StatusTypes.COMPLETED)"
          >
            <div class="flex items-center justify-between mb-4">
              <h3
                class="font-semibold text-sm uppercase tracking-wide text-muted-foreground"
              >
                Abgeschlossen
              </h3>
              <UBadge
                :label="projectStore.completedProjects.length.toString()"
                variant="subtle"
              />
            </div>
            <div
              class="space-y-3 p-2 rounded-lg transition-colors"
              :class="dragOverColumn === StatusTypes.COMPLETED ? 'bg-primary/10 ring-2 ring-primary' : ''"
            >
              <UCard
                v-for="project in projectStore.completedProjects"
                :key="project.id"
                :ui="{ body: 'p-4' }"
                draggable="true"
                class="cursor-move hover:bg-muted/50 transition-colors"
                @dragstart="onDragStart($event, project)"
                @click.stop="$router.push(`/projects/${project.id}`)"
              >
                <div class="space-y-3">
                  <div class="flex items-start justify-between">
                    <h4 class="font-semibold">{{ project.name }}</h4>
                    <UBadge
                      :label="project.status"
                      :color="getStatusColor(project.status)"
                      size="xs"
                      variant="subtle"
                    />
                  </div>

                  <p class="text-sm text-muted-foreground line-clamp-2">
                    {{ project.description }}
                  </p>

                  <UProgress :value="100" size="sm" color="info" />
                </div>
              </UCard>

              <div
                v-if="projectStore.completedProjects.length === 0"
                class="text-center py-8 text-muted-foreground text-sm"
              >
                Keine abgeschlossenen Projekte
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Timeline Tab -->
      <div v-else-if="selectedTab === 'timeline'" class="p-6">
        <GanttChart :projects="projectsList" />
      </div>

      <!-- Aktivität Tab -->
      <div v-else-if="selectedTab === 'activity'" class="p-6">
        <ActivityHeatmap :time-entries="timeEntriesList" :projects="projectsList" />
      </div>

      <!-- Archiv Tab -->
      <div v-else-if="selectedTab === 'archive'" class="p-6">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <UCard
            v-for="project in projectStore.archivedProjects"
            :key="project.id"
            :ui="{ body: 'p-4' }"
            class="cursor-pointer hover:bg-muted/50 transition-colors"
          >
            <h4 class="font-semibold">{{ project.name }}</h4>
            <p class="text-sm text-muted-foreground mt-1">
              {{ project.description }}
            </p>
          </UCard>

          <div
            v-if="projectStore.archivedProjects.length === 0"
            class="col-span-full text-center py-12 text-muted-foreground"
          >
            <UIcon
              name="i-lucide-archive"
              class="size-16 mx-auto mb-4 opacity-50"
            />
            <p>Keine archivierten Projekte</p>
          </div>
        </div>
      </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
