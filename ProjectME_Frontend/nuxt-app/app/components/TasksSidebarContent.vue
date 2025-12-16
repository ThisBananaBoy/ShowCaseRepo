<script setup lang="ts">
import type { Task } from "~/types/Task";
import { isTaskCompleted } from '~/utils/taskHelpers'

interface Props {
  availableTasks?: Task[];
  tasksByProject?: { grouped: Record<string, Task[]>; noProjectTasks: Task[] };
  activeProjects?: Array<{ id: string; name: string }>;
  getProjectColor?: (projectId: string) => string;
  getProjectBorderColor?: (projectId: string) => string;
  onDragStart?: (task: Task) => void;
  onDragEnd?: () => void;
  onDropToSidebar?: () => void;
  title?: string;
}

const props = withDefaults(defineProps<Props>(), {
  title: "Verfügbare Aufgaben",
});

const hoveredDropZone = ref<string | null>(null);

function handleSidebarDragStart(event: DragEvent, task: Task) {
  if (!task.id) return;
  props.onDragStart?.(task);
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", task.id);
  }
}

function handleDragOver(event: DragEvent) {
  event.preventDefault();
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = "move";
  }
}

function handleDrop(event: DragEvent) {
  event.preventDefault();
  hoveredDropZone.value = null;
  props.onDropToSidebar?.();
}
</script>

<template>
  <div
    class="p-4 overflow-y-auto h-full"
    @drop="handleDrop"
    @dragover="handleDragOver"
    @dragleave="hoveredDropZone = null"
  >
    <h3 class="font-semibold mb-4 text-sm uppercase text-muted-foreground">
      {{ title }}
    </h3>

    <div class="space-y-3">
      <!-- All Projects (always shown, even without tasks) -->
      <template v-for="project in activeProjects" :key="project.id">
        <div>
          <div class="flex items-center gap-2 mb-2">
            <div
              class="w-3 h-3 rounded-full shrink-0"
              :class="getProjectColor?.(project.id) || 'bg-neutral'"
            />
            <span class="font-semibold text-sm flex-1">{{ project.name }}</span>
            <UBadge
              v-if="tasksByProject?.grouped[project.id] && tasksByProject.grouped[project.id]!.length > 0"
              :label="String(tasksByProject.grouped[project.id]!.length)"
              color="neutral"
              size="xs"
            />
          </div>

          <!-- Tasks for this project -->
          <div
            v-if="tasksByProject?.grouped[project.id] && tasksByProject.grouped[project.id]!.length > 0"
            class="space-y-1 ml-6"
          >
            <div
              v-for="task in tasksByProject.grouped[project.id]"
              :key="task.id"
              class="cursor-move"
              :draggable="!!task.id"
              @dragstart="handleSidebarDragStart($event, task)"
              @dragend="onDragEnd?.()"
            >
              <div
                class="px-2 py-1.5 rounded border hover:bg-muted/50 transition-colors flex items-center gap-2"
                :class="
                  getProjectBorderColor?.(task.project_id || '') ||
                  'border-muted-foreground/20'
                "
              >
                <UCheckbox :model-value="isTaskCompleted(task)" size="sm" />
                <p
                  :class="[
                    'text-xs flex-1 truncate',
                    isTaskCompleted(task) && 'line-through text-muted-foreground',
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

          <!-- Subtle "no tasks" message -->
          <p v-else class="text-xs text-muted-foreground/50 ml-6 italic">
            Keine offenen Aufgaben
          </p>
        </div>
      </template>

      <!-- Tasks without project -->
      <div
        v-if="
          tasksByProject?.noProjectTasks &&
          tasksByProject.noProjectTasks.length > 0
        "
      >
        <div class="flex items-center gap-2 mb-2">
          <UIcon
            name="i-lucide-folder-x"
            class="size-4 text-muted-foreground"
          />
          <span class="font-semibold text-sm flex-1">Ohne Projekt</span>
          <UBadge
            :label="String(tasksByProject.noProjectTasks.length)"
            color="neutral"
            size="xs"
          />
        </div>

        <div class="space-y-1 ml-6">
          <div
            v-for="task in tasksByProject.noProjectTasks"
            :key="task.id"
            class="cursor-move"
            :draggable="!!task.id"
            @dragstart="handleSidebarDragStart($event, task)"
            @dragend="onDragEnd?.()"
          >
            <div
              class="px-2 py-1.5 rounded border hover:bg-muted/50 transition-colors flex items-center gap-2 border-muted-foreground/20"
            >
              <UCheckbox :model-value="task.is_completed" size="sm" />
              <p
                :class="[
                  'text-xs flex-1 truncate',
                  task.is_completed && 'line-through text-muted-foreground',
                ]"
              >
                {{ task.name }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
