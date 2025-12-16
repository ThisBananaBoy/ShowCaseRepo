<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { format, isSameDay } from 'date-fns'
import { de } from 'date-fns/locale'
import type { Task } from '~/types/Task'
import type { Appointment } from '~/types/Appointment'
import AddAppointment from '~/components/appointments/AddAppointment.vue'
import { isTaskCompleted } from '~/utils/taskHelpers'

const props = defineProps<{
  appointments?: Appointment[]
  tasksWithTime?: Task[]
  getProjectColor?: (projectId: string) => string
  getProjectBorderColor?: (projectId: string) => string
  dragOverTimeSchedule?: boolean
  onDragOver?: (event: DragEvent) => void
  onDragLeave?: () => void
  onDrop?: (event: DragEvent) => void
  onResize?: (task: Task, edge: 'top' | 'bottom', event: MouseEvent) => void
  onDragTimedItem?: (event: MouseEvent, task?: Task, appointment?: Appointment) => void
  onTimelineClick?: (time: Date, event: MouseEvent) => void
  onCreateAppointment?: (appointment: Omit<Appointment, 'id'>) => void
  hideHeader?: boolean
  hideTimeAxis?: boolean
  compact?: boolean
  padding?: 'none' | 'sm' | 'md' | 'lg'
  day?: Date
}>()

const paddingClasses = computed(() => {
  const paddingValue = props.padding || 'lg'
  const paddingMap = {
    none: { container: 'p-0', left: 'left-0', right: 'right-0' },
    sm: { container: 'p-1', left: 'left-1', right: 'right-1' },
    md: { container: 'p-2', left: 'left-2', right: 'right-2' },
    lg: { container: 'p-4', left: 'left-4', right: 'right-4' }
  }
  return paddingMap[paddingValue]
})

// Zeitraster: Stunden von 6 bis 22 Uhr
const timeSlots = computed(() => {
  const slots = []
  for (let hour = 6; hour <= 22; hour++) {
    slots.push(hour)
  }
  return slots
})

// Aktuelle Zeit für Indikator
const currentTime = ref(new Date())
const isToday = computed(() => {
  if (!props.day) return true // Wenn kein Tag übergeben wird, zeige den Indikator immer an (für Rückwärtskompatibilität)
  return isSameDay(props.day, new Date())
})
const currentTimePosition = computed(() => {
  if (!isToday.value) return -1 // Nicht anzeigen, wenn nicht heute

  const now = currentTime.value
  const hour = now.getHours()
  const minutes = now.getMinutes()
  const totalMinutes = hour * 60 + minutes

  // Start bei 6 Uhr (360 Minuten), Ende bei 22 Uhr (1320 Minuten)
  const startMinutes = 6 * 60
  const endMinutes = 22 * 60
  const totalRange = endMinutes - startMinutes

  if (totalMinutes < startMinutes) return -1 // Vor 6 Uhr
  if (totalMinutes > endMinutes) return 100 // Nach 22 Uhr

  const position = ((totalMinutes - startMinutes) / totalRange) * 100
  return position
})

// Berechne Position für Appointment/Task im Zeitraster
function getTimePosition(startTime: Date, endTime: Date) {
  const startHour = startTime.getHours()
  const startMinutes = startTime.getMinutes()
  const endHour = endTime.getHours()
  const endMinutes = endTime.getMinutes()

  const startTotalMinutes = startHour * 60 + startMinutes
  const endTotalMinutes = endHour * 60 + endMinutes

  const startRange = 6 * 60 // 6 Uhr
  const endRange = 22 * 60 // 22 Uhr
  const totalRange = endRange - startRange

  const top = ((startTotalMinutes - startRange) / totalRange) * 100
  const height = ((endTotalMinutes - startTotalMinutes) / totalRange) * 100

  return { top, height }
}

// Typ für Timeline-Einträge
interface TimelineEntry {
  id: string
  startTime: Date
  endTime: Date
  type: 'appointment' | 'task'
  data: Appointment | Task
}

