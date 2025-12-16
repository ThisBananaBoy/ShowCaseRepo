<script setup lang="ts">
import { nextTick, onMounted, onUnmounted } from 'vue'
import { format, getWeek } from 'date-fns'
import { de } from 'date-fns/locale'
import type { Task } from '~/types/Task'
import type { Appointment } from '~/types/Appointment'
import Timeline from '~/components/Timeline.vue'
import TaskItem from '~/components/tasks/TaskItem.vue'
import AddTaskButton from '~/components/tasks/AddTaskButton.vue'
import TaskCompletionZone from '~/components/tasks/TaskCompletionZone.vue'
import { useTaskStore } from '~/stores/useTaskStore'
import { useProjectStore } from '~/stores/useProjectStore'
import { useAppointmentStore } from '~/stores/useAppointmentStore'
import { storeToRefs } from 'pinia'
import { isTaskCompleted } from '~/utils/taskHelpers'

const props = defineProps<{
  days: Date[]
  projectId?: string  // Für Filterung der Tasks
  getProjectColor?: (projectId: string) => string
  getProjectBorderColor?: (projectId: string) => string
  // Externe Drag-Koordination (z.B. von Sidebar)
  onExternalTaskDrop?: (taskId: string, day: Date, isCompleteZone?: boolean) => void
  compact?: boolean
}>()

// Store-Zugriff
const taskStore = useTaskStore()
const projectStore = useProjectStore()
const appointmentStore = useAppointmentStore()
const { tasks } = storeToRefs(taskStore)
const { projects } = storeToRefs(projectStore)
const { appointments } = storeToRefs(appointmentStore)

// Gefilterte Tasks basierend auf projectId
const filteredTasks = computed(() => {
  if (props.projectId) {
    return tasks.value.filter(task => task.project_id === props.projectId)
  }
  return tasks.value
})

// Aktive Projekte für Projekt-Auswahlmenü
const activeProjects = computed(() =>
  projects.value.filter(p => p.status === 'active')
)

// Projekt-Farben (für visuelle Unterscheidung)
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

const getProjectColorFn = (projectId: string): string => {
  if (props.getProjectColor) {
    return props.getProjectColor(projectId)
  }
  const projectIndex = activeProjects.value.findIndex(p => p.id === projectId)
  if (projectIndex === -1) return 'bg-neutral'
  return projectColors[projectIndex % projectColors.length]!
}

const getProjectBorderColorFn = (projectId: string): string => {
  if (props.getProjectBorderColor) {
    return props.getProjectBorderColor(projectId)
  }
  const projectIndex = activeProjects.value.findIndex(p => p.id === projectId)
  if (projectIndex === -1) return 'border-muted-foreground/20'
  return projectBorderColors[projectIndex % projectBorderColors.length]!
}

function getWeekNumber(date: Date) {
  return getWeek(date, { locale: de, weekStartsOn: 1 })
}

// Tasks nach Tag gruppieren
const tasksByDay = computed(() => {
  const grouped: Record<string, Task[]> = {}
  filteredTasks.value.forEach((task) => {
    if (task.due_date) {
      const taskDate = task.due_date instanceof Date ? task.due_date : new Date(task.due_date)
      const dayKey = format(taskDate, 'yyyy-MM-dd')
      if (!grouped[dayKey]) grouped[dayKey] = []
      grouped[dayKey].push(task)
    }
  })
  return grouped
})

function getTasksForDay(day: Date): Task[] {
  const dayKey = format(day, 'yyyy-MM-dd')
  const dayTasks = tasksByDay.value[dayKey] || []
  return dayTasks.filter(task => !isTaskCompleted(task))
}

function getCompletedTasksForDay(day: Date): Task[] {
  const dayKey = format(day, 'yyyy-MM-dd')
  const dayTasks = tasksByDay.value[dayKey] || []
  return dayTasks.filter(task => isTaskCompleted(task))
}

function getTasksWithTimeForDay(day: Date): Task[] {
  const dayKey = format(day, 'yyyy-MM-dd')
  return filteredTasks.value.filter(task => {
    if (!task.start_time || !task.end_time) return false
    try {
      const taskDate = task.start_time instanceof Date ? task.start_time : new Date(task.start_time)
      if (isNaN(taskDate.getTime())) return false
      const taskDateKey = format(taskDate, 'yyyy-MM-dd')
      return taskDateKey === dayKey
    } catch {
      return false
    }
  })
}

// Aktuelle Zeit für Indikator
const currentTime = ref(new Date())


