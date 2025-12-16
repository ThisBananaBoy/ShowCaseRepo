<script setup lang="ts">
import { nextTick, onMounted, onUnmounted } from 'vue'
import { storeToRefs } from 'pinia'
import AddTaskButton from '~/components/tasks/AddTaskButton.vue'
import TaskItem from '~/components/tasks/TaskItem.vue'
import { useTaskStore } from '~/stores/useTaskStore'
import { useAppointmentStore } from '~/stores/useAppointmentStore'
import { useProjectStore } from '~/stores/useProjectStore'
import type { CreateTaskDto, Task } from '~/types/Task'
import type { Appointment } from '~/types/Appointment'
import { StatusTypes } from '~/types/Project'
import { isTaskCompleted, isTaskToday, getTaskTodayStatus } from '~/utils/taskHelpers'

const taskStore = useTaskStore()
const appointmentStore = useAppointmentStore()
const projectStore = useProjectStore()
const { tasks } = storeToRefs(taskStore)
const { projects } = storeToRefs(projectStore)
const { appointments } = storeToRefs(appointmentStore)
const { setSidebarContent } = usePageSidebar()

const newTaskName = ref('')
const draggedTask = ref<Task | null>(null)
const dragOverColumn = ref<string | null>(null)
const editingNewTask = ref(false)
const showProjectMenu = ref(false)
const projectMenuPosition = ref({ x: 0, y: 0 })
const selectedProjectForTask = ref<string | null>(null)

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

const taskList = computed(() => tasks.value)

const activeProjects = computed(() =>
  projects.value.filter(project => project.status === StatusTypes.ACTIVE)
)

function isSameDay(value?: Date | string) {
  if (!value) return false
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return false
  const now = new Date()
  return date.getFullYear() === now.getFullYear()
    && date.getMonth() === now.getMonth()
    && date.getDate() === now.getDate()
}

function getProjectColor(projectId: string): string {
  const projectIndex = activeProjects.value.findIndex((p) => p.id === projectId)
  if (projectIndex === -1) return 'bg-neutral'
  return projectColors[projectIndex % projectColors.length]!
}

function getProjectBorderColor(projectId: string): string {
  const projectIndex = activeProjects.value.findIndex((p) => p.id === projectId)
  if (projectIndex === -1) return 'border-muted-foreground/20'
  return projectBorderColors[projectIndex % projectBorderColors.length]!
}

// Computed
const todayTasks = computed(() =>
  taskList.value.filter(task => isTaskToday(task))
)

const todayOpenTasks = computed(() =>
  todayTasks.value.filter(t => !isTaskCompleted(t))
)

const todayCompletedTasks = computed(() =>
  todayTasks.value.filter(t => isTaskCompleted(t))
)

const todayAppointments = computed(() => appointmentStore.getTodayAppointments())

// Tasks mit Zeit
const tasksWithTime = computed(() =>
  todayTasks.value.filter(t => t.start_time && t.end_time)
)

const availableTasks = computed(() =>
  taskList.value.filter(t => !isTaskCompleted(t))
)

