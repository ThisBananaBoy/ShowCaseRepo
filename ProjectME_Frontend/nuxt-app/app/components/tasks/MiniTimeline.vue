<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import {  isSameDay } from 'date-fns'
import type { Task } from '~/types/Task'
import type { Appointment } from '~/types/Appointment'
import { isTaskCompleted } from '~/utils/taskHelpers'


const props = defineProps<{
  day: Date
  appointments?: Appointment[]
  tasksWithTime?: Task[]
  getProjectColor?: (projectId: string) => string
  getProjectBorderColor?: (projectId: string) => string
}>()

// Zeitraster: Stunden von 6 bis 22 Uhr
const timeSlots = computed(() => {
  const slots = []
  for (let hour = 6; hour <= 22; hour++) {
    slots.push(hour)
  }
  return slots
})

// Filtere Events für den spezifischen Tag
const dayAppointments = computed(() => {
  if (!props.appointments) return []
  return props.appointments.filter(apt => {
    const aptDate = new Date(apt.start_time)
    return isSameDay(aptDate, props.day)
  })
})

const dayTasksWithTime = computed(() => {
  if (!props.tasksWithTime) return []
  return props.tasksWithTime.filter(task => {
    if (!task.start_time) return false
    const taskDate = new Date(task.start_time)
    return isSameDay(taskDate, props.day)
  })
})

// Aktuelle Zeit für Indikator (nur wenn heute)
const currentTime = ref(new Date())
const isToday = computed(() => isSameDay(currentTime.value, props.day))
const currentTimePosition = computed(() => {
  if (!isToday.value) return -1

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
  <div class="w-full h-32 border-b border-border bg-muted/20 relative overflow-hidden">
    <div class="relative h-full p-1">
      <!-- Zeitraster Stunden (kompakt) -->
      <div class="relative h-full">
        <!-- Stunden-Markierungen (nur jede 2. Stunde) -->
        <div
          v-for="hour in timeSlots"
          :key="hour"
          class="absolute left-0 right-0"
          :style="{
            top: `${((hour - 6) / 16) * 100}%`,
            height: `${(1 / 16) * 100}%`
          }"
        >
          <div
            v-if="hour % 2 === 0"
            class="text-[8px] text-muted-foreground/60 px-1"
          >
            {{ String(hour).padStart(2, '0') }}:00
          </div>
          <div class="absolute top-0 left-8 right-0 border-t border-border/30" />
        </div>

        <!-- Appointments (absolut positioniert) -->
        <div
          v-for="appt in dayAppointments"
          :key="appt.id"
          class="absolute left-12 right-1 rounded px-1 py-0.5 text-[9px] text-white shadow-sm z-20"
          :class="appt.color || 'bg-blue-500'"
          :style="{
            top: `${getTimePosition(new Date(appt.start_time), new Date(appt.end_time)).top}%`,
            height: `${getTimePosition(new Date(appt.start_time), new Date(appt.end_time)).height}%`,
            minHeight: '12px'
          }"
        >
          <div class="font-medium truncate leading-tight">{{ appt.title }}</div>
        </div>

        <!-- Tasks mit Zeit (absolut positioniert) -->
        <div
          v-for="task in dayTasksWithTime"
          :key="task.id"
          class="absolute left-12 right-1 rounded px-1 py-0.5 text-[9px] border shadow-sm z-10"
          :class="[
            task.project_id && props.getProjectBorderColor ? props.getProjectBorderColor(task.project_id) + ' bg-background' : 'border-muted-foreground/20 bg-muted/30',
            isTaskCompleted(task) && 'opacity-60'
          ]"
          :style="{
            top: `${getTimePosition(new Date(task.start_time!), new Date(task.end_time!)).top}%`,
            height: `${getTimePosition(new Date(task.start_time!), new Date(task.end_time!)).height}%`,
            minHeight: '12px'
          }"
        >
          <div class="flex items-center gap-0.5">
            <div
              v-if="task.project_id && props.getProjectColor"
              class="w-1 h-1 rounded-full shrink-0"
              :class="props.getProjectColor(task.project_id)"
            />
            <span
              class="font-medium truncate leading-tight text-[8px]"
              :class="isTaskCompleted(task) && 'line-through text-muted-foreground'"
            >
              {{ task.name }}
            </span>
          </div>
        </div>

        <!-- Aktueller Zeit-Indikator (rote Linie) -->
        <div
          v-if="isToday && currentTimePosition >= 0 && currentTimePosition <= 100"
          class="absolute left-12 right-1 z-30 pointer-events-none"
          :style="{ top: `${currentTimePosition}%` }"
        >
          <div class="flex items-center">
            <div class="w-1 h-1 rounded-full bg-red-500 shrink-0 -ml-0.5" />
            <div class="h-0.5 bg-red-500 flex-1" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