// Drag & Drop State
const dragOverTimeScheduleByDay = ref<Record<string, boolean>>({})
const draggedTask = ref<Task | null>(null)
const draggedTaskId = ref<string | null>(null)  // Für externe Drags (z.B. Sidebar)

// Konvertiere Y-Position im Zeitraster zu Zeit
function getTimeFromPosition(y: number, containerElement: HTMLElement, day: Date): Date {
  const rect = containerElement.getBoundingClientRect()
  const relativeY = y - rect.top
  const percentage = (relativeY / rect.height) * 100

  const startRange = 6 * 60 // 6 Uhr
  const endRange = 22 * 60 // 22 Uhr
  const totalRange = endRange - startRange

  const totalMinutes = startRange + (percentage / 100) * totalRange
  const hours = Math.floor(totalMinutes / 60)
  const minutes = Math.floor(totalMinutes % 60)

  const time = new Date(day)
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

// Handler für Drag Over auf Timeline
function handleTimelineDragOver(day: Date, event: DragEvent) {
  event.preventDefault()
  const dayKey = format(day, "yyyy-MM-dd")
  dragOverTimeScheduleByDay.value[dayKey] = true
}

// Handler für Drag Leave von Timeline
function handleTimelineDragLeave(day: Date) {
  const dayKey = format(day, "yyyy-MM-dd")
  dragOverTimeScheduleByDay.value[dayKey] = false
}

// Handler für Drop auf Timeline
async function handleTimelineDrop(day: Date, event: DragEvent) {
  event.preventDefault()
  const dayKey = format(day, "yyyy-MM-dd")
  dragOverTimeScheduleByDay.value[dayKey] = false

  const container = event.currentTarget as HTMLElement
  const time = roundToQuarterHour(getTimeFromPosition(event.clientY, container, day))

  // Externe Drags (z.B. von Sidebar)
  if (draggedTaskId.value) {
    if (props.onExternalTaskDrop) {
      await props.onExternalTaskDrop(draggedTaskId.value, day, false)
    }
    draggedTaskId.value = null
    return
  }

  // Interne Drags
  if (draggedTask.value && draggedTask.value.id) {
    const existing = tasks.value.find(t => t.id === draggedTask.value!.id)
    if (existing) {
      const endTime = new Date(time)
      endTime.setHours(endTime.getHours() + 1)
      await taskStore.updateTask(existing.id, { due_date: day, start_time: time, end_time: endTime })
    }
  }

  draggedTask.value = null
}

// Handler für Drag Start von Task
function handleTaskDragStart(task: Task, event: DragEvent) {
  draggedTask.value = task
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
  }
}

// Handler für Drag End
function handleTaskDragEnd() {
  draggedTask.value = null
}

// State für Timeline Resize und Drag
const resizingTask = ref<{ task: Task, edge: 'top' | 'bottom' } | null>(null)
const draggingTimedItem = ref<{ task?: Task, appointment?: Appointment, startY: number, startTime: Date, day: Date } | null>(null)
const resizeStartY = ref(0)
const resizeStartTime = ref<Date | null>(null)

let mouseMoveHandler: ((e: MouseEvent) => void) | null = null
let mouseUpHandler: (() => void) | null = null

