<script setup lang="ts">
import { onUnmounted } from 'vue'
import type { Task } from '~/types/Task'
import type { Appointment } from '~/types/Appointment'
import { useTaskStore } from '~/stores/useTaskStore'
import { isTaskCompleted, isTaskToday } from '~/utils/taskHelpers'
import { useAppointmentStore } from '~/stores/useAppointmentStore'
import { useProjectStore } from '~/stores/useProjectStore'
import { storeToRefs } from 'pinia'

const taskStore = useTaskStore()
const appointmentStore = useAppointmentStore()
const projectStore = useProjectStore()

const { tasks } = storeToRefs(taskStore)
const { activeProjects } = storeToRefs(projectStore)

// Projekt-Farben
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
  const projectIndex = activeProjects.value.findIndex(p => p.id === projectId)
  if (projectIndex === -1) return 'bg-neutral'
  return projectColors[projectIndex % projectColors.length]!
}

function getProjectBorderColor(projectId: string): string {
  const projectIndex = activeProjects.value.findIndex(p => p.id === projectId)
  if (projectIndex === -1) return 'border-muted-foreground/20'
  return projectBorderColors[projectIndex % projectBorderColors.length]!
}

const isToday = (value?: Date) => {
  if (!value) return false
  const date = new Date(value)
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  date.setHours(0, 0, 0, 0)
  return date.getTime() === today.getTime()
}

const getTodayTasks = () =>
  tasks.value.filter(task => isTaskToday(task))

const getAvailableTasks = () =>
  tasks.value.filter(task => {
    const scheduled = task.start_time && task.end_time
    const isPlannedToday = isTaskToday(task)
    return !scheduled && !isPlannedToday && !isTaskCompleted(task)
  })

const findTaskById = (id: string) =>
  tasks.value.find(task => task.id === id) as Task | undefined

const todayAppointments = computed(() => appointmentStore.getTodayAppointments())

const tasksWithTime = computed(() =>
  getTodayTasks().filter(t => t.start_time && t.end_time)
)

const availableTasks = computed(() => getAvailableTasks())

// Tasks nach Projekten gruppieren
const tasksByProject = computed(() => {
  const grouped: Record<string, Task[]> = {}
  const noProjectTasks: Task[] = []

  availableTasks.value.forEach((task: Task) => {
    if (task.project_id) {
      if (!grouped[task.project_id]) {
        grouped[task.project_id] = []
      }
      grouped[task.project_id]!.push(task)
    } else {
      noProjectTasks.push(task)
    }
  })

  return { grouped, noProjectTasks }
})

const draggedTask = ref<Task | null>(null)
const dragOverTimeSchedule = ref(false)
const resizingTask = ref<{ task: Task, edge: 'top' | 'bottom' } | null>(null)
const draggingTimedItem = ref<{ task?: Task, appointment?: Appointment, startY: number, startTime: Date } | null>(null)
const resizeStartY = ref(0)
const resizeStartTime = ref<Date | null>(null)

// Konvertiere Y-Position im Zeitraster zu Zeit
function getTimeFromPosition(y: number, containerElement: HTMLElement): Date {
  const rect = containerElement.getBoundingClientRect()
  const relativeY = y - rect.top
  const percentage = (relativeY / rect.height) * 100

  const startRange = 6 * 60 // 6 Uhr
  const endRange = 22 * 60 // 22 Uhr
  const totalRange = endRange - startRange

  const totalMinutes = startRange + (percentage / 100) * totalRange
  const hours = Math.floor(totalMinutes / 60)
  const minutes = Math.floor(totalMinutes % 60)

  const today = new Date()
  const time = new Date(today)
  time.setHours(hours, minutes, 0, 0)

  return time
}

// Runde Zeit auf 15-Minuten-Intervalle
function roundToQuarterHour(date: Date): Date {
  const minutes = date.getMinutes()
  const roundedMinutes = Math.round(minutes / 15) * 15
  const rounded = new Date(date)
  rounded.setMinutes(roundedMinutes, 0, 0)
  return rounded
}

function onDragOverTimeSchedule(event: DragEvent) {
  event.preventDefault()
  dragOverTimeSchedule.value = true
}

function onDragLeaveTimeSchedule() {
  dragOverTimeSchedule.value = false
}

