<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useProjectStore } from '~/stores/useProjectStore'

const projectStore = useProjectStore()
const { activeProjects } = storeToRefs(projectStore)
const { setSidebarContent } = usePageSidebar()

type ToolType = 'pomodoro' | 'stopwatch' | 'reviews'

const selectedTool = ref<ToolType>('pomodoro')
const selectedProject = ref<string | null>(null)

const tools = [
  {
    id: 'pomodoro' as ToolType,
    label: 'Pomodoro Timer',
    icon: 'i-lucide-timer'
  },
  {
    id: 'stopwatch' as ToolType,
    label: 'Stoppuhr',
    icon: 'i-lucide-clock'
  },
  {
    id: 'reviews' as ToolType,
    label: 'Reviews',
    icon: 'i-lucide-star'
  }
]

const projectOptions = computed(() =>
  activeProjects.value.map(p => ({ value: p.id, label: p.name }))
)

function handleToolSelect(toolId: string) {
  selectedTool.value = toolId as ToolType
}

function handleProjectChange(projectId: string | null) {
  selectedProject.value = projectId
}

// Set sidebar content immediately (before mount to avoid race condition)
setSidebarContent('WorkSidebarContent', {
  selectedProject: selectedProject.value,
  selectedTool: selectedTool.value,
  tools,
  projectOptions: projectOptions.value,
  onToolSelect: handleToolSelect,
  onProjectChange: handleProjectChange
})

// Update sidebar when props change
watch([selectedProject, selectedTool, projectOptions], () => {
  setSidebarContent('WorkSidebarContent', {
    selectedProject: selectedProject.value,
    selectedTool: selectedTool.value,
    tools,
    projectOptions: projectOptions.value,
    onToolSelect: handleToolSelect,
    onProjectChange: handleProjectChange
  })
})

onMounted(() => {
  if (!activeProjects.value.length) {
    projectStore.fetchProjects()
  }
})
</script>

<template>
  <UDashboardPanel id="work" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar title="Arbeitsplatz">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex-1 overflow-y-auto w-full min-h-0">
        <div class="max-w-4xl mx-auto p-6">
          <!-- Pomodoro Timer -->
          <WorkPomodoroTimer
            v-if="selectedTool === 'pomodoro'"
            :selected-project="selectedProject"
          />

          <!-- Stopwatch -->
          <WorkStopwatch
            v-if="selectedTool === 'stopwatch'"
            :selected-project="selectedProject"
          />

          <!-- Reviews -->
          <WorkReviews
            v-if="selectedTool === 'reviews'"
            :selected-project="selectedProject"
          />
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>

