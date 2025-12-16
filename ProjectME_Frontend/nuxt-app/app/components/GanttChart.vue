<script setup lang="ts">
import { startOfMonth, endOfMonth, eachWeekOfInterval, format, differenceInDays, startOfWeek } from 'date-fns'
import { de } from 'date-fns/locale'
import type { Project } from '~/types/Project'

const props = defineProps<{
  projects: readonly Project[]
}>()

function getProjectEndDate(project: Project) {
  if (!project.last_deadline_date) return null
  return new Date(project.last_deadline_date)
}

// Zeitraum berechnen (frühester Start bis spätestes Ende aller Projekte)
const timeRange = computed(() => {
  const dates = props.projects.flatMap(p => {
    const endDate = getProjectEndDate(p)
    return [p.start_date, endDate].filter(Boolean) as Date[]
  })
  if (dates.length === 0) return { start: new Date(), end: new Date() }

  const earliest = new Date(Math.min(...dates.map(d => d.getTime())))
  const latest = new Date(Math.max(...dates.map(d => d.getTime())))

  return {
    start: startOfMonth(earliest),
    end: endOfMonth(latest)
  }
})

// Alle Wochen im Zeitraum
const weeks = computed(() => {
  return eachWeekOfInterval(timeRange.value, {
    weekStartsOn: 1,
    locale: de
  })
})

// Gesamte Tage im Zeitraum
const totalDays = computed(() => {
  const days = differenceInDays(timeRange.value.end, timeRange.value.start)
  return Math.max(days, 1)
})

// Berechne Position und Breite eines Projekts
function getProjectBar(project: Project) {
  const projectEnd = getProjectEndDate(project)
  if (!project.start_date || !projectEnd) return null

  const start = new Date(project.start_date)
  const end = projectEnd

  const daysFromStart = differenceInDays(start, timeRange.value.start)
  const duration = differenceInDays(end, start)

  const leftPercent = (daysFromStart / totalDays.value) * 100
  const widthPercent = (duration / totalDays.value) * 100

  return {
    left: `${Math.max(0, leftPercent)}%`,
    width: `${Math.min(100 - leftPercent, widthPercent)}%`
  }
}

function formatWeekLabel(date: Date) {
  const weekStart = startOfWeek(date, { weekStartsOn: 1, locale: de })
  return format(weekStart, 'dd.MM', { locale: de })
}

function getStatusColor(status: string) {
  switch (status) {
    case 'active': return 'bg-success'
    case 'paused': return 'bg-warning'
    case 'completed': return 'bg-info'
    default: return 'bg-neutral'
  }
}
</script>

<template>
  <div class="space-y-4">
    <!-- Gantt Chart -->
    <div class="overflow-x-auto">
      <div class="min-w-[800px]">
        <!-- Header: Wochen -->
        <div class="flex border-b border-default">
          <div class="w-48 shrink-0 p-3 font-semibold text-sm border-r border-default sticky left-0 bg-white dark:bg-gray-950 z-10">
            Projekt
          </div>
          <div class="flex-1 flex">
            <div
              v-for="(week, idx) in weeks"
              :key="idx"
              class="flex-1 p-2 text-center text-xs text-muted-foreground border-r border-default last:border-r-0"
            >
              <div class="font-medium">
                KW {{ format(week, 'w', { locale: de }) }}
              </div>
              <div class="text-[10px] mt-0.5">
                {{ formatWeekLabel(week) }}
              </div>
            </div>
          </div>
        </div>

        <!-- Projekt-Zeilen -->
        <div
          v-for="project in projects"
          :key="project.id"
          class="flex border-b border-default hover:bg-muted/30 transition-colors group"
        >
          <!-- Projekt Name -->
          <div class="w-48 shrink-0 p-3 border-r border-default sticky left-0 bg-white dark:bg-gray-950 group-hover:bg-muted/30 transition-colors z-10">
            <div class="font-medium text-sm truncate">
              {{ project.name }}
            </div>
            <div class="text-xs text-muted-foreground mt-0.5">
              {{ project.status }}
            </div>
          </div>

          <!-- Timeline -->
          <div class="flex-1 relative py-4 px-2">
            <!-- Grid Lines -->
            <div class="absolute inset-0 flex">
              <div
                v-for="(week, idx) in weeks"
                :key="idx"
                class="flex-1 border-r border-default/50 last:border-r-0"
              />
            </div>

            <!-- Projekt Bar -->
            <div
              v-if="getProjectBar(project)"
              class="absolute h-6 rounded-md flex items-center px-2 text-xs font-medium text-white cursor-pointer transition-all hover:opacity-90 hover:scale-[1.02]"
              :class="getStatusColor(project.status)"
              :style="{
                left: getProjectBar(project)?.left,
                width: getProjectBar(project)?.width,
                top: '50%',
                transform: 'translateY(-50%)'
              }"
              @click="$router.push(`/projects/${project.id}`)"
            >
              <span class="truncate">
                {{ project.name }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Legende -->
    <div class="flex items-center gap-4 text-xs">
      <span class="text-muted-foreground">Status:</span>
      <div class="flex items-center gap-2">
        <div class="w-3 h-3 rounded bg-success" />
        <span>Aktiv</span>
      </div>
      <div class="flex items-center gap-2">
        <div class="w-3 h-3 rounded bg-warning" />
        <span>Pausiert</span>
      </div>
      <div class="flex items-center gap-2">
        <div class="w-3 h-3 rounded bg-info" />
        <span>Abgeschlossen</span>
      </div>
    </div>
  </div>
</template>