// Kombiniere Appointments und Tasks und berechne Spalten für Überlappungen
const timelineEntries = computed(() => {
  const entries: TimelineEntry[] = []

  // Appointments hinzufügen
  if (props.appointments) {
    props.appointments.forEach(appt => {
      entries.push({
        id: appt.id,
        startTime: new Date(appt.start_time),
        endTime: new Date(appt.end_time),
        type: 'appointment',
        data: appt
      })
    })
  }

  // Tasks hinzufügen
  if (props.tasksWithTime) {
    props.tasksWithTime.forEach(task => {
      if (task.start_time && task.end_time) {
        entries.push({
          id: task.id || '',
          startTime: new Date(task.start_time),
          endTime: new Date(task.end_time),
          type: 'task',
          data: task
        })
      }
    })
  }

  // Alphabetische Sortierung für Spaltenzuweisung
  // Zuerst nach Startzeit, dann alphabetisch nach Titel/Name
  entries.sort((a, b) => {
    const startDiff = a.startTime.getTime() - b.startTime.getTime()
    if (startDiff !== 0) return startDiff

    // Bei gleicher Startzeit: Sortiere alphabetisch nach Titel/Name
    const aTitle = a.type === 'appointment'
      ? (a.data as Appointment).title
      : (a.data as Task).name
    const bTitle = b.type === 'appointment'
      ? (b.data as Appointment).title
      : (b.data as Task).name

    return aTitle.localeCompare(bTitle, 'de', { sensitivity: 'base' })
  })

  // Verbesserter Algorithmus: Berechne Überlappungen dynamisch
  // Jedes Event bekommt eine Spalte basierend auf tatsächlichen Überlappungen in seinem Zeitraum
  // WICHTIG: Die Spaltenzuweisung erfolgt in der stabilen Sortierreihenfolge

  interface EntryLayout {
    column: number
    maxColumns: number // Maximale Anzahl gleichzeitiger Events in diesem Zeitraum
  }

  const entryLayouts = new Map<string, EntryLayout>()

  // Für jedes Event in der stabilen Reihenfolge: Finde alle Events, die zur gleichen Zeit aktiv sind
  entries.forEach((entry) => {
    // Finde alle Events, die mit diesem Event überlappen
    const overlappingEntries = entries.filter(otherEntry => {
      if (otherEntry.id === entry.id) return false
      return !(entry.endTime.getTime() <= otherEntry.startTime.getTime() ||
               entry.startTime.getTime() >= otherEntry.endTime.getTime())
    })

    // Berechne die maximale Anzahl gleichzeitiger Events im Zeitraum dieses Events
    // Dazu erstellen wir Zeitpunkte für Start und Ende aller überlappenden Events
    const timePoints: Array<{ time: number, type: 'start' | 'end', entryId?: string }> = []

    // Füge das aktuelle Event hinzu
    timePoints.push({ time: entry.startTime.getTime(), type: 'start', entryId: entry.id })
    timePoints.push({ time: entry.endTime.getTime(), type: 'end', entryId: entry.id })

    // Füge alle überlappenden Events hinzu
    overlappingEntries.forEach(otherEntry => {
      timePoints.push({ time: otherEntry.startTime.getTime(), type: 'start', entryId: otherEntry.id })
      timePoints.push({ time: otherEntry.endTime.getTime(), type: 'end', entryId: otherEntry.id })
    })

    // Sortiere Zeitpunkte
    timePoints.sort((a, b) => {
      if (a.time !== b.time) return a.time - b.time
      // Bei gleicher Zeit: 'end' vor 'start' (damit Events, die sich berühren, nicht als überlappend gelten)
      return a.type === 'end' ? -1 : 1
    })

    // Berechne maximale Anzahl gleichzeitiger Events
    // maxConcurrent zählt bereits alle Events (inklusive des aktuellen)
    let maxConcurrent = 0
    let currentCount = 0
    timePoints.forEach(point => {
      if (point.type === 'start') {
        currentCount++
        maxConcurrent = Math.max(maxConcurrent, currentCount)
      } else {
        currentCount--
      }
    })

    // maxConcurrent ist bereits die maximale Anzahl (inklusive des aktuellen Events)
    const maxColumns = Math.max(1, maxConcurrent)

    // Finde eine verfügbare Spalte für dieses Event
    // WICHTIG: Prüfe nur Events, die VOR diesem Event in der stabilen Sortierreihenfolge kommen
    // Das stellt sicher, dass die Spaltenzuweisung konsistent bleibt
    const usedColumns = new Set<number>()

    // Prüfe alle bereits platzierten Events, die mit diesem überlappen
    // Nur Events, die in der Sortierreihenfolge VOR diesem Event kommen, werden berücksichtigt
    overlappingEntries.forEach(otherEntry => {
      // Prüfe, ob das andere Event vor diesem Event in der Sortierreihenfolge kommt
      const otherIndex = entries.findIndex(e => e.id === otherEntry.id)
      const currentIndex = entries.findIndex(e => e.id === entry.id)

      if (otherIndex < currentIndex) {
        const otherLayout = entryLayouts.get(otherEntry.id)
        if (otherLayout) {
          usedColumns.add(otherLayout.column)
        }
      }
    })

    // Finde die erste verfügbare Spalte
    // Priorisiere die niedrigste verfügbare Spalte für Stabilität
    let assignedColumn = 0
    for (let col = 0; col < maxColumns; col++) {
      if (!usedColumns.has(col)) {
        assignedColumn = col
        break
      }
    }

    entryLayouts.set(entry.id, {
      column: assignedColumn,
      maxColumns
    })
  })

  return { entries, entryLayouts }
})

