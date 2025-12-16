<script setup lang="ts">
import type { Task } from '~/types/Task'

interface Props {
  completedTasks: Task[]
  isHovered?: boolean
  isDraggedOver?: boolean
  isOpen?: boolean
  getProjectColor?: (projectId: string) => string
  size?: 'sm' | 'md'
  emptyText?: string
  showCount?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  completedTasks: () => [],
  isHovered: false,
  isDraggedOver: false,
  isOpen: false,
  size: 'md',
  emptyText: 'Keine erledigten Aufgaben',
  showCount: true
})

const emit = defineEmits<{
  'drop': [event: DragEvent]
  'dragover': [event: DragEvent]
  'dragleave': []
  'mouseenter': []
  'mouseleave': []
  'click': []
  'task-drag-start': [task: Task, event: DragEvent]
}>()

function handleDrop(event: DragEvent) {
  event.preventDefault()
  emit('drop', event)
}

function handleDragOver(event: DragEvent) {
  event.preventDefault()
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'move'
  }
  emit('dragover', event)
}

function handleDragLeave() {
  emit('dragleave')
}

function handleMouseEnter() {
  emit('mouseenter')
}

function handleMouseLeave() {
  emit('mouseleave')
}

function handleClick() {
  emit('click')
}

function handleTaskDragStart(task: Task, event: DragEvent) {
  emit('task-drag-start', task, event)
}

const iconSize = props.size === 'sm' ? 'w-3 h-3' : 'w-4 h-4'
const textSize = props.size === 'sm' ? 'text-[10px]' : 'text-xs'
const paddingSize = props.size === 'sm' ? 'px-2 py-1' : 'px-2 py-1.5'
</script>

<template>
  <div
    class="mt-auto pt-2 border-t border-dashed transition-all duration-200 relative shrink-0"
    :class="{
      'border-primary/50 bg-primary/5': isHovered || isDraggedOver,
      'border-muted-foreground/20': !(isHovered || isDraggedOver),
    }"
    @drop="handleDrop"
    @dragover="handleDragOver"
    @dragleave="handleDragLeave"
    @mouseenter="handleMouseEnter"
    @mouseleave="handleMouseLeave"
    @click.stop="handleClick"
  >
    <!-- Text/Icon - nur wenn Panel geschlossen -->
    <Transition name="fade">
      <div
        v-if="!isOpen"
        class="flex items-center justify-center gap-2 min-h-8"
      >
        <UIcon
          name="i-lucide-check-circle-2"
          :class="[
            iconSize,
            'transition-all duration-200',
            {
              'text-primary scale-110': isHovered || isDraggedOver,
              'text-muted-foreground': !(isHovered || isDraggedOver),
            }
          ]"
        />
        <span
          v-if="showCount"
          :class="[
            textSize,
            'transition-all duration-200',
            {
              'text-primary font-medium': isHovered || isDraggedOver,
              'text-muted-foreground': !(isHovered || isDraggedOver),
            }
          ]"
        >
          {{ completedTasks.length > 0 ? `${completedTasks.length}` : '' }}
        </span>
        <span
          v-else-if="completedTasks.length === 0"
          :class="[
            textSize,
            'transition-all duration-200',
            {
              'text-primary font-medium': isHovered || isDraggedOver,
              'text-muted-foreground': !(isHovered || isDraggedOver),
            }
          ]"
        >
          Zum Erledigen hier ablegen
        </span>
        <span
          v-else
          :class="[
            textSize,
            'transition-all duration-200',
            {
              'text-primary font-medium': isHovered || isDraggedOver,
              'text-muted-foreground': !(isHovered || isDraggedOver),
            }
          ]"
        >
          {{ `${completedTasks.length} erledigt` }}
        </span>
      </div>
    </Transition>

    <!-- Completed Tasks Panel -->
    <Transition name="slide-down">
      <div
        v-if="isOpen"
        :class="[
          'space-y-1 overflow-y-auto',
          size === 'sm' ? 'max-h-[150px]' : 'max-h-[200px]'
        ]"
      >
        <div
          v-for="task in completedTasks"
          :key="task.id"
          class="cursor-move"
          :draggable="!!task.id"
          @dragstart="(e) => handleTaskDragStart(task, e as DragEvent)"
        >
          <div
            :class="[
              paddingSize,
              'rounded border border-muted-foreground/20 bg-muted/30 hover:bg-muted/50 transition-colors flex items-center gap-2 opacity-60'
            ]"
          >
            <UIcon name="i-lucide-check-circle-2" :class="[iconSize, 'text-success shrink-0']" />
            <p :class="[textSize, 'flex-1 line-through text-muted-foreground', size === 'sm' ? 'wrap-break-word' : 'truncate']">
              {{ task.name }}
            </p>
            <div
              v-if="task.project_id && getProjectColor"
              class="w-2 h-2 rounded-full shrink-0"
              :class="getProjectColor(task.project_id)"
            />
          </div>
        </div>
        <div
          v-if="completedTasks.length === 0"
          :class="[
            'text-center py-2 text-muted-foreground',
            size === 'sm' ? 'text-[10px] py-2' : 'text-xs py-4'
          ]"
        >
          {{ emptyText }}
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
  overflow: hidden;
}

.slide-down-enter-from {
  opacity: 0;
  max-height: 0;
  transform: translateY(-10px);
}

.slide-down-leave-to {
  opacity: 0;
  max-height: 0;
  transform: translateY(-10px);
}
</style>

