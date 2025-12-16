<script setup lang="ts">
import { nextTick, onMounted, onUnmounted } from 'vue'
import {
  getWeek,
  startOfWeek,
  endOfWeek,
  format,
  eachDayOfInterval
} from 'date-fns'
import { de } from 'date-fns/locale'
import CalendarTimeTable from '~/components/CalendarTimeTable.vue'
import type { Task, CreateTaskDto } from '~/types/Task'
import type { Appointment } from '~/types/Appointment'
import { useTaskStore } from '~/stores/useTaskStore'
import { isTaskCompleted, getTaskWeek } from '~/utils/taskHelpers'
import { useProjectStore } from '~/stores/useProjectStore'
import { useAppointmentStore } from '~/stores/useAppointmentStore'
import { storeToRefs } from 'pinia'

const route = useRoute()
const router = useRouter()
const { setSidebarContent } = usePageSidebar()

const taskStore = useTaskStore()
const projectStore = useProjectStore()
const appointmentStore = useAppointmentStore()
const { tasks } = storeToRefs(taskStore)
const { projects } = storeToRefs(projectStore)
const { appointments } = storeToRefs(appointmentStore)

// Parse id parameter (YYYY-MM-DD format)
const weekId = route.params.id as string
const weekStartDate = computed(() => {
  try {
    const date = new Date(weekId)
    return startOfWeek(date, { weekStartsOn: 1 })
  } catch {
    // Fallback to current week if invalid
    return startOfWeek(new Date(), { weekStartsOn: 1 })
  }
})

// Debug: Log tasks and appointments for KW 46
watch(() => weekStartDate.value, (weekStart) => {
  const weekNum = getWeekNumber(weekStart)
  if (weekNum === 46) {
    console.log('KW 46 detected. Week start:', weekStart)
    console.log('Total tasks:', tasks.value.length)
    console.log('Total appointments:', appointments.value.length)
    console.log('Tasks with start_time:', tasks.value.filter(t => t.start_time).length)
    console.log('Appointments for week:', appointments.value.filter(a => {
      if (!a.start_time) return false
      const aptDate = new Date(a.start_time)
      const aptWeek = getWeekNumber(aptDate)
      return aptWeek === 46
    }).length)
  }
}, { immediate: true })

// Aktive Projekte
const activeProjects = computed(() =>
  projects.value.filter(p => p.status === 'active')
)

// Projekt-Farben (für visuelle Unterscheidung)
const projectColors = [
  "bg-blue-500",
  "bg-purple-500",
  "bg-pink-500",
  "bg-orange-500",
  "bg-teal-500",
  "bg-indigo-500",
  "bg-rose-500",
  "bg-amber-500",
];

const projectBorderColors = [
  "border-blue-500/30",
  "border-purple-500/30",
  "border-pink-500/30",
  "border-orange-500/30",
  "border-teal-500/30",
  "border-indigo-500/30",
  "border-rose-500/30",
  "border-amber-500/30",
];

function getProjectColor(projectId: string): string {
  const projectIndex = activeProjects.value.findIndex((p) => p.id === projectId);
  if (projectIndex === -1) return "bg-neutral";
  return projectColors[projectIndex % projectColors.length]!;
}

function getProjectBorderColor(projectId: string): string {
  const projectIndex = activeProjects.value.findIndex((p) => p.id === projectId);
  if (projectIndex === -1) return "border-muted-foreground/20";
  return projectBorderColors[projectIndex % projectBorderColors.length]!;
}

// Alle Aufgaben aus aktiven Projekten
const allActiveTasks = computed(() => {
  const activeProjectIds = activeProjects.value.map(p => p.id)
  return tasks.value.filter(
    task => task.project_id && activeProjectIds.includes(task.project_id)
  )
})

const days = computed(() => {
  const start = weekStartDate.value;
  const end = endOfWeek(start, { weekStartsOn: 1 });
  return eachDayOfInterval({ start, end });
});

const tasksByDay = computed(() => {
  const grouped: Record<string, Task[]> = {};
  allActiveTasks.value.forEach((task) => {
    if (task.due_date) {
      const taskDate =
        task.due_date instanceof Date
          ? task.due_date
          : new Date(task.due_date);
      const dayKey = format(taskDate, "yyyy-MM-dd");
      if (!grouped[dayKey]) grouped[dayKey] = [];
      grouped[dayKey].push(task);
    }
  });
  return grouped;
});

function getWeekNumber(date: Date) {
  return getWeek(date, { locale: de, weekStartsOn: 1 });
}

function getTasksForDay(day: Date) {
  const dayKey = format(day, "yyyy-MM-dd");
  const dayTasks = tasksByDay.value[dayKey] || [];
  // Nur nicht-fertige Aufgaben anzeigen
  return dayTasks.filter((task) => !isTaskCompleted(task));
}

function getCompletedTasksForDay(day: Date) {
  const dayKey = format(day, "yyyy-MM-dd");
  const dayTasks = tasksByDay.value[dayKey] || [];
  return dayTasks.filter((task) => isTaskCompleted(task));
}

