<script setup lang="ts">
import type { Task } from '~/types/Task'
import { isTaskCompleted } from '~/utils/taskHelpers'

interface Props {
  task: Task
  draggable?: boolean
  showRemoveButton?: boolean
  getProjectColor?: (projectId: string) => string
  getProjectBorderColor?: (projectId: string) => string
  size?: 'sm' | 'md'
}

const props = withDefaults(defineProps<Props>(), {
  draggable: true,
  showRemoveButton: false,
  size: 'md'
})

const emit = defineEmits<{
  'drag-start': [task: Task, event?: DragEvent]
  'drag-end': []
  'remove': [taskId: string]
}>()

function handleDragStart(event: DragEvent) {
  emit('drag-start', props.task, event)
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
  }
}

function handleDragEnd() {
  emit('drag-end')
}

function handleRemove() {
  emit('remove', props.task.id)
}

</script>

<template>
  <div
    class="cursor-move"
    :draggable="draggable && !!task.id"
    @dragstart="handleDragStart"
    @dragend="handleDragEnd"
    @click.stop
  >
    <div
      :class="[
        size === 'sm' ? 'px-2 py-1' : 'px-2 py-1.5',
        'rounded border hover:bg-muted/50 transition-colors flex items-center gap-2',
        task.project_id && getProjectBorderColor
          ? getProjectBorderColor(task.project_id)
          : 'border-muted-foreground/20',
      ]"
    >
      <p
        :class="[
          'text-xs flex-1 wrap-break-word',
          isTaskCompleted(task) && 'line-through text-muted-foreground',
        ]"
      >
        {{ task.name }}
      </p>
      <div
        v-if="task.project_id && getProjectColor"
        class="w-2 h-2 rounded-full shrink-0"
        :class="getProjectColor(task.project_id)"
      />
      <UButton
        v-if="showRemoveButton"
        icon="i-lucide-x"
        color="neutral"
        variant="ghost"
        size="xs"
        square
        @click.stop="handleRemove"
      />
    </div>
  </div>
</template>