function onDropToTimeSchedule(event: DragEvent) {
  event.preventDefault()
  dragOverTimeSchedule.value = false

  if (!draggedTask.value) return

  const container = event.currentTarget as HTMLElement
  const time = roundToQuarterHour(getTimeFromPosition(event.clientY, container))

  // Standard-Dauer: 1 Stunde
  const endTime = new Date(time)
  endTime.setHours(endTime.getHours() + 1)

  const task = findTaskById(draggedTask.value.id)
  if (!task) return

  void taskStore.updateTask(task.id, {
    start_time: time,
    end_time: endTime
  })

  draggedTask.value = null
}

function onDragStart(task: Task) {
  draggedTask.value = task
}

function onDragEnd() {
  draggedTask.value = null
  dragOverTimeSchedule.value = false
}

function onDropToSidebar() {
  // Wenn eine Task aus der Timeline zurück in die Sidebar gezogen wird
  if (draggedTask.value) {
    const task = findTaskById(draggedTask.value.id)
    if (task) {
      // Entferne Zeit-Informationen
      void taskStore.updateTask(task.id, {
        start_time: undefined,
        end_time: undefined
      })
    }
  }
  draggedTask.value = null
}

let mouseMoveHandler: ((e: MouseEvent) => void) | null = null
let mouseUpHandler: (() => void) | null = null

function startDragTimedItem(event: MouseEvent, task?: Task, appointment?: Appointment) {
  event.preventDefault()
  event.stopPropagation()

  // Cleanup existing handlers
  if (mouseMoveHandler) {
    document.removeEventListener('mousemove', mouseMoveHandler)
  }
  if (mouseUpHandler) {
    document.removeEventListener('mouseup', mouseUpHandler)
  }

  if (!task && !appointment) return

  const startTime = task ? new Date(task.start_time!) : new Date(appointment!.start_time)
  draggingTimedItem.value = { task, appointment, startY: event.clientY, startTime }

  const container = document.querySelector('.time-schedule-container') as HTMLElement
  if (!container) return

  const containerRect = container.getBoundingClientRect()
  const containerHeight = containerRect.height

  mouseMoveHandler = (e: MouseEvent) => {
    if (!draggingTimedItem.value) return

    const deltaY = e.clientY - draggingTimedItem.value.startY
    const deltaPercentage = (deltaY / containerHeight) * 100

    const startRange = 6 * 60 // 6 Uhr in Minuten
    const endRange = 22 * 60 // 22 Uhr in Minuten
    const totalRange = endRange - startRange
    const deltaMinutes = (deltaPercentage / 100) * totalRange

    const newStartTime = new Date(draggingTimedItem.value.startTime)
    newStartTime.setMinutes(newStartTime.getMinutes() + deltaMinutes)
    const rounded = roundToQuarterHour(newStartTime)

    if (draggingTimedItem.value.task) {
      const task = findTaskById(draggingTimedItem.value.task.id)
      if (!task || !task.start_time || !task.end_time) return

      const duration = task.end_time.getTime() - task.start_time.getTime()
      const newEndTime = new Date(rounded.getTime() + duration)

      // Boundary checks
      const startRangeMinutes = 6 * 60
      const endRangeMinutes = 22 * 60
      const roundedMinutes = rounded.getHours() * 60 + rounded.getMinutes()
      const newEndMinutes = newEndTime.getHours() * 60 + newEndTime.getMinutes()

      if (roundedMinutes >= startRangeMinutes && newEndMinutes <= endRangeMinutes) {
        task.start_time = rounded
        task.end_time = newEndTime
      }
    } else if (draggingTimedItem.value.appointment) {
      const appt = todayAppointments.value.find(a => a.id === draggingTimedItem.value!.appointment!.id)
      if (!appt) return

      const duration = appt.end_time.getTime() - appt.start_time.getTime()
      const newEndTime = new Date(rounded.getTime() + duration)

      // Boundary checks
      const startRangeMinutes = 6 * 60
      const endRangeMinutes = 22 * 60
      const roundedMinutes = rounded.getHours() * 60 + rounded.getMinutes()
      const newEndMinutes = newEndTime.getHours() * 60 + newEndTime.getMinutes()

      if (roundedMinutes >= startRangeMinutes && newEndMinutes <= endRangeMinutes) {
        appt.start_time = rounded
        appt.end_time = newEndTime
      }
    }
  }

  mouseUpHandler = () => {
    draggingTimedItem.value = null
    if (mouseMoveHandler) {
      document.removeEventListener('mousemove', mouseMoveHandler)
      mouseMoveHandler = null
    }
    if (mouseUpHandler) {
      document.removeEventListener('mouseup', mouseUpHandler)
      mouseUpHandler = null
    }
  }

  document.addEventListener('mousemove', mouseMoveHandler)
  document.addEventListener('mouseup', mouseUpHandler)
}

