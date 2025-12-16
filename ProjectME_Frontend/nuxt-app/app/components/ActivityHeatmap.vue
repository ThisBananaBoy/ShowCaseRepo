<script setup lang="ts">
import { startOfWeek, endOfWeek, eachDayOfInterval, format, subWeeks, startOfDay } from 'date-fns'
import { de } from 'date-fns/locale'
import type { TimeEntry } from '~/types/TimeEntry'
import type { Project } from '~/types/Project'

const props = defineProps<{
  timeEntries: readonly TimeEntry[]
  projects: readonly Project[]
}>()

// Zeitraum: letzte 12 Wochen
const endDate = computed(() => endOfWeek(new Date(), { weekStartsOn: 1, locale: de }))
const startDate = computed(() => startOfWeek(subWeeks(endDate.value, 11), { weekStartsOn: 1, locale: de }))

// Alle Tage im Zeitraum
const allDays = computed(() => {
  return eachDayOfInterval({ start: startDate.value, end: endDate.value })
})

// Gruppiere Tage nach Wochen (für Spalten)
const weeks = computed(() => {
  const result: Date[][] = []
  let currentWeek: Date[] = []

  allDays.value.forEach((day, idx) => {
    currentWeek.push(day)
    if ((idx + 1) % 7 === 0 || idx === allDays.value.length - 1) {
      result.push([...currentWeek])
      currentWeek = []
    }
  })

  return result
})

// Aktivitätsdaten nach Datum und Projekt gruppieren
const activityMap = computed(() => {
  const map = new Map<string, Map<string, number>>()

  props.timeEntries.forEach(entry => {
    const dateKey = format(startOfDay(entry.start_time), 'yyyy-MM-dd')
    const projectId = entry.project_id || 'unknown'
    const durationMinutes = entry.end_time
      ? Math.max(0, Math.round((new Date(entry.end_time).getTime() - new Date(entry.start_time).getTime()) / 60000))
      : 0

    if (!map.has(dateKey)) {
      map.set(dateKey, new Map())
    }

    const dayMap = map.get(dateKey)!
    const currentMinutes = dayMap.get(projectId) || 0
    dayMap.set(projectId, currentMinutes + durationMinutes)
  })

  return map
})

// Farben für Projekte
const projectColors = computed(() => {
  const colors = [
    'bg-blue-500',
    'bg-green-500',
    'bg-purple-500',
    'bg-orange-500',
    'bg-pink-500',
    'bg-teal-500',
    'bg-red-500',
    'bg-yellow-500'
  ]

  const map = new Map<string, string>()
  props.projects.forEach((project, idx) => {
    const projectId = project.id || 'unknown'
    const color = colors[idx % colors.length] || 'bg-gray-500'
    map.set(projectId, color)
  })

  return map
})

// Intensität basierend auf Minuten
function getIntensity(minutes: number): string {
  if (minutes === 0) return 'opacity-10'
  if (minutes < 60) return 'opacity-30'
  if (minutes < 120) return 'opacity-50'
  if (minutes < 180) return 'opacity-70'
  return 'opacity-100'
}

// Tooltip-Daten für einen Tag
function getDayTooltip(day: Date) {
  const dateKey = format(day, 'yyyy-MM-dd')
  const dayActivities = activityMap.value.get(dateKey)

  if (!dayActivities || dayActivities.size === 0) {
    return {
      date: format(day, 'dd. MMM yyyy', { locale: de }),
      projects: [],
      total: 0
    }
  }

  const projects = Array.from(dayActivities.entries()).map(([projectId, minutes]) => {
  const project = props.projects.find(p => p.id === projectId)
    return {
      name: project?.name || 'Unbekannt',
      minutes,
      color: projectColors.value.get(projectId) || 'bg-gray-500'
    }
  })

  const total = projects.reduce((sum, p) => sum + p.minutes, 0)

  return {
    date: format(day, 'dd. MMM yyyy', { locale: de }),
    projects,
    total
  }
}

// Hauptfarbe für einen Tag (Projekt mit meisten Minuten)
function getDayColor(day: Date): string {
  const dateKey = format(day, 'yyyy-MM-dd')
  const dayActivities = activityMap.value.get(dateKey)

  if (!dayActivities || dayActivities.size === 0) {
    return 'bg-gray-200 dark:bg-gray-800'
  }

  let maxMinutes = 0
  let maxProjectId = ''

  dayActivities.forEach((minutes, projectId) => {
    if (minutes > maxMinutes) {
      maxMinutes = minutes
      maxProjectId = projectId
    }
  })

  return projectColors.value.get(maxProjectId) || 'bg-gray-500'
}