async function createTask(name: string, day: Date, projectId?: string) {
  const payload: CreateTaskDto = {
    name: name.trim(),
    status: 'todo',
    priority: 2,
    project_id: projectId || undefined,
    due_date: day
  }

  await taskStore.createTask(payload)
}


async function moveTaskToDay(taskId: string, day: Date) {
  await taskStore.updateTask(taskId, {
    due_date: day
  })
}

const draggedTask = ref<string | null>(null);

async function handleDrop(event: DragEvent, targetDay?: Date, isCompleteZone = false) {
  event.preventDefault()
  if (!draggedTask.value) return

  const task = tasks.value.find(t => t.id === draggedTask.value)

  if (isCompleteZone) {
    if (task?.id) {
      await taskStore.updateTask(task.id, {
        status: 'completed',
        completed_at: new Date()
      })
    }
  } else {
    if (targetDay) {
      await moveTaskToDay(draggedTask.value, targetDay)
    }
    if (task?.id && isTaskCompleted(task)) {
      await taskStore.updateTask(task.id, {
        status: 'todo',
        completed_at: undefined
      })
    }
  }

  draggedTask.value = null
}


function onSidebarDragStart(task: Task) {
  if (!task.id) return;
  draggedTask.value = task.id;
}

function onSidebarDragEnd() {
  draggedTask.value = null;
}

async function handleDropToSidebar() {
  if (!draggedTask.value) return

  const task = tasks.value.find(t => t.id === draggedTask.value)
  if (task?.id) {
    await taskStore.updateTask(task.id, {
      due_date: undefined,
      status: 'todo',
      completed_at: undefined
    })
  }

  draggedTask.value = null
}

function handleDragOver(event: DragEvent) {
  event.preventDefault();
}


async function handleTimelineDrop(day: Date, event: DragEvent, hour?: number) {
  event.preventDefault()
  draggedOverTimeline.value = null
  if (draggedTask.value) {
    await moveTaskToDay(draggedTask.value, day)
    if (hour !== undefined) {
      const task = tasks.value.find(t => t.id === draggedTask.value)
      if (task?.id) {
        const startTime = new Date(day)
        startTime.setHours(hour, 0, 0, 0)
        const endTime = new Date(day)
        endTime.setHours(hour + 1, 0, 0, 0)
        await taskStore.updateTask(task.id, {
          start_time: startTime,
          end_time: endTime
        })
      }
    }
  }
  draggedTask.value = null
}

// Drag & Drop State für Timeline
const resizingTask = ref<{ task: Task, edge: 'top' | 'bottom' } | null>(null)
const draggingTimedItem = ref<{ task?: Task, appointment?: Appointment, startY: number, startTime: Date, day: Date } | null>(null)
const resizeStartY = ref(0)
const resizeStartTime = ref<Date | null>(null)

let mouseMoveHandler: ((e: MouseEvent) => void) | null = null
let mouseUpHandler: (() => void) | null = null

// Runde Zeit auf 15-Minuten-Intervalle
function roundToQuarterHour(date: Date): Date {
  const minutes = date.getMinutes()
  const roundedMinutes = Math.round(minutes / 15) * 15
  const rounded = new Date(date)
  rounded.setMinutes(roundedMinutes, 0, 0)
  return rounded
}