// Tasks nach Projekten gruppieren
const tasksByProject = computed(() => {
  const grouped: Record<string, Task[]> = {}
  const noProjectTasks: Task[] = []

  availableTasks.value.forEach((task) => {
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

async function onDropToTimeSchedule(event: DragEvent) {
  event.preventDefault()
  dragOverTimeSchedule.value = false

  if (!draggedTask.value) return

  const container = event.currentTarget as HTMLElement
  const time = roundToQuarterHour(getTimeFromPosition(event.clientY, container))

  // Standard-Dauer: 1 Stunde
  const endTime = new Date(time)
  endTime.setHours(endTime.getHours() + 1)

  const task = taskList.value.find(t => t.id === draggedTask.value!.id)
  if (!task) return

  await taskStore.updateTask(task.id, {
    start_time: time,
    end_time: endTime,
    status: 'todo'
  })

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
      const currentTask = taskList.value.find(t => t.id === draggingTimedItem.value!.task!.id)
      if (!currentTask || !currentTask.start_time || !currentTask.end_time) return

      const duration = new Date(currentTask.end_time).getTime() - new Date(currentTask.start_time).getTime()
      const newEndTime = new Date(rounded.getTime() + duration)

      // Boundary checks
      const startRangeMinutes = 6 * 60
      const endRangeMinutes = 22 * 60
      const roundedMinutes = rounded.getHours() * 60 + rounded.getMinutes()
      const newEndMinutes = newEndTime.getHours() * 60 + newEndTime.getMinutes()

      if (roundedMinutes >= startRangeMinutes && newEndMinutes <= endRangeMinutes) {
        void taskStore.updateTask(currentTask.id, {
          start_time: rounded,
          end_time: newEndTime
        })
      }
    } else if (draggingTimedItem.value.appointment) {
      const appt = todayAppointments.value.find(a => a.id === draggingTimedItem.value!.appointment!.id)
      if (!appt) return

      const duration = new Date(appt.end_time).getTime() - new Date(appt.start_time).getTime()
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

    const task = taskList.value.find(t => t.id === resizingTask.value!.task.id)
    if (!task || !task.start_time || !task.end_time) return

    const taskStart = new Date(task.start_time)
    const taskEnd = new Date(task.end_time)
    if (Number.isNaN(taskStart.getTime()) || Number.isNaN(taskEnd.getTime())) return

    if (edge === 'top') {
      const newStartTime = new Date(resizeStartTime.value!)
      newStartTime.setMinutes(newStartTime.getMinutes() + deltaMinutes)
      const rounded = roundToQuarterHour(newStartTime)

      // Verhindere dass Start nach Ende kommt
      if (rounded.getTime() < taskEnd.getTime()) {
        void taskStore.updateTask(task.id, { start_time: rounded })
      }
    } else {
      const newEndTime = new Date(resizeStartTime.value!)
      newEndTime.setMinutes(newEndTime.getMinutes() + deltaMinutes)
      const rounded = roundToQuarterHour(newEndTime)

      // Verhindere dass Ende vor Start kommt
      if (rounded.getTime() > taskStart.getTime()) {
        void taskStore.updateTask(task.id, { end_time: rounded })
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


// Update current time every minute


// Functions
async function addTask() {
  if (!newTaskName.value.trim()) return

  const newTask: CreateTaskDto = {
    name: newTaskName.value,
    status: 'todo',
    priority: 2,
    project_id: selectedProjectForTask.value || undefined
  }

  await taskStore.createTask(newTask)
  newTaskName.value = ''
  selectedProjectForTask.value = null
  editingNewTask.value = false
}

function showProjectSelection(event?: MouseEvent) {
  if (event) {
    event.stopPropagation()
    projectMenuPosition.value = { x: event.clientX, y: event.clientY }
  }
  showProjectMenu.value = true
}

function selectProject(projectId: string) {
  selectedProjectForTask.value = projectId
  showProjectMenu.value = false
  editingNewTask.value = true
  newTaskName.value = ''
  nextTick(() => {
    const input = document.querySelector(
      '.new-task-input'
    ) as HTMLInputElement
    input?.focus()
  })
}

function cancelNewTask() {
  editingNewTask.value = false
  newTaskName.value = ''
  selectedProjectForTask.value = null
  showProjectMenu.value = false
}

async function createTask(value?: string) {
  if (value) {
    newTaskName.value = value
  }
  if (!newTaskName.value.trim()) {
    cancelNewTask()
    return
  }
  await addTask()
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement
  if (showProjectMenu.value && !target.closest('.project-menu')) {
    showProjectMenu.value = false
    if (!editingNewTask.value) {
      cancelNewTask()
    }
  }
}

// Set sidebar content immediately (before mount to avoid race condition)
setSidebarContent('TasksSidebarContent', {
  availableTasks: availableTasks.value,
  tasksByProject: tasksByProject.value,
  activeProjects: activeProjects.value,
  getProjectColor,
  getProjectBorderColor,
  onDragStart,
  onDragEnd,
  title: 'Verfügbare Aufgaben'
})

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
  if (!projects.value.length) projectStore.fetchProjects()
  if (!taskList.value.length) taskStore.fetchTasks()
  if (!appointments.value.length) appointmentStore.fetchAppointments()
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
  // Cleanup event listeners on unmount
  if (mouseMoveHandler) {
    document.removeEventListener('mousemove', mouseMoveHandler)
  }
  if (mouseUpHandler) {
    document.removeEventListener('mouseup', mouseUpHandler)
  }
})

// Update sidebar when data changes
watch([availableTasks, tasksByProject, activeProjects], () => {
  setSidebarContent('TasksSidebarContent', {
    availableTasks: availableTasks.value,
    tasksByProject: tasksByProject.value,
    activeProjects: activeProjects.value,
    getProjectColor,
    getProjectBorderColor,
    onDragStart,
    onDragEnd,
    title: 'Verfügbare Aufgaben'
  })
}, { deep: true })

function onDragStart(task: Task) {
  draggedTask.value = task
}

function onDragEnd() {
  draggedTask.value = null
  dragOverColumn.value = null
  dragOverTimeSchedule.value = false
}

function onDragOver(event: DragEvent, column: string) {
  event.preventDefault()
  dragOverColumn.value = column
}

function onDragLeave() {
  dragOverColumn.value = null
}


async function onDropToToday(event: DragEvent) {
  event.preventDefault()
  dragOverColumn.value = null

  if (!draggedTask.value) return

  const task = taskList.value.find(t => t.id === draggedTask.value!.id)
  if (!task) return

  await taskStore.updateTask(task.id, {
    status: 'todo',
    completed_at: undefined
  })

  draggedTask.value = null
}

async function onDropToSubmit(event: DragEvent) {
  event.preventDefault()
  dragOverColumn.value = null

  if (!draggedTask.value) return

  const task = taskList.value.find(t => t.id === draggedTask.value!.id)
  if (!task) return

  await taskStore.updateTask(task.id, {
    status: 'completed',
    completed_at: new Date()
  })

  draggedTask.value = null
}

function removeFromToday(taskId: string) {
  const task = taskList.value.find(t => t.id === taskId)
  if (!task) return

  if (task.project_id) {
    // Task gehört zu einem Projekt - nur aus Heute entfernen (start_time/due_date entfernen)
    void taskStore.updateTask(task.id, {
      start_time: undefined,
      end_time: undefined
    })
  } else {
    // Task ist standalone - komplett löschen
    void taskStore.deleteTask(task.id)
  }
}

function handleCreateAppointment(appointmentData: Omit<Appointment, 'id'>) {
  appointmentStore.createAppointment(appointmentData)
}

</script>

<template>
  <UDashboardPanel id="today" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar title="Heute">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>

        <template #right>
          <div class="text-sm text-muted-foreground">
            {{ todayOpenTasks.length }} Aufgaben
          </div>
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex-1 flex w-full overflow-hidden min-h-0">
        <!-- Aufgaben Spalte -->
        <div class="flex-1 border-r border-border flex flex-col min-h-0 overflow-hidden">
          <div class="flex-1 grid grid-rows-2 min-h-0 overflow-hidden">
            <!-- Offene Aufgaben -->
            <div class="p-6 min-h-0 border-border overflow-hidden flex flex-col">
              <h3 class="font-semibold mb-4 flex items-center gap-2">
                <UIcon name="i-lucide-circle" class="size-4" />
                Offene Aufgaben
                <UBadge :label="String(todayOpenTasks.length)" color="neutral" size="xs" />
              </h3>

              <div
                class="flex-1 p-4 border-2 border-dashed rounded-lg overflow-y-auto transition-colors min-h-0"
                :class="dragOverColumn === 'open' ? 'border-primary bg-primary/5' : 'border-border'"
                @dragover="onDragOver($event, 'open')"
                @dragleave="onDragLeave"
                @drop="onDropToToday($event)"
              >
                <div class="space-y-1">
                  <!-- Existing Tasks -->
                  <TaskItem
                    v-for="task in todayOpenTasks"
                    :key="task.id"
                    :task="task"
                    :get-project-color="getProjectColor"
                    :get-project-border-color="getProjectBorderColor"
                    :show-remove-button="true"
                    @drag-start="onDragStart"
                    @drag-end="onDragEnd"
                    @remove="removeFromToday"
                  />

                  <!-- Add Task Button -->
                  <AddTaskButton
                    v-model="newTaskName"
                    :is-editing="editingNewTask"
                    @update:is-editing="editingNewTask = $event"
                    @start-editing="showProjectSelection"
                    @submit="createTask"
                    @cancel="cancelNewTask"
                  />
                </div>

                <div
                  v-if="todayOpenTasks.length === 0 && !editingNewTask"
                  class="text-center py-16 text-muted-foreground"
                >
                  <UIcon name="i-lucide-inbox" class="size-16 mx-auto mb-4 opacity-50" />
                  <p class="text-sm">
                    Ziehe Aufgaben hierher oder erstelle neue
                  </p>
                </div>
              </div>
            </div>

            <!-- Submit Spalte -->
            <div class="p-6 min-h-0 overflow-hidden flex flex-col">
              <h3 class="font-semibold mb-4 flex items-center gap-2">
                <UIcon name="i-lucide-check-circle-2" class="size-4" />
                Submit
                <UBadge :label="String(todayCompletedTasks.length)" color="neutral" size="xs" />
              </h3>

              <div
                class="flex-1 p-4 border-2 overflow-y-auto border-dashed rounded-lg transition-colors min-h-0"
                :class="dragOverColumn === 'submit' ? 'border-primary bg-primary/5' : 'border-border'"
                @dragover="onDragOver($event, 'submit')"
                @dragleave="onDragLeave"
                @drop="onDropToSubmit($event)"
              >
                <div class="space-y-1">
                  <!-- Completed Tasks -->
                  <TaskItem
                    v-for="task in todayCompletedTasks"
                    :key="task.id"
                    :task="task"
                    :get-project-color="getProjectColor"
                    :get-project-border-color="getProjectBorderColor"
                    :show-remove-button="true"
                    @drag-start="onDragStart"
                    @drag-end="onDragEnd"
                    @remove="removeFromToday"
                  />
                </div>

                <div
                  v-if="todayCompletedTasks.length === 0"
                  class="text-center py-16 text-muted-foreground"
                >
                  <UIcon name="i-lucide-check-circle-2" class="size-16 mx-auto mb-4 opacity-50" />
                  <p class="text-sm">
                    Ziehe Aufgaben hierher, um sie als fertig zu markieren
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Zeitraster -->
        <div class="flex-1 border-l border-border flex flex-col min-h-0">
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
            :on-create-appointment="handleCreateAppointment"
            :day="new Date()"
          />
        </div>
      </div>

      <!-- Project Selection Menu -->
      <Teleport to="body">
        <Transition name="fade">
          <div
            v-if="showProjectMenu"
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
                @click="selectProject(project.id)"
              >
                <div
                  class="w-3 h-3 rounded-full shrink-0"
                  :class="getProjectColor(project.id)"
                />
                <span class="text-sm">{{ project.name }}</span>
              </div>
              <div
                class="flex items-center gap-2 px-3 py-2 rounded hover:bg-muted/50 cursor-pointer transition-colors border-t border-default mt-1 pt-1"
                @click="selectProject('')"
              >
                <span class="text-sm text-muted-foreground">Kein Projekt</span>
              </div>
            </div>
          </div>
        </Transition>
      </Teleport>
    </template>
  </UDashboardPanel>
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
</style>