// Berechne linke Position für Spalten-Layout
function getColumnLeft(column: number, maxColumns: number): string {
  const baseLeft = props.hideTimeAxis ? 0 : (props.compact ? 40 : 48) // Zeitachse Breite
  const paddingLeft = props.padding === 'none' ? 0 : props.padding === 'sm' ? 4 : props.padding === 'md' ? 8 : 16
  const totalLeftOffset = baseLeft + paddingLeft
  const columnWidth = 100 / maxColumns
  const leftPercent = column * columnWidth
  const gapOffset = column * 2 // 2px gap zwischen Spalten
  return `calc(${totalLeftOffset}px + ${leftPercent}% + ${gapOffset}px)`
}

// Berechne Breite für Spalten-Layout
function getColumnWidth(maxColumns: number): string {
  const columnWidth = 100 / maxColumns
  const gapWidth = (maxColumns - 1) * 2 // 2px gap zwischen Spalten
  return `calc(${columnWidth}% - ${gapWidth / maxColumns}px)`
}

// Add Appointment Modal State
const showAddAppointment = ref(false)
const appointmentStartTime = ref<Date | undefined>(undefined)
const appointmentEndTime = ref<Date | undefined>(undefined)

// Runde Zeit auf 15-Minuten-Intervalle
function roundToQuarterHour(date: Date): Date {
  const minutes = date.getMinutes()
  const roundedMinutes = Math.round(minutes / 15) * 15
  const rounded = new Date(date)
  rounded.setMinutes(roundedMinutes, 0, 0)
  return rounded
}

// Berechne Zeit aus Klick-Position
function handleTimelineClick(event: MouseEvent) {
  // Ignoriere Klicks auf Einträge selbst (Appointments oder Tasks)
  const target = event.target as HTMLElement
  if (target.closest('.absolute.rounded')) return
  // Ignoriere auch Klicks auf Drag-Handles
  if (target.closest('.cursor-move') || target.closest('.cursor-ns-resize')) return

  const container = event.currentTarget as HTMLElement
  // Finde das innere Zeitraster-Element
  const timeGrid = container.querySelector('.relative') as HTMLElement
  if (!timeGrid) return

  const timeGridRect = timeGrid.getBoundingClientRect()

  // Berechne relative Position innerhalb des Zeitrasters
  const clickY = event.clientY - timeGridRect.top
  const totalHeight = timeGridRect.height

  if (totalHeight <= 0) return

  // Berechne Prozent-Position
  const percentPosition = Math.max(0, Math.min(100, (clickY / totalHeight) * 100))

  // Konvertiere zu Zeit
  const startRange = 6 * 60 // 6 Uhr in Minuten
  const endRange = 22 * 60 // 22 Uhr in Minuten
  const totalRange = endRange - startRange

  const minutesFromStart = (percentPosition / 100) * totalRange
  const totalMinutes = startRange + minutesFromStart

  const hours = Math.floor(totalMinutes / 60)
  const minutes = Math.floor(totalMinutes % 60)

  // Erstelle Datum mit der berechneten Zeit
  const clickTime = new Date()
  if (props.day) {
    clickTime.setFullYear(props.day.getFullYear(), props.day.getMonth(), props.day.getDate())
  } else {
    // Wenn kein Tag übergeben, verwende heute
    const today = new Date()
    clickTime.setFullYear(today.getFullYear(), today.getMonth(), today.getDate())
  }
  clickTime.setHours(hours, minutes, 0, 0)

  // Runde Zeit auf 15-Minuten-Intervalle
  const roundedTime = roundToQuarterHour(clickTime)

  // Standard-Dauer: 1 Stunde
  const endTime = new Date(roundedTime)
  endTime.setHours(endTime.getHours() + 1)

  // Setze initiale Zeiten und öffne Modal
  appointmentStartTime.value = roundedTime
  appointmentEndTime.value = endTime
  showAddAppointment.value = true

  // Falls ein Callback vorhanden ist, rufe ihn auch auf (für Rückwärtskompatibilität)
  if (props.onTimelineClick) {
    props.onTimelineClick(clickTime, event)
  }
}

