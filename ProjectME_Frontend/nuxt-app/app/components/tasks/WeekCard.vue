<script setup lang="ts">
import { de } from "date-fns/locale";
import type { Task } from "~/types/Task";

import { getWeek, format } from "date-fns";

import TaskItem from "~/components/tasks/TaskItem.vue";
import TaskCompletionZone from "~/components/tasks/TaskCompletionZone.vue";

interface Props {
  weekStart: Date;
  tasks: Task[];
  completedTasks: Task[];
  isEditing?: boolean;
  isHovered?: boolean;
  isDraggedOver?: boolean;
  showCompletedPanel?: boolean;
  getProjectColor?: (projectId: string) => string;
  getProjectBorderColor?: (projectId: string) => string;
}

const props = withDefaults(defineProps<Props>(), {
  tasks: () => [],
  completedTasks: () => [],
  isEditing: false,
  isHovered: false,
  isDraggedOver: false,
  showCompletedPanel: false,
});

const emit = defineEmits<{
  navigate: [weekStart: Date];
  drop: [event: DragEvent, weekStart: Date, isCompleteZone?: boolean];
  dragOver: [event: DragEvent];
  mouseEnter: [weekStart: Date];
  mouseLeave: [];
  taskDragStart: [task: Task, event: DragEvent];
  taskDragEnd: [];
  toggleCompletedPanel: [weekStart: Date];
  completeZoneDragOver: [event: DragEvent];
  completeZoneDragLeave: [];
  completeZoneMouseEnter: [weekStart: Date];
  completeZoneMouseLeave: [];
}>();

function getWeekNumber(date: Date) {
  return getWeek(date, { locale: de, weekStartsOn: 1 });
}

const weekNumber = computed(() => getWeekNumber(props.weekStart));

function handleClick() {
  if (!props.isEditing) {
    emit("navigate", props.weekStart);
  }
}

function handleDrop(event: DragEvent) {
  emit("drop", event, props.weekStart);
}

function handleCompleteZoneDrop(event: DragEvent) {
  emit("drop", event, props.weekStart, true);
}
</script>

<template>
  <div
    :class="[
      'border rounded-lg p-4 h-full bg-muted/30 transition-all duration-300 hover:shadow-lg relative group flex flex-col hover:scale-[1.02] w-full',
      props.isEditing ? '' : 'cursor-pointer',
    ]"
    @click.stop="handleClick"
    @drop="handleDrop"
    @dragover="emit('dragOver', $event)"
    @mouseenter="emit('mouseEnter', weekStart)"
    @mouseleave="emit('mouseLeave')"
  >
    <div class="font-semibold mb-3 text-sm">
      KW {{ weekNumber }}
      <span class="text-muted-foreground text-xs ml-2">
        {{ format(weekStart, "dd.MM.", { locale: de }) }}
      </span>
    </div>
    <div class="space-y-1 flex-1">
      <!-- Existing Tasks -->
      <TaskItem
        v-for="task in tasks"
        :key="task.id"
        :task="task"
        :get-project-color="getProjectColor"
        :get-project-border-color="getProjectBorderColor"
        @drag-start="
          (task, event) => {
            if (event) emit('taskDragStart', task, event);
          }
        "
        @drag-end="emit('taskDragEnd')"
      />

      <slot name="add-task-button" />
    </div>

    <!-- Complete Zone -->
    <TaskCompletionZone
      :completed-tasks="completedTasks"
      :is-hovered="isHovered"
      :is-dragged-over="isDraggedOver"
      :is-open="showCompletedPanel"
      :get-project-color="getProjectColor"
      size="md"
      :show-count="false"
      @drop="handleCompleteZoneDrop"
      @dragover="emit('completeZoneDragOver', $event)"
      @dragleave="emit('completeZoneDragLeave')"
      @mouseenter="emit('completeZoneMouseEnter', weekStart)"
      @mouseleave="emit('completeZoneMouseLeave')"
      @click="props.isEditing ? null : emit('toggleCompletedPanel', weekStart)"
      @task-drag-start="(task, e) => emit('taskDragStart', task, e)"
    />
  </div>
</template>