function startResize(task: Task, edge: 'top' | 'bottom', event: MouseEvent) {
  event.preventDefault()
  event.stopPropagation()

  // Cleanup existing handlers
  if (mouseMoveHandler) {
    document.removeEventListener('mousemove', mouseMoveHandler)
  }
  if (mouseUpHandler) {
    document.removeEventListener('mouseup', mouseUpHandler)
  }

  resizingTask.value = { task, edge }
  resizeStartY.value = event.clientY
  resizeStartTime.value = edge === 'top' ? new Date(task.start_time!) : new Date(task.end_time!)

  const container = document.querySelector('.time-schedule-container') as HTMLElement
  if (!container) return

  const containerRect = container.getBoundingClientRect()
  const containerHeight = containerRect.height

  mouseMoveHandler = (e: MouseEvent) => {
    if (!resizingTask.value) return

    const deltaY = e.clientY - resizeStartY.value
    const deltaPercentage = (deltaY / containerHeight) * 100

    const startRange = 6 * 60 // 6 Uhr in Minuten
    const endRange = 22 * 60 // 22 Uhr in Minuten
    const totalRange = endRange - startRange
    const deltaMinutes = (deltaPercentage / 100) * totalRange

    const task = findTaskById(resizingTask.value.task.id)
    if (!task || !task.start_time || !task.end_time) return

    if (edge === 'top') {
      const newStartTime = new Date(resizeStartTime.value!)
      newStartTime.setMinutes(newStartTime.getMinutes() + deltaMinutes)
      const rounded = roundToQuarterHour(newStartTime)

      // Verhindere dass Start nach Ende kommt
      if (rounded.getTime() < task.end_time.getTime()) {
        task.start_time = rounded
      }
    } else {
      const newEndTime = new Date(resizeStartTime.value!)
      newEndTime.setMinutes(newEndTime.getMinutes() + deltaMinutes)
      const rounded = roundToQuarterHour(newEndTime)

      // Verhindere dass Ende vor Start kommt
      if (rounded.getTime() > task.start_time.getTime()) {
        task.end_time = rounded
      }
    }
  }

  mouseUpHandler = () => {
    resizingTask.value = null
    resizeStartY.value = 0
    resizeStartTime.value = null
    if (mouseMoveHandler) {
      document.removeEventListener('mousemove', mouseMoveHandler)
      mouseMoveHandler = null
    }
    if (mouseUpHandler) {
      document.removeEventListener('mouseup', mouseUpHandler)
      mouseUpHandler = null
    }
  }

  document.addEventListener('mousemove', mouseMoveHandler)
  document.addEventListener('mouseup', mouseUpHandler)
}

onUnmounted(() => {
  // Cleanup event listeners on unmount
  if (mouseMoveHandler) {
    document.removeEventListener('mousemove', mouseMoveHandler)
  }
  if (mouseUpHandler) {
    document.removeEventListener('mouseup', mouseUpHandler)
  }
})
</script>

<template>
  <div class="h-full flex w-full overflow-hidden">
    <!-- Aufgaben Sidebar (links) -->
    <div class="w-80 border-r border-border flex flex-col overflow-hidden">
      <TasksSidebarContent
        :available-tasks="availableTasks"
        :tasks-by-project="tasksByProject"
        :active-projects="activeProjects"
        :get-project-color="getProjectColor"
        :get-project-border-color="getProjectBorderColor"
        :on-drag-start="onDragStart"
        :on-drag-end="onDragEnd"
        :on-drop-to-sidebar="onDropToSidebar"
        title="Verfügbare Aufgaben"
      />
    </div>

    <!-- Timeline (rechts) -->
    <div class="w-96 flex flex-col overflow-hidden">
      <Timeline
        :appointments="todayAppointments"
        :tasks-with-time="tasksWithTime"
        :get-project-color="getProjectColor"
        :get-project-border-color="getProjectBorderColor"
        :drag-over-time-schedule="dragOverTimeSchedule"
        :on-drag-over="onDragOverTimeSchedule"
        :on-drag-leave="onDragLeaveTimeSchedule"
        :on-drop="onDropToTimeSchedule"
        :on-resize="startResize"
        :on-drag-timed-item="startDragTimedItem"
      />
    </div>
  </div>
</template>