function formatMinutes(minutes: number): string {
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`
}

// Wochentage Labels
const weekdayLabels = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So']

function getWeekLabel(week: Date[], weekIdx: number) {
  const firstDay = week[0]
  if (!firstDay) return ''
  const prevFirst = weeks.value[weekIdx - 1]?.[0]
  const currentMonth = format(firstDay, 'M', { locale: de })
  const prevMonth = prevFirst ? format(prevFirst, 'M', { locale: de }) : null
  if (weekIdx === 0 || currentMonth !== prevMonth) {
    return format(firstDay, 'MMM', { locale: de })
  }
  return ''
}
</script>

<template>
  <div class="space-y-4">
    <div class="overflow-x-auto">
      <div class="inline-block min-w-full">
        <div class="flex gap-1">
          <!-- Wochentag Labels -->
          <div class="flex flex-col gap-1 text-xs text-muted-foreground pr-2">
            <div class="h-3" />
            <div v-for="(label, idx) in weekdayLabels" :key="idx" class="h-3 flex items-center">
              {{ label }}
            </div>
          </div>

          <!-- Heatmap Grid -->
          <div class="flex gap-1">
            <div v-for="(week, weekIdx) in weeks" :key="weekIdx" class="flex flex-col gap-1">
              <!-- Monat Label -->
              <div class="h-3 text-xs text-muted-foreground">
                <span>{{ getWeekLabel(week, weekIdx) }}</span>
              </div>

              <!-- Tage der Woche -->
              <UTooltip
                v-for="(day, dayIdx) in week"
                :key="dayIdx"
                class="group"
              >
                <template #text>
                  <div class="text-xs">
                    <div class="font-semibold mb-1">
                      {{ getDayTooltip(day).date }}
                    </div>
                    <div v-if="getDayTooltip(day).projects.length > 0" class="space-y-1">
                      <div
                        v-for="project in getDayTooltip(day).projects"
                        :key="project.name"
                        class="flex items-center justify-between gap-3"
                      >
                        <div class="flex items-center gap-1">
                          <div :class="[project.color, 'w-2 h-2 rounded-sm']" />
                          <span>{{ project.name }}</span>
                        </div>
                        <span class="font-medium">{{ formatMinutes(project.minutes) }}</span>
                      </div>
                      <div class="border-t border-muted-foreground/20 pt-1 mt-1 flex justify-between">
                        <span class="font-semibold">Gesamt:</span>
                        <span class="font-semibold">{{ formatMinutes(getDayTooltip(day).total) }}</span>
                      </div>
                    </div>
                    <div v-else class="text-muted-foreground">
                      Keine Aktivität
                    </div>
                  </div>
                </template>

                <div
                  class="w-3 h-3 rounded-sm cursor-pointer transition-transform hover:scale-150 hover:ring-2 hover:ring-primary"
                  :class="[
                    getDayColor(day),
                    getIntensity(getDayTooltip(day).total)
                  ]"
                />
              </UTooltip>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Legende -->
    <div class="flex items-center gap-4 text-xs flex-wrap">
      <span class="text-muted-foreground">Projekte:</span>
      <div
        v-for="project in projects"
        :key="project.id"
        class="flex items-center gap-2"
      >
        <div :class="[projectColors.get(project.id), 'w-3 h-3 rounded-sm']" />
        <span>{{ project.name }}</span>
      </div>
    </div>

    <div class="flex items-center gap-2 text-xs text-muted-foreground">
      <span>Weniger</span>
      <div class="flex gap-1">
        <div class="w-3 h-3 rounded-sm bg-gray-200 dark:bg-gray-800" />
        <div class="w-3 h-3 rounded-sm bg-green-500 opacity-30" />
        <div class="w-3 h-3 rounded-sm bg-green-500 opacity-50" />
        <div class="w-3 h-3 rounded-sm bg-green-500 opacity-70" />
        <div class="w-3 h-3 rounded-sm bg-green-500 opacity-100" />
      </div>
      <span>Mehr</span>
    </div>
  </div>
</template>

