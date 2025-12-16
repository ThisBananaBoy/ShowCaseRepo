<script setup lang="ts">
import { format } from 'date-fns'
import type { RecurringTask, CreateRecurringTaskDto } from '~/types/RecurringTask'
import { useRecurringTaskStore } from '~/stores/useRecurringTaskStore'
import { storeToRefs } from 'pinia'

const recurringTaskStore = useRecurringTaskStore()
const { recurringTasks } = storeToRefs(recurringTaskStore)
const { setSidebarContent } = usePageSidebar() // Setzt Content für die linke PageSidebar

const draggedTask = ref<RecurringTask | null>(null)
const draggedTaskDay = ref<number | null>(null) // Speichert den Tag (1-28), wenn eine Aufgabe vom Kalender gezogen wird
const dragOverDay = ref<number | null>(null)
const dragOverContextSidebar = ref(false) // Drag-Over für die rechte ContextSidebar

// 28 Tage (1-28)
const calendarDays = computed(() => {
  return Array.from({ length: 28 }, (_, i) => i + 1)
})

// Erstelle ein Datum für einen Tag (1-28)
function getDateForDay(day: number): Date {
  const date = new Date()
  date.setDate(day)
  date.setHours(0, 0, 0, 0)
  return date
}

const getDateKey = (value: Date | string) =>
  format(new Date(value), 'yyyy-MM-dd')

// Aufgaben für einen bestimmten Tag (1-28)
function getTasksForDay(day: number): RecurringTask[] {
  const date = getDateForDay(day)
  return recurringTaskStore.getRecurringTasksByDate(date)
}

const findTaskById = (id: string) =>
  recurringTasks.value.find(task => task.id === id)

function onDragStart(task: RecurringTask, day?: number) {
  draggedTask.value = task
  draggedTaskDay.value = day || null
}

function onDragEnd() {
  draggedTask.value = null
  draggedTaskDay.value = null
  dragOverDay.value = null
  dragOverContextSidebar.value = false
}

function onDragOver(event: DragEvent, day?: number) {
  event.preventDefault()
  if (day !== undefined) {
    dragOverDay.value = day
  } else {
    dragOverContextSidebar.value = true
  }
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'move'
  }
}

function onDragLeave() {
  dragOverDay.value = null
  dragOverContextSidebar.value = false
}

async function onDropToCalendar(event: DragEvent, day: number) {
  event.preventDefault()
  if (!draggedTask.value) return

  const date = getDateForDay(day)
  const task = findTaskById(draggedTask.value.id)

  if (task) {
    const updatedDates = [...task.assigned_dates]

    // Wenn die Aufgabe vom Kalender kommt, entferne sie vom alten Tag
    if (draggedTaskDay.value !== null) {
      const oldDate = getDateForDay(draggedTaskDay.value)
      const oldDateKey = getDateKey(oldDate)
      const filtered = updatedDates.filter(
        d => getDateKey(d) !== oldDateKey
      )
      updatedDates.splice(0, updatedDates.length, ...filtered)
    }

    // Prüfe ob Datum bereits existiert
    const dateKey = getDateKey(date)
    const dateExists = updatedDates.some(
      d => getDateKey(d) === dateKey
    )

    if (!dateExists) {
      updatedDates.push(date)
    }

    await recurringTaskStore.updateRecurringTask(task.id, {
      assigned_dates: updatedDates
    })
  }

  draggedTask.value = null
  draggedTaskDay.value = null
  dragOverDay.value = null
}

async function onDropToContextSidebar(event: DragEvent) {
  event.preventDefault()
  if (!draggedTask.value || draggedTaskDay.value === null) return

  const task = findTaskById(draggedTask.value.id)
  if (task) {
    const dateToRemove = getDateForDay(draggedTaskDay.value)
    const dateKey = getDateKey(dateToRemove)
    const updatedDates = task.assigned_dates.filter(
      d => getDateKey(d) !== dateKey
    )
    await recurringTaskStore.updateRecurringTask(task.id, {
      assigned_dates: updatedDates
    })
  }

  draggedTask.value = null
  draggedTaskDay.value = null
  dragOverContextSidebar.value = false
}

async function createTask(name: string) {
  const payload: CreateRecurringTaskDto = {
    name,
    assigned_dates: [],
    created_at: new Date(),
  }
  await recurringTaskStore.createRecurringTask(payload)
}

// PageSidebar (linke Seite) Content setzen
setSidebarContent('RecurringTasksSidebarContent', {
  recurringTasks: recurringTasks,
  onDragStart,
  onDragEnd,
  onCreateTask: createTask,
  onDropToContextSidebar: onDropToContextSidebar,
  onDragOver,
  onDragLeave,
})

// PageSidebar (linke Seite) aktualisieren wenn sich Daten ändern
watch(recurringTasks, () => {
  setSidebarContent('RecurringTasksSidebarContent', {
    recurringTasks: recurringTasks,
    onDragStart,
    onDragEnd,
    onCreateTask: createTask,
    onDropToContextSidebar: onDropToContextSidebar,
    onDragOver,
    onDragLeave,
  })
}, { deep: true })
</script>

<template>
  <div class="h-full flex w-full overflow-hidden">
    <!-- Kalender -->
    <div class="flex-1 overflow-y-auto min-h-0">
      <div class="p-6">
        <!-- Kalender -->
        <div class="border rounded-lg overflow-hidden">
          <!-- Wochentage Header -->
          <div class="grid grid-cols-7 bg-muted/50">
            <div
              v-for="day in ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So']"
              :key="day"
              class="p-3 text-center text-sm font-semibold border-r last:border-r-0"
            >
              {{ day }}
            </div>
          </div>

          <!-- Kalendertage - 4 Zeilen x 7 Spalten = 28 Tage -->
          <div class="grid grid-cols-7">
            <div
              v-for="day in calendarDays"
              :key="day"
              class="min-h-[120px] border-r border-b last:border-r-0 p-2"
              :class="{
                'bg-primary/5': dragOverDay === day,
                'bg-background': dragOverDay !== day,
              }"
              @drop="onDropToCalendar($event, day)"
              @dragover="onDragOver($event, day)"
              @dragleave="onDragLeave"
            >
              <div class="text-sm font-medium mb-1">
                {{ day }}
              </div>
              <div class="space-y-1">
                <!-- Existing Tasks -->
                <div
                  v-for="task in getTasksForDay(day)"
                  :key="`${task.id}-${day}`"
                  class="cursor-move"
                  :draggable="true"
                  @dragstart="onDragStart(task, day)"
                  @dragend="onDragEnd"
                >
                  <div
                    class="px-2 py-1 rounded border hover:bg-muted/50 transition-colors flex items-center gap-2 border-muted-foreground/20"
                  >
                    <p class="text-xs flex-1 truncate">
                      {{ task.name }}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
