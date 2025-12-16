<script setup lang="ts">
import type { Task } from '~/types/Task'
import { isTaskCompleted } from '~/utils/taskHelpers'

interface Props {
  availableTasks?: Task[]
  tasksByProject?: { grouped: Record<string, Task[]>, noProjectTasks: Task[] }
  activeProjects?: Array<{ id: string, name: string }>
  getProjectColor?: (projectId: string) => string
  getProjectBorderColor?: (projectId: string) => string
  onDragStart?: (task: Task) => void
  onDragEnd?: () => void
}

const props = defineProps<Props>()
</script>

<template>
  <div class="p-4 overflow-y-auto">
    <h3 class="font-semibold mb-4 text-sm uppercase text-muted-foreground">
      Verfügbare Aufgaben
    </h3>

    <div class="space-y-3">
      <!-- Tasks grouped by project -->
      <template
        v-for="project in activeProjects"
        :key="project.id"
      >
        <div
          v-if="tasksByProject?.grouped[project.id] && tasksByProject.grouped[project.id]!.length > 0"
        >
          <div class="flex items-center gap-2 mb-2">
            <div
              class="w-3 h-3 rounded-full shrink-0"
              :class="getProjectColor?.(project.id) || 'bg-neutral'"
            />
            <span class="font-semibold text-sm flex-1">{{ project.name }}</span>
            <UBadge :label="String(tasksByProject.grouped[project.id]!.length)" color="neutral" size="xs" />
          </div>

          <div class="space-y-1 ml-6">
            <div
              v-for="task in tasksByProject.grouped[project.id]"
              :key="task.id"
              class="cursor-move"
              :draggable="!!task.id"
              @dragstart="onDragStart?.(task)"
              @dragend="onDragEnd?.()"
            >
              <div
                class="px-2 py-1.5 rounded border hover:bg-muted/50 transition-colors flex items-center gap-2"
                :class="getProjectBorderColor?.(task.project_id || '') || 'border-muted-foreground/20'"
              >
                <UCheckbox :model-value="isTaskCompleted(task)" size="sm" />
                <p
                  :class="[
                    'text-xs flex-1 truncate',
                    isTaskCompleted(task) &&
                      'line-through text-muted-foreground',
                  ]"
                >
                  {{ task.name }}
                </p>
                <div
                  v-if="task.project_id"
                  class="w-2 h-2 rounded-full shrink-0"
                  :class="getProjectColor?.(task.project_id) || 'bg-neutral'"
                />
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Tasks without project -->
      <div v-if="tasksByProject?.noProjectTasks && tasksByProject.noProjectTasks.length > 0">
        <div class="flex items-center gap-2 mb-2">
          <UIcon name="i-lucide-folder-x" class="size-4 text-muted-foreground" />
          <span class="font-semibold text-sm flex-1">Ohne Projekt</span>
          <UBadge :label="String(tasksByProject.noProjectTasks.length)" color="neutral" size="xs" />
        </div>

        <div class="space-y-1 ml-6">
          <div
            v-for="task in tasksByProject.noProjectTasks"
            :key="task.id"
            class="cursor-move"
            :draggable="!!task.id"
            @dragstart="onDragStart?.(task)"
            @dragend="onDragEnd?.()"
          >
            <div
              class="px-2 py-1.5 rounded border hover:bg-muted/50 transition-colors flex items-center gap-2 border-muted-foreground/20"
            >
              <UCheckbox :model-value="task.is_completed" size="sm" />
              <p
                :class="[
                  'text-xs flex-1 truncate',
                  task.is_completed &&
                    'line-through text-muted-foreground',
                ]"
              >
                {{ task.name }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <div
        v-if="!availableTasks || availableTasks.length === 0"
        class="text-center py-8 text-muted-foreground text-sm"
      >
        <UIcon name="i-lucide-check-circle" class="size-12 mx-auto mb-2 opacity-50" />
        Keine offenen Aufgaben
      </div>
    </div>
  </div>
</template>

