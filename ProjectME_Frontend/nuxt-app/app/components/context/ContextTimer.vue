<script setup lang="ts">
interface Props {
  selectedProject?: string | null
}

const props = defineProps<Props>()

const timerMinutes = ref(25)
const timeRemaining = ref(25 * 60) // in seconds
const isRunning = ref(false)

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

onUnmounted(() => {
  if (interval) clearInterval(interval)
})
</script>

<template>
  <div class="space-y-4 "  >
    <!-- Timer Display -->
    <div class="text-center">
      <div class="relative inline-flex items-center justify-center">
        <svg class="size-48 -rotate-90">
          <circle
            cx="96"
            cy="96"
            r="84"
            stroke="currentColor"
            stroke-width="6"
            fill="none"
            class="text-muted"
          />
          <circle
            cx="96"
            cy="96"
            r="84"
            stroke="currentColor"
            stroke-width="6"
            fill="none"
            class="text-primary transition-all duration-1000"
            :stroke-dasharray="`${2 * Math.PI * 84}`"
            :stroke-dashoffset="`${2 * Math.PI * 84 * (1 - progress / 100)}`"
            stroke-linecap="round"
          />
        </svg>
        <div class="absolute inset-0 flex items-center justify-center">
          <div class="text-4xl font-bold tabular-nums">
            {{ formattedTime }}
          </div>
        </div>
      </div>

      <!-- Controls -->
      <div class="flex justify-center gap-2 mt-4">
        <UButton
          v-if="!isRunning"
          icon="i-lucide-play"
          size="md"
          @click="startTimer"
        >
          Start
        </UButton>
        <UButton
          v-else
          icon="i-lucide-pause"
          size="md"
          color="secondary"
          @click="pauseTimer"
        >
          Pause
        </UButton>
        <UButton
          icon="i-lucide-square"
          size="md"
          color="neutral"
          variant="outline"
          @click="stopTimer"
        >
          Stop
        </UButton>
      </div>

      <!-- Quick Duration -->
      <div class="flex justify-center gap-2 mt-4">
        <UButton
          v-for="duration in [15, 25, 45]"
          :key="duration"
          size="xs"
          color="neutral"
          :variant="timerMinutes === duration ? 'solid' : 'ghost'"
          @click="setDuration(duration)"
        >
          {{ duration }} min
        </UButton>
      </div>
    </div>
  </div>
</template>