// Handler für Resize von Tasks in Timeline
function handleResize(task: Task, edge: 'top' | 'bottom', event: MouseEvent) {
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

  const container = (event.currentTarget as HTMLElement)?.closest?.('.time-schedule-container') as HTMLElement
  if (!container) return

  const containerRect = container.getBoundingClientRect()
  const containerHeight = containerRect.height

  // Speichere ursprünglichen Zustand für Rollback
  const originalTask = tasks.value.find(t => t.id === task.id)
  if (!originalTask) return
  const originalTaskState = { ...originalTask }

  let finalUpdate: Partial<Task> | null = null

  mouseMoveHandler = (e: MouseEvent) => {
    if (!resizingTask.value) return

    const deltaY = e.clientY - resizeStartY.value
    const deltaPercentage = (deltaY / containerHeight) * 100

    const startRange = 6 * 60 // 6 Uhr in Minuten
    const endRange = 22 * 60 // 22 Uhr in Minuten
    const totalRange = endRange - startRange
    const deltaMinutes = (deltaPercentage / 100) * totalRange

    const taskToResize = tasks.value.find(t => t.id === resizingTask.value!.task.id)
    if (!taskToResize || !taskToResize.start_time || !taskToResize.end_time) return

    if (edge === 'top') {
      const newStartTime = new Date(resizeStartTime.value!)
      newStartTime.setMinutes(newStartTime.getMinutes() + deltaMinutes)
      const rounded = roundToQuarterHour(newStartTime)

      // Verhindere dass Start nach Ende kommt
      if (rounded.getTime() < taskToResize.end_time.getTime()) {
        finalUpdate = { start_time: rounded }
      }
    } else {
      const newEndTime = new Date(resizeStartTime.value!)
      newEndTime.setMinutes(newEndTime.getMinutes() + deltaMinutes)
      const rounded = roundToQuarterHour(newEndTime)

      // Verhindere dass Ende vor Start kommt
      if (rounded.getTime() > taskToResize.start_time.getTime()) {
        finalUpdate = { end_time: rounded }
      }
    }
  }

  mouseUpHandler = async () => {
    if (resizingTask.value && finalUpdate) {
      const finalTaskState = { ...originalTaskState, ...finalUpdate }
      const { id, ...updateData } = finalTaskState
      await taskStore.updateTask(originalTaskState.id, updateData)
    }
    resizingTask.value = null
    finalUpdate = null
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

// Handler für Drag von Tasks/Appointments innerhalb der Timeline
function handleDragTimedItem(event: MouseEvent, task?: Task, appointment?: Appointment) {
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
  const day = task
    ? (task.start_time instanceof Date ? task.start_time : (task.start_time ? new Date(task.start_time) : new Date()))
    : (appointment!.start_time instanceof Date ? appointment!.start_time : new Date(appointment!.start_time))
  const taskDay = new Date(day)
  taskDay.setHours(0, 0, 0, 0)

  draggingTimedItem.value = { task, appointment, startY: event.clientY, startTime, day: taskDay }

  const container = (event.currentTarget as HTMLElement)?.closest?.('.time-schedule-container') as HTMLElement
  if (!container) return

  const containerRect = container.getBoundingClientRect()
  const containerHeight = containerRect.height

  // Speichere ursprünglichen Zustand für Rollback (nur für Tasks)
  let originalTaskState: Task | null = null
  if (task) {
    const originalTask = tasks.value.find(t => t.id === task.id)
    if (originalTask) {
      originalTaskState = { ...originalTask }
    }
  }

  let finalTaskUpdate: Partial<Task> | null = null

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
      const taskToDrag = tasks.value.find(t => t.id === draggingTimedItem.value!.task!.id)
      if (!taskToDrag || !taskToDrag.start_time || !taskToDrag.end_time) return

      const duration = taskToDrag.end_time.getTime() - taskToDrag.start_time.getTime()
      const newEndTime = new Date(rounded.getTime() + duration)

      // Boundary checks
      const startRangeMinutes = 6 * 60
      const endRangeMinutes = 22 * 60
      const roundedMinutes = rounded.getHours() * 60 + rounded.getMinutes()
      const newEndMinutes = newEndTime.getHours() * 60 + newEndTime.getMinutes()

      if (roundedMinutes >= startRangeMinutes && newEndMinutes <= endRangeMinutes) {
        finalTaskUpdate = { start_time: rounded, end_time: newEndTime }
      }
    } else if (draggingTimedItem.value.appointment) {
      const appt = appointments.value.find(a => a.id === draggingTimedItem.value!.appointment!.id)
      if (!appt) return

      const duration = appt.end_time.getTime() - appt.start_time.getTime()
      const newEndTime = new Date(rounded.getTime() + duration)

      // Boundary checks
      const startRangeMinutes = 6 * 60
      const endRangeMinutes = 22 * 60
      const roundedMinutes = rounded.getHours() * 60 + rounded.getMinutes()
      const newEndMinutes = newEndTime.getHours() * 60 + newEndTime.getMinutes()

      if (roundedMinutes >= startRangeMinutes && newEndMinutes <= endRangeMinutes) {
        // Appointment-Updates über AppointmentStore (wird später implementiert)
        // appt.start_time = rounded
        // appt.end_time = newEndTime
      }
    }
  }

  mouseUpHandler = async () => {
    if (draggingTimedItem.value?.task && finalTaskUpdate && originalTaskState) {
      const finalTaskState = { ...originalTaskState, ...finalTaskUpdate }
      const { id, ...updateData } = finalTaskState
      await taskStore.updateTask(originalTaskState.id, updateData)
    }
    draggingTimedItem.value = null
    finalTaskUpdate = null
    originalTaskState = null
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

// State für Aufgaben-Erstellung pro Tag
const editingNewTaskByDay = ref<Record<string, boolean>>({})
const newTaskNameByDay = ref<Record<string, string>>({})
const showProjectMenuByDay = ref<Record<string, boolean>>({})
const projectMenuPosition = ref({ x: 0, y: 0 })
const selectedProjectForTask = ref<string | null>(null)
const editingDay = ref<Date | null>(null)
const hoveredCompleteZone = ref<Date | null>(null)
const showCompletedPanel = ref<Date | null>(null)
const draggedOverCompleteZone = ref(false)
const isTimelineExpanded = ref(true)

// Handler für Aufgaben-Erstellung
function showProjectSelection(event?: MouseEvent, day?: Date) {
  if (event) {
    event.stopPropagation()
    projectMenuPosition.value = { x: event.clientX, y: event.clientY }
  }
  if (day) {
    const dayKey = format(day, "yyyy-MM-dd")
    showProjectMenuByDay.value[dayKey] = true
    editingDay.value = day
  }
}

function selectProject(projectId: string, day?: Date) {
  selectedProjectForTask.value = projectId
  if (day) {
    const dayKey = format(day, "yyyy-MM-dd")
    showProjectMenuByDay.value[dayKey] = false
    editingNewTaskByDay.value[dayKey] = true
    newTaskNameByDay.value[dayKey] = ""
    editingDay.value = day
    nextTick(() => {
      const input = document.querySelector(".new-task-input") as HTMLInputElement
      input?.focus()
    })
  }
}

function cancelNewTask(day: Date) {
  const dayKey = format(day, "yyyy-MM-dd")
  editingNewTaskByDay.value[dayKey] = false
  newTaskNameByDay.value[dayKey] = ""
  selectedProjectForTask.value = null
  showProjectMenuByDay.value[dayKey] = false
  editingDay.value = null
}

async function createTask(day: Date) {
  const dayKey = format(day, "yyyy-MM-dd")
  const taskName = newTaskNameByDay.value[dayKey]?.trim()
  if (!taskName) {
    cancelNewTask(day)
    return
  }

  const projectId = selectedProjectForTask.value || props.projectId || undefined
  await taskStore.createTask({
    name: taskName,
    project_id: projectId,
    due_date: day,
    status: 'todo' as const,
    priority: 2
  })
  cancelNewTask(day)
}

function toggleCompletedPanel(day: Date) {
  if (showCompletedPanel.value?.getTime() === day.getTime()) {
    showCompletedPanel.value = null
  } else {
    showCompletedPanel.value = day
  }
}

function handleCompleteZoneDragOver(event: DragEvent) {
  event.preventDefault()
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = "move"
  }
  draggedOverCompleteZone.value = true
}

function handleCompleteZoneDragLeave() {
  draggedOverCompleteZone.value = false
}

// Handler für Task Drop (interne + externe Drags)
async function handleTaskDrop(event: DragEvent, day: Date, isCompleteZone = false) {
  event.preventDefault()

  // Externe Drags (z.B. von Sidebar) - erkennen über draggedTaskId
  if (draggedTaskId.value) {
    if (props.onExternalTaskDrop) {
      await props.onExternalTaskDrop(draggedTaskId.value, day, isCompleteZone)
    }
    draggedTaskId.value = null
    return
  }

  // Interne Drags
  if (draggedTask.value && draggedTask.value.id) {
    const existing = tasks.value.find(t => t.id === draggedTask.value!.id)
      if (existing) {
        if (isCompleteZone) {
          await taskStore.updateTask(existing.id, { status: 'completed' as const, completed_at: new Date() })
        } else {
          await taskStore.updateTask(existing.id, { due_date: day, status: 'todo' as const, completed_at: undefined })
        }
      }
  }

  draggedTask.value = null
}

// Handler für Task Drag Over (erkennt externe Drags)
function handleTaskDragOver(event: DragEvent, _day: Date) {
  event.preventDefault()
  // Versuche Task-ID aus dataTransfer zu lesen (für externe Drags)
  if (event.dataTransfer) {
    const taskId = event.dataTransfer.getData('text/plain')
    if (taskId && !draggedTask.value) {
      draggedTaskId.value = taskId
    }
  }
}

// Click outside handler für Projekt-Menü
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  const anyMenuOpen = Object.values(showProjectMenuByDay.value).some(v => v)
  if (anyMenuOpen && !target.closest('.project-menu')) {
    Object.keys(showProjectMenuByDay.value).forEach(key => {
      showProjectMenuByDay.value[key] = false
    })
    if (!Object.values(editingNewTaskByDay.value).some(v => v)) {
      editingDay.value = null
    }
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})


