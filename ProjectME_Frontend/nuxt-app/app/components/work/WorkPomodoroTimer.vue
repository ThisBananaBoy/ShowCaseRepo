<script setup lang="ts">
interface Props {
  selectedProject?: string | null
}

const props = defineProps<Props>()

const timerMinutes = ref(25)
const timeRemaining = ref(25 * 60) // in seconds
const isRunning = ref(false)

const sessions = ref([
  { id: 1, project: 'Projekt A', duration: 25, date: new Date(Date.now() - 3600000) },
  { id: 2, project: 'Projekt B', duration: 15, date: new Date(Date.now() - 7200000) }
])

let interval: ReturnType<typeof setInterval> | null = null

const formattedTime = computed(() => {
  const minutes = Math.floor(timeRemaining.value / 60)
  const seconds = timeRemaining.value % 60
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
})

const progress = computed(() => {
  const total = timerMinutes.value * 60
  return ((total - timeRemaining.value) / total) * 100
})

function startTimer() {
  if (isRunning.value) return

  isRunning.value = true
  interval = setInterval(() => {
    if (timeRemaining.value > 0) {
      timeRemaining.value--
    } else {
      stopTimer()
      // Timer abgelaufen
      if (props.selectedProject) {
        sessions.value.unshift({
          id: Date.now(),
          project: props.selectedProject,
          duration: timerMinutes.value,
          date: new Date()
        })
      }
    }
  }, 1000)
}

function pauseTimer() {
  isRunning.value = false
  if (interval) clearInterval(interval)
}

function stopTimer() {
  pauseTimer()
  timeRemaining.value = timerMinutes.value * 60
}

function setDuration(minutes: number) {
  if (isRunning.value) return
  timerMinutes.value = minutes
  timeRemaining.value = minutes * 60
}

function formatDate(date: Date) {
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const hours = Math.floor(diff / 3600000)

  if (hours < 1) return 'Gerade eben'
  if (hours === 1) return 'Vor 1 Stunde'
  if (hours < 24) return `Vor ${hours} Stunden`
  return date.toLocaleDateString('de-DE')
}

onUnmounted(() => {
  if (interval) clearInterval(interval)
})
</script>

<template>
  <div class="space-y-8">
    <!-- Timer Display -->
    <UCard class="text-center">
      <div class="py-8">
        <div class="relative inline-flex items-center justify-center">
          <svg class="size-64 -rotate-90">
            <circle
              cx="128"
              cy="128"
              r="112"
              stroke="currentColor"
              stroke-width="8"
              fill="none"
              class="text-muted"
            />
            <circle
              cx="128"
              cy="128"
              r="112"
              stroke="currentColor"
              stroke-width="8"
              fill="none"
              class="text-primary transition-all duration-1000"
              :stroke-dasharray="`${2 * Math.PI * 112}`"
              :stroke-dashoffset="`${2 * Math.PI * 112 * (1 - progress / 100)}`"
              stroke-linecap="round"
            />
          </svg>
          <div class="absolute inset-0 flex items-center justify-center">
            <div class="text-6xl font-bold tabular-nums">
              {{ formattedTime }}
            </div>
          </div>
        </div>

        <!-- Controls -->
        <div class="flex justify-center gap-4 mt-8">
          <UButton
            v-if="!isRunning"
            icon="i-lucide-play"
            size="xl"
            @click="startTimer"
          >
            Start
          </UButton>
          <UButton
            v-else
            icon="i-lucide-pause"
            size="xl"
            color="yellow"
            @click="pauseTimer"
          >
            Pause
          </UButton>
          <UButton
            icon="i-lucide-square"
            size="xl"
            color="neutral"
            variant="outline"
            @click="stopTimer"
          >
            Stop
          </UButton>
        </div>

        <!-- Quick Duration -->
        <div class="flex justify-center gap-2 mt-6">
          <UButton
            v-for="duration in [15, 25, 45]"
            :key="duration"
            size="sm"
            color="neutral"
            :variant="timerMinutes === duration ? 'solid' : 'ghost'"
            @click="setDuration(duration)"
          >
            {{ duration }} min
          </UButton>
        </div>
      </div>
    </UCard>

    <!-- Recent Sessions -->
    <UCard>
      <template #header>
        <h3 class="font-semibold">
          Letzte Sessions
        </h3>
      </template>
      <div class="space-y-2">
        <div
          v-for="session in sessions"
          :key="session.id"
          class="flex items-center justify-between p-3 rounded-lg bg-muted/50"
        >
          <div>
            <p class="font-medium">
              {{ session.project }}
            </p>
            <p class="text-xs text-muted-foreground">
              {{ formatDate(session.date) }}
            </p>
          </div>
          <div class="text-sm font-semibold">
            {{ session.duration }} min
          </div>
        </div>
        <div v-if="sessions.length === 0" class="text-center py-8 text-muted-foreground">
          Noch keine Sessions
        </div>
      </div>
    </UCard>
  </div>
</template>

