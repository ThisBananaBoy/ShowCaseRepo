<script setup lang="ts">
import type { SelectMenuItem } from '@nuxt/ui'
import ContextTimer from './ContextTimer.vue'
import WorkStopwatch from '../work/WorkStopwatch.vue'
import { storeToRefs } from 'pinia'
import type { Project } from '~/types/Project'
import { useProjectStore } from '~/stores/useProjectStore'

type ToolType = 'pomodoro' | 'stopwatch'

const selectedTool = ref<ToolType>('pomodoro')
const selectedProject = ref<string | null>(null)

const projectStore = useProjectStore()
const { projects } = storeToRefs(projectStore)

const projectOptions = computed(() =>
  projectStore.activeProjects.map((project: Project) => ({ value: project.id, label: project.name }))
)

onMounted(() => {
  if (!projects.value.length) {
    projectStore.fetchProjects()
  }
})

function handleToolSelect(toolId: ToolType) {
  selectedTool.value = toolId
}

function handleProjectChange(value: SelectMenuItem) {
  if (typeof value === 'object' && value !== null && 'value' in value) {
    selectedProject.value = value.value as string | null
  } else if (typeof value === 'string') {
    selectedProject.value = value
  } else {
    selectedProject.value = null
  }
}
</script>

<template>
  <div class="h-full flex flex-col">
    <!-- Tool Selection -->
    <div class="p-4 border-b border-default space-y-3">
      <div class="space-y-2">
        <UButton
          v-for="tool in [
            { id: 'pomodoro', label: 'Pomodoro Timer', icon: 'i-lucide-timer' },
            { id: 'stopwatch', label: 'Stoppuhr', icon: 'i-lucide-clock' },
          ]"
          :key="tool.id"
          :variant="selectedTool === tool.id ? 'solid' : 'ghost'"
          :color="selectedTool === tool.id ? 'primary' : 'neutral'"
          class="w-full justify-start"
          :icon="tool.icon"
          @click="handleToolSelect(tool.id as ToolType)"
        >
          {{ tool.label }}
        </UButton>
      </div>

      <!-- Project Selection -->
      <div class="pt-4 border-t border-default">
        <h3 class="text-sm font-semibold text-muted-foreground mb-3 px-2">
          Projekt zuordnen
        </h3>
        <USelectMenu
          :model-value="selectedProject"
          :options="[
            { value: null, label: 'Kein Projekt' },
            ...projectOptions,
          ]"
          option-attribute="label"
          value-attribute="value"
          placeholder="Projekt auswählen (optional)"
          size="md"
          @update:model-value="handleProjectChange"
        />
      </div>
    </div>

    <!-- Content -->
    <div class="flex-1 overflow-y-auto p-4">
      <ContextTimer
        v-if="selectedTool === 'pomodoro'"
        :selected-project="selectedProject"
      />
      <WorkStopwatch
        v-else-if="selectedTool === 'stopwatch'"
        :selected-project="selectedProject"
      />
    </div>
  </div>
</template>