// Filtere Appointments für einen Tag
function getAppointmentsForDay(day: Date) {
  if (!appointments.value || appointments.value.length === 0) return []
  const dayKey = format(day, "yyyy-MM-dd")
  return appointments.value.filter(apt => {
    if (!apt.start_time) return false
    try {
      const aptDate = apt.start_time instanceof Date ? apt.start_time : new Date(apt.start_time)
      if (isNaN(aptDate.getTime())) return false
      const aptDateKey = format(aptDate, "yyyy-MM-dd")
      return aptDateKey === dayKey
    } catch {
      return false
    }
  })
}

// Update current time every minute
let timeInterval: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  timeInterval = setInterval(() => {
    currentTime.value = new Date()
  }, 60000)
})

onUnmounted(() => {
  if (timeInterval) {
    clearInterval(timeInterval)
  }
  // Cleanup event listeners
  if (mouseMoveHandler) {
    document.removeEventListener('mousemove', mouseMoveHandler)
  }
  if (mouseUpHandler) {
    document.removeEventListener('mouseup', mouseUpHandler)
  }
})
</script>

<template>
  <div class="flex flex-col h-full max-h-full min-h-0 border border-border rounded-lg overflow-hidden bg-background">
    <!-- Einzige Tabelle mit Header (sticky), scrollbarem Body und Footer (sticky) -->
    <div class="flex-1 overflow-y-auto min-h-0">
      <div
        class="relative flex flex-col h-full"
        :class="Object.values(dragOverTimeScheduleByDay).some(v => v) ? 'bg-primary/5' : ''"
      >
        <table class="w-full border-collapse" :style="{ height: '100%', display: 'table', tableLayout: 'fixed' }">
          <colgroup>
            <col v-for="day in props.days" :key="day.getTime()" style="width: calc(100% / 7)">
          </colgroup>

          <!-- Header: Tage (sticky oben) -->
          <thead class="sticky top-0 z-30 bg-background border-b-2 border-border">
            <tr>
              <th
                v-for="day in props.days"
                :key="day.getTime()"
                class="py-1.5 px-2 text-sm font-semibold text-foreground text-center bg-muted"
              >
                <div class="font-semibold text-xs">
                  {{ format(day, "EEE", { locale: de }) }}
                </div>
                <div class="text-[10px] mt-0.5 text-muted-foreground font-normal">
                  {{ format(day, "dd.MM.", { locale: de }) }}
                </div>
              </th>
            </tr>
            <!-- Toggle-Zeile für Timeline -->
            <tr>
              <th
                colspan="7"
                class="py-1 px-2 bg-muted border-t border-border"
              >
                <button
                  class="flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors w-full justify-center"
                  @click="isTimelineExpanded = !isTimelineExpanded"
                >
                  <UIcon
                    :name="isTimelineExpanded ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'"
                    class="w-3 h-3"
                  />
                  <span>{{ isTimelineExpanded ? 'Timeline ausblenden' : 'Timeline einblenden' }}</span>
                </button>
              </th>
            </tr>
          </thead>

          <!-- Timeline-Zeile - eine Zeile mit Timeline pro Tag -->
          <tbody v-if="isTimelineExpanded" class="relative" style="height: 50%">
            <tr class="relative" style="height: 100%">
              <!-- Tages-Spalten mit Timeline-Komponente -->
              <td
                v-for="day in props.days"
                :key="`timeline-${day.getTime()}`"
                class="relative align-top bg-background"
                style="height: 100%"
              >
                <div class="h-full">
                  <Timeline
                    :appointments="getAppointmentsForDay(day)"
                    :tasks-with-time="getTasksWithTimeForDay(day)"
                    :get-project-color="getProjectColor"
                    :get-project-border-color="getProjectBorderColor"
                    :drag-over-time-schedule="dragOverTimeScheduleByDay[format(day, 'yyyy-MM-dd')] || false"
                    :on-drag-over="(e) => handleTimelineDragOver(day, e)"
                    :on-drag-leave="() => handleTimelineDragLeave(day)"
                    :on-drop="(e) => handleTimelineDrop(day, e)"
                    :on-resize="handleResize"
                    :on-drag-timed-item="handleDragTimedItem"
                    :day="day"
                    hide-header
                    hide-time-axis
                    compact
                    padding="sm"
                  />
                </div>
              </td>
            </tr>
          </tbody>

          <!-- Aufgaben-Zeile (sticky unten) -->
          <tfoot class="sticky bottom-0 z-20 bg-muted border-t-2 border-border" :style="{ height: isTimelineExpanded ? '50%' : '100%' }">
            <tr :style="{ height: '100%' }">
              <td
                v-for="day in props.days"
                :key="`tasks-${day.getTime()}`"
                class="p-3 align-top bg-background"
                style="height: 100%"
                @drop="(e) => handleTaskDrop(e, day)"
                @dragover="(e) => handleTaskDragOver(e, day)"
              >
                <div class="flex flex-col h-full">
                  <!-- Aufgaben-Liste -->
                  <div class="space-y-1.5 flex-1 min-h-0 overflow-y-auto">
                    <TaskItem
                      v-for="task in getTasksForDay(day)"
                      :key="task.id"
                      :task="task"
                      size="sm"
                      :get-project-color="getProjectColorFn"
                      :get-project-border-color="getProjectBorderColorFn"
                      @drag-start="(task, event) => { if (event) handleTaskDragStart(task, event); }"
                      @drag-end="handleTaskDragEnd"
                    />

                    <!-- Add Task Button -->
                    <AddTaskButton
                      v-if="!(editingNewTaskByDay[format(day, 'yyyy-MM-dd')] && editingDay?.getTime() !== day.getTime()) && !showProjectMenuByDay[format(day, 'yyyy-MM-dd')]"
                      v-model="newTaskNameByDay[format(day, 'yyyy-MM-dd')]"
                      :is-editing="editingNewTaskByDay[format(day, 'yyyy-MM-dd')] && editingDay?.getTime() === day.getTime()"
                      size="sm"
                      @update:is-editing="editingNewTaskByDay[format(day, 'yyyy-MM-dd')] = $event; if ($event) editingDay = day; else editingDay = null"
                      @start-editing="showProjectSelection($event, day)"
                      @submit="createTask(day)"
                      @cancel="cancelNewTask(day)"
                    />
                  </div>

                  <!-- Complete Zone (Drop Zone für fertige Aufgaben) - immer am unteren Ende -->
                  <div class="mt-auto align-bottom shrink-0">
                    <TaskCompletionZone
                    :completed-tasks="getCompletedTasksForDay(day)"
                    :is-hovered="hoveredCompleteZone?.getTime() === day.getTime()"
                    :is-dragged-over="draggedOverCompleteZone"
                    :is-open="showCompletedPanel?.getTime() === day.getTime()"
                    :get-project-color="getProjectColor"
                    size="sm"
                    show-count
                    @drop="(e) => handleTaskDrop(e, day, true)"
                    @dragover="handleCompleteZoneDragOver"
                    @dragleave="handleCompleteZoneDragLeave"
                    @mouseenter="hoveredCompleteZone = day"
                    @mouseleave="hoveredCompleteZone = null; draggedOverCompleteZone = false"
                    @click="toggleCompletedPanel(day)"
                    @task-drag-start="(task, e) => { if (task.id) handleTaskDragStart(task, e); }"
                    />
                  </div>
                </div>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>

    <!-- Project Selection Menu -->
    <Teleport to="body">
      <Transition name="fade">
        <div
          v-if="Object.values(showProjectMenuByDay).some(v => v)"
          class="project-menu fixed z-50 bg-background border rounded-lg shadow-lg p-2 min-w-[200px]"
          :style="{
            left: `${projectMenuPosition.x}px`,
            top: `${projectMenuPosition.y}px`,
            transform: 'translate(-50%, 10px)',
          }"
          @click.stop
        >
          <div class="space-y-1">
            <div
              v-for="project in activeProjects"
              :key="project.id"
              class="flex items-center gap-2 px-3 py-2 rounded hover:bg-muted/50 cursor-pointer transition-colors"
              @click="selectProject(project.id, editingDay || undefined)"
            >
              <div
                class="w-3 h-3 rounded-full shrink-0"
                :class="getProjectColorFn(project.id) || 'bg-neutral'"
              />
              <span class="text-sm">{{ project.name }}</span>
            </div>
            <div
              class="flex items-center gap-2 px-3 py-2 rounded hover:bg-muted/50 cursor-pointer transition-colors border-t border-default mt-1 pt-1"
              @click="selectProject('', editingDay || undefined)"
            >
              <span class="text-sm text-muted-foreground">Kein Projekt</span>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