function handleCreateAppointment(appointmentData: Omit<Appointment, 'id'>) {
  if (props.onCreateAppointment) {
    props.onCreateAppointment(appointmentData)
  }
  showAddAppointment.value = false
  appointmentStartTime.value = undefined
  appointmentEndTime.value = undefined
}

function handleCancelAppointment() {
  showAddAppointment.value = false
  appointmentStartTime.value = undefined
  appointmentEndTime.value = undefined
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
})
</script>

<template>
  <div class="flex flex-col h-full min-h-0">
    <div v-if="!props.hideHeader" :class="[paddingClasses.container, 'shrink-0 bg-background border-b border-border z-10']">
      <h3 class="font-semibold text-sm">Zeitplan</h3>
    </div>
    <div class="flex-1 overflow-y-auto min-h-0" :class="[paddingClasses.container]" style="max-height: 100%">
      <div
        :class="[
          'relative',
          paddingClasses.container,
          'time-schedule-container',
          props.dragOverTimeSchedule ? 'bg-primary/5' : '',
          (props.onCreateAppointment || props.onTimelineClick) ? 'cursor-pointer' : ''
        ]"
        @dragover="props.onDragOver"
        @dragleave="props.onDragLeave"
        @drop="props.onDrop"
        @click="handleTimelineClick"
      >
        <!-- Zeitraster Stunden -->
        <div class="relative">
          <!-- Stunden-Markierungen -->
          <div
            v-for="hour in timeSlots"
            :key="hour"
            class="flex items-start mb-0"
            :class="props.hideTimeAxis ? '' : 'gap-2'"
            :style="{ minHeight: props.compact ? '40px' : '60px' }"
          >
            <div
              v-if="!props.hideTimeAxis"
              class="text-muted-foreground shrink-0 flex items-center"
              :class="props.compact ? 'w-10 text-[10px]' : 'w-12 text-xs'"
              :style="{ height: props.compact ? '40px' : '60px' }"
            >
              {{ String(hour).padStart(2, '0') }}:00
            </div>
            <div class="flex-1 border-t border-border relative" style="margin-top: 0" />
          </div>

          <!-- Appointments (absolut positioniert mit Spalten-Layout) -->
          <template v-for="entry in timelineEntries.entries.filter(e => e.type === 'appointment')" :key="entry.id">
            <div
              v-if="timelineEntries.entryLayouts.has(entry.id)"
              class="absolute rounded shadow-sm z-20 cursor-move text-white px-1.5 py-0.5"
              :class="[
                (entry.data as Appointment).color || 'bg-blue-500',
                props.compact ? 'text-[10px]' : 'px-2 py-1 text-xs'
              ]"
              :style="{
                top: `${getTimePosition(entry.startTime, entry.endTime).top}%`,
                height: `${getTimePosition(entry.startTime, entry.endTime).height}%`,
                minHeight: props.compact ? '20px' : '32px',
                left: getColumnLeft(timelineEntries.entryLayouts.get(entry.id)!.column, timelineEntries.entryLayouts.get(entry.id)!.maxColumns),
                width: getColumnWidth(timelineEntries.entryLayouts.get(entry.id)!.maxColumns)
              }"
              @mousedown.stop="props.onDragTimedItem && props.onDragTimedItem($event, undefined, entry.data as Appointment)"
            >
              <div class="font-medium truncate">{{ (entry.data as Appointment).title }}</div>
              <div v-if="(entry.data as Appointment).location && !props.compact" class="text-xs opacity-90 truncate">
                {{ (entry.data as Appointment).location }}
              </div>
              <div :class="props.compact ? 'text-[9px]' : 'text-xs'" class="opacity-90">
                {{ format(entry.startTime, 'HH:mm', { locale: de }) }} - {{ format(entry.endTime, 'HH:mm', { locale: de }) }}
              </div>
            </div>
          </template>

          <!-- Tasks mit Zeit (absolut positioniert mit Spalten-Layout) -->
          <template v-for="entry in timelineEntries.entries.filter(e => e.type === 'task')" :key="entry.id">
            <div
              v-if="timelineEntries.entryLayouts.has(entry.id)"
              class="absolute rounded border shadow-sm z-10 group px-1.5 py-0.5"
              :class="[
                (entry.data as Task).project_id && props.getProjectBorderColor ? props.getProjectBorderColor((entry.data as Task).project_id!) + ' bg-background' : 'border-muted-foreground/20 bg-muted/30',
                isTaskCompleted(entry.data as Task) && 'opacity-60',
                props.compact ? 'text-[10px]' : 'px-2 py-1 text-xs'
              ]"
              :style="{
                top: `${getTimePosition(entry.startTime, entry.endTime).top}%`,
                height: `${getTimePosition(entry.startTime, entry.endTime).height}%`,
                minHeight: props.compact ? '20px' : '32px',
                left: getColumnLeft(timelineEntries.entryLayouts.get(entry.id)!.column, timelineEntries.entryLayouts.get(entry.id)!.maxColumns),
                width: getColumnWidth(timelineEntries.entryLayouts.get(entry.id)!.maxColumns)
              }"
            >
              <!-- Resize Handle oben -->
              <div
                v-if="props.onResize"
                class="absolute top-0 left-0 right-0 cursor-ns-resize opacity-0 group-hover:opacity-100 transition-opacity bg-primary/20 hover:bg-primary/40 rounded-t z-30"
                :class="props.compact ? 'h-1' : 'h-2'"
                @mousedown.stop="props.onResize(entry.data as Task, 'top', $event)"
              />

              <!-- Drag Area (mittlerer Bereich) -->
              <div
                v-if="props.onDragTimedItem"
                class="absolute inset-0 cursor-move"
                :class="props.compact ? 'pt-1 pb-1' : 'pt-2 pb-2'"
                :style="props.compact ? 'top: 4px; bottom: 4px' : 'top: 8px; bottom: 8px'"
                @mousedown.stop="props.onDragTimedItem($event, entry.data as Task)"
              >
                <div class="flex items-center gap-1 h-full">
                  <div
                    v-if="(entry.data as Task).project_id && props.getProjectColor"
                    class="rounded-full shrink-0"
                    :class="[props.getProjectColor((entry.data as Task).project_id!), props.compact ? 'w-1.5 h-1.5' : 'w-2 h-2']"
                  />
                  <span
                    class="font-medium truncate"
                    :class="isTaskCompleted(entry.data as Task) && 'line-through text-muted-foreground'"
                  >
                    {{ (entry.data as Task).name }}
                  </span>
                </div>
                <div
                  class="text-muted-foreground absolute bottom-0 left-0 right-0"
                  :class="[
                    props.compact ? 'text-[9px] px-1.5 pb-0.5' : 'text-xs left-2 right-2 pb-1'
                  ]"
                >
                  {{ format(entry.startTime, 'HH:mm', { locale: de }) }} - {{ format(entry.endTime, 'HH:mm', { locale: de }) }}
                </div>
              </div>
              <template v-else>
                <div class="flex items-center gap-1" :class="props.compact ? 'pt-1' : 'pt-2'">
                  <div
                    v-if="(entry.data as Task).project_id && props.getProjectColor"
                    class="rounded-full shrink-0"
                    :class="[props.getProjectColor((entry.data as Task).project_id!), props.compact ? 'w-1.5 h-1.5' : 'w-2 h-2']"
                  />
                  <span
                    class="font-medium truncate"
                    :class="isTaskCompleted(entry.data as Task) && 'line-through text-muted-foreground'"
                  >
                    {{ (entry.data as Task).name }}
                  </span>
                </div>
                <div :class="props.compact ? 'text-[9px]' : 'text-xs'" class="text-muted-foreground">
                  {{ format(entry.startTime, 'HH:mm', { locale: de }) }} - {{ format(entry.endTime, 'HH:mm', { locale: de }) }}
                </div>
              </template>

              <!-- Resize Handle unten -->
              <div
                v-if="props.onResize"
                class="absolute bottom-0 left-0 right-0 cursor-ns-resize opacity-0 group-hover:opacity-100 transition-opacity bg-primary/20 hover:bg-primary/40 rounded-b z-30"
                :class="props.compact ? 'h-1' : 'h-2'"
                @mousedown.stop="props.onResize(entry.data as Task, 'bottom', $event)"
              />
            </div>
          </template>

          <!-- Aktueller Zeit-Indikator (rote Linie) -->
          <div
            v-if="currentTimePosition >= 0 && currentTimePosition <= 100"
            class="absolute z-10 pointer-events-none left-0 right-0"
            :style="{ top: `${currentTimePosition}%` }"
          >
            <div class="flex items-center">
              <div
                class="rounded-full bg-red-500 shrink-0"
                :class="props.compact ? 'w-1.5 h-1.5 -ml-0.5' : 'w-2 h-2 -ml-1'"
              />
              <div class="h-0.5 bg-red-500 flex-1" />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Appointment Modal -->
    <AddAppointment
      v-if="props.onCreateAppointment"
      v-model:open="showAddAppointment"
      :initial-start-time="appointmentStartTime"
      :initial-end-time="appointmentEndTime"
      @create="handleCreateAppointment"
      @cancel="handleCancelAppointment"
    />
  </div>
</template>