function handleTimelineResize(task: Task, edge: 'top' | 'bottom', event: MouseEvent) {
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
      if (rounded.getTime() < taskToResize.end_time.getTime() && taskToResize.id) {
        void taskStore.updateTask(taskToResize.id, { start_time: rounded })
      }
    } else {
      const newEndTime = new Date(resizeStartTime.value!)
      newEndTime.setMinutes(newEndTime.getMinutes() + deltaMinutes)
      const rounded = roundToQuarterHour(newEndTime)

      // Verhindere dass Ende vor Start kommt
      if (rounded.getTime() > taskToResize.start_time.getTime() && taskToResize.id) {
        void taskStore.updateTask(taskToResize.id, { end_time: rounded })
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

function handleTimelineDragTimedItem(event: MouseEvent, task?: Task, appointment?: Appointment) {
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

      if (roundedMinutes >= startRangeMinutes && newEndMinutes <= endRangeMinutes && taskToDrag.id) {
        void taskStore.updateTask(taskToDrag.id, {
          start_time: rounded,
          end_time: newEndTime
        })
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

      if (roundedMinutes >= startRangeMinutes && newEndMinutes <= endRangeMinutes && appt.id) {
        appointmentStore.patchLocalAppointment(appt.id, {
          start_time: rounded,
          end_time: newEndTime
        })
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

onUnmounted(() => {
  // Cleanup event listeners on unmount
  if (mouseMoveHandler) {
    document.removeEventListener('mousemove', mouseMoveHandler)
  }
  if (mouseUpHandler) {
    document.removeEventListener('mouseup', mouseUpHandler)
  }
})

const hoveredDay = ref<Date | null>(null);
const editingNewTask = ref(false);
const editingDay = ref<Date | null>(null);
const newTaskName = ref("");
const showAddTask = ref(false);
const hoverTimeout = ref<number | null>(null);
const showProjectMenu = ref(false);
const projectMenuPosition = ref({ x: 0, y: 0 });
const selectedProjectForTask = ref<string | null>(null);
const draggedOverTimeline = ref<Date | null>(null);

function onTimelineDragOver(event: DragEvent) {
  event.preventDefault()
  draggedOverTimeline.value = days.value[0] || null
}

function onTimelineDragLeave() {
  draggedOverTimeline.value = null
}

function onTimelineDrop(event: DragEvent, day: Date, hour?: number) {
  void handleTimelineDrop(day, event, hour)
}

function onTaskDrop(event: DragEvent, day: Date, isCompleteZone?: boolean) {
  void handleDrop(event, day, !!isCompleteZone)
}

function onTaskDragOver(event: DragEvent, _day: Date) {
  event.preventDefault()
  handleDragOver(event)
}

function onTaskDragStart(task: Task) {
  draggedTask.value = task.id
}

function selectProject(projectId: string, day?: Date) {
  selectedProjectForTask.value = projectId;
  showProjectMenu.value = false;
  editingNewTask.value = true;
  editingDay.value = day || null;
  newTaskName.value = "";
  nextTick(() => {
    const input = document.querySelector(
      ".new-task-input"
    ) as HTMLInputElement;
    input?.focus();
  });
}



// Tasks für Sidebar - verfügbare Tasks (ohne due_date oder nicht zugewiesen)
const availableTasksForSidebar = computed(() => {
  return tasks.value.filter((task) => {
    // Nur Tasks aus aktiven Projekten oder ohne Projekt
    if (task.project_id) {
      const project = projects.value.find((p) => p.id === task.project_id)
      if (!project || project.status !== 'active') return false
    }
    // Nur nicht-fertige Tasks
    if (isTaskCompleted(task)) return false
    // Tasks ohne due_date
    return !task.due_date
  })
})

// Tasks nach Projekten gruppieren für Sidebar
const tasksByProjectForSidebar = computed(() => {
  const grouped: Record<string, Task[]> = {}
  const noProjectTasks: Task[] = []

  availableTasksForSidebar.value.forEach((task) => {
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

// Set sidebar content
watch([availableTasksForSidebar, tasksByProjectForSidebar, activeProjects], () => {
  setSidebarContent('TasksSidebarContent', {
    availableTasks: availableTasksForSidebar.value,
    tasksByProject: tasksByProjectForSidebar.value,
    activeProjects: activeProjects.value,
    getProjectColor,
    getProjectBorderColor,
    onDragStart: onSidebarDragStart,
    onDragEnd: onSidebarDragEnd,
    onDropToSidebar: handleDropToSidebar,
    title: 'Verfügbare Aufgaben'
  });
}, { deep: true, immediate: true });

// Click outside handler für Projekt-Menü
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement;
  if (showProjectMenu.value && !target.closest('.project-menu')) {
    showProjectMenu.value = false;
    if (!editingNewTask.value) {
      hoveredDay.value = null;
      showAddTask.value = false;
    }
  }
}

onMounted(() => {
  taskStore.fetchTasks()
  projectStore.fetchProjects()
  appointmentStore.fetchAppointments()
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
  }
});
</script>

<template>
  <UDashboardPanel :id="`tasks-week-${weekId}`" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar :title="`KW ${getWeekNumber(weekStartDate)} - Tagesansicht`">
        <template #leading>
          <UButton
            icon="i-lucide-arrow-left"
            color="neutral"
            variant="ghost"
            square
            @click="router.push('/tasks')"
          />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex-1 overflow-hidden w-full min-h-0">
        <div class="p-6 h-full flex flex-col min-h-0">
          <Transition name="fade-slide" mode="out-in">
            <div
              :key="weekStartDate.getTime()"
              class="flex-1 min-h-0 h-full"
            >
              <CalendarTimeTable
                :days="days"
                :appointments="appointments"
                :tasks-with-time="tasks.filter(t => t.start_time && t.end_time)"
                :tasks-by-day="getTasksForDay"
                :get-completed-tasks-for-day="getCompletedTasksForDay"
                :get-project-color="getProjectColor"
                :get-project-border-color="getProjectBorderColor"
                :drag-over-time-schedule="draggedOverTimeline !== null"
                :on-drag-over="onTimelineDragOver"
                :on-drag-leave="onTimelineDragLeave"
                :on-drop="onTimelineDrop"
                :on-resize="handleTimelineResize"
                :on-drag-timed-item="handleTimelineDragTimedItem"
                :on-task-drop="onTaskDrop"
                :on-task-drag-over="onTaskDragOver"
                :on-task-drag-start="onTaskDragStart"
                :on-create-task="createTask"
                :projects="projects"
              />
            </div>
          </Transition>
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
                @click="selectProject(project.id, editingDay || undefined)"
              >
                <div
                  class="w-3 h-3 rounded-full shrink-0"
                  :class="getProjectColor(project.id)"
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
    </template>
  </UDashboardPanel>
</template>

<style scoped>
.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.3s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

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

