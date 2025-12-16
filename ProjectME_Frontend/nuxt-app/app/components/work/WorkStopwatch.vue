<script setup lang="ts">
interface Props {
  selectedProject?: string | null
}

const props = defineProps<Props>()

const elapsedTime = ref(0) // in seconds
const isRunning = ref(false)
const laps = ref<Array<{ id: number, time: number, project?: string }>>([])

let interval: ReturnType<typeof setInterval> | null = null

const formattedTime = computed(() => {
  const hours = Math.floor(elapsedTime.value / 3600)
  const minutes = Math.floor((elapsedTime.value % 3600) / 60)
  const seconds = elapsedTime.value % 60

  if (hours > 0) {
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
  }
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
})

function startStopwatch() {
  if (isRunning.value) return

  isRunning.value = true
  interval = setInterval(() => {
    elapsedTime.value++
  }, 1000)
}

function pauseStopwatch() {
  isRunning.value = false
  if (interval) clearInterval(interval)
}

function stopStopwatch() {
  pauseStopwatch()
  elapsedTime.value = 0
  laps.value = []
}

function addLap() {
  if (elapsedTime.value === 0) return

  laps.value.unshift({
    id: Date.now(),
    time: elapsedTime.value,
    project: props.selectedProject || undefined
  })
}

function formatLapTime(seconds: number) {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = seconds % 60

  if (hours > 0) {
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  }
  return `${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
}

onUnmounted(() => {
  if (interval) clearInterval(interval)
})
</script>

<template>
  <div class="space-y-8">
    <!-- Stopwatch Display -->
    <UCard class="text-center">
      <div class="py-8">
        <div class="text-7xl font-bold tabular-nums mb-8">
          {{ formattedTime }}
        </div>

        <!-- Controls -->
        <div class="flex justify-center gap-4">
          <UButton
            v-if="!isRunning"
            icon="i-lucide-play"
            size="xl"
            @click="startStopwatch"
          >
            Start
          </UButton>
          <UButton
            v-else
            icon="i-lucide-pause"
            size="xl"
            color="error"
            @click="pauseStopwatch"
          >
            Pause
          </UButton>
          <UButton
            icon="i-lucide-square"
            size="xl"
            color="neutral"
            variant="outline"
            @click="stopStopwatch"
          >
            Stop
          </UButton>
          <UButton
            icon="i-lucide-flag"
            size="xl"
            color="neutral"
            variant="outline"
            :disabled="elapsedTime === 0"
            @click="addLap"
          >
            Runde
          </UButton>
        </div>
      </div>
    </UCard>

    <!-- Laps -->
    <UCard v-if="laps.length > 0">
      <template #header>
        <h3 class="font-semibold">
          Runden
        </h3>
      </template>
      <div class="space-y-2">
        <div
          v-for="lap in laps"
          :key="lap.id"
          class="flex items-center justify-between p-3 rounded-lg bg-muted/50"
        >
          <div>
            <p class="font-medium">
              {{ lap.project || 'Kein Projekt' }}
            </p>
          </div>
          <div class="text-lg font-semibold tabular-nums">
            {{ formatLapTime(lap.time) }}
          </div>
        </div>
      </div>
    </UCard>
  </div>
</template>

