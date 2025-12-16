<script setup lang="ts">
import CreateProject from '~/components/create/CreateProject.vue'
import type { StatusTypes } from '~/types/Project'

const { closeSlideover } = useSlideover()

const currentStep = ref(1)
const totalSteps = 4

const createProjectRef = ref<InstanceType<typeof CreateProject> | null>(null)
const formData = computed(() => createProjectRef.value?.formData) as ComputedRef<{ name: string; description: string; status: StatusTypes } | undefined>
const startDateString = computed(() => createProjectRef.value?.startDateString) as ComputedRef<string | undefined>
const endDateString = computed(() => createProjectRef.value?.endDateString) as ComputedRef<string | undefined>
const canProceed = computed(() => {
  switch (currentStep.value) {
    case 1:
      return createProjectRef.value?.canProceed ?? false
    default:
      return true
  }
})

function nextStep() {
  if (currentStep.value < totalSteps && canProceed.value) {
    currentStep.value++
  }
}

function previousStep() {
  if (currentStep.value > 1) {
    currentStep.value--
  }
}

function goToStep(step: number) {
  if (step <= currentStep.value || (step === 1 && canProceed.value)) {
    currentStep.value = step
  }
}

async function handleCreateProject() {
  const ref = createProjectRef.value
  if (!ref || !formData.value) return

  await ref.createProject?.()
  closeSlideover('add-project')
}

// Reset form when component is mounted
onMounted(() => {
  const ref = createProjectRef.value
  if (ref && 'resetForm' in ref) {
    const resetFn = ref.resetForm as () => void
    resetFn()
  }
})

const steps = [
  { number: 1, title: 'Projektname', description: 'Gib einen Namen ein' },
  { number: 2, title: 'Beschreibung', description: 'Optional: Beschreibe dein Projekt' },
  { number: 3, title: 'Zeitraum', description: 'Optional: Start- und Enddatum' },
  { number: 4, title: 'Status', description: 'Wähle den initialen Status' }
]
</script>

<template>
  <div class="h-full flex flex-col bg-elevated/30">
    <!-- Header -->
    <div class="p-6 border-b border-default bg-elevated/50">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h2 class="text-2xl font-bold text-foreground">Neues Projekt erstellen</h2>
          <p class="text-sm text-muted-foreground mt-1">
            Folge den Schritten, um dein Projekt einzurichten
          </p>
        </div>
        <UButton
          variant="ghost"
          color="neutral"
          icon="i-lucide-x"
          size="sm"
          @click="closeSlideover('add-project')"
        />
      </div>

      <!-- Steps Navigation -->
      <div class="relative">
        <div class="absolute top-6 left-0 right-0 h-0.5 bg-muted -z-10" />
        <div class="flex items-center justify-between">
          <div
            v-for="step in steps"
            :key="step.number"
            class="flex flex-col items-center flex-1 relative"
          >
            <button
              type="button"
              class="relative z-10 flex items-center justify-center w-12 h-12 rounded-full transition-all duration-200"
              :class="[
                currentStep >= step.number
                  ? 'bg-primary text-primary-foreground shadow-lg shadow-primary/20 scale-110'
                  : 'bg-elevated border-2 border-default text-muted-foreground hover:border-primary/50',
                currentStep === step.number && 'ring-4 ring-primary/20'
              ]"
              :disabled="currentStep < step.number && step.number > 1"
              @click="goToStep(step.number)"
            >
              <span
                v-if="currentStep > step.number"
                class="text-lg"
              >
                <Icon name="i-lucide-check" class="w-6 h-6" />
              </span>
              <span
                v-else
                class="text-sm font-semibold"
              >
                {{ step.number }}
              </span>
            </button>
            <div class="mt-3 text-center max-w-[120px]">
              <p
                class="text-xs font-medium"
                :class="currentStep >= step.number ? 'text-foreground' : 'text-muted-foreground'"
              >
                {{ step.title }}
              </p>
              <p class="text-xs text-muted-foreground mt-0.5 hidden sm:block">
                {{ step.description }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Step Content -->
    <div class="flex-1 overflow-y-auto">
      <div class="max-w-2xl mx-auto p-8">
        <!-- Step 1: Name -->
        <div v-if="currentStep === 1" class="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
          <div class="space-y-2">
            <h3 class="text-2xl font-semibold text-foreground">Projektname</h3>
            <p class="text-muted-foreground">
              Gib einen aussagekräftigen Namen für dein Projekt ein. Dieser kann später noch geändert werden.
            </p>
          </div>
          <div class="space-y-2">
            <label class="text-sm font-medium text-foreground">Name *</label>
            <UInput
              v-if="formData"
              :model-value="formData?.name ?? ''"
              placeholder="z.B. Website Relaunch, Mobile App Development..."
              size="xl"
              autofocus
              class="w-full"
              @update:model-value="formData && (formData.name = $event as string)"
              @keyup.enter="nextStep"
            />
            <p class="text-xs text-muted-foreground">
              Der Name sollte das Projekt eindeutig identifizieren
            </p>
          </div>
        </div>

        <!-- Step 2: Description -->
        <div v-if="currentStep === 2" class="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
          <div class="space-y-2">
            <h3 class="text-2xl font-semibold text-foreground">Beschreibung</h3>
            <p class="text-muted-foreground">
              Füge eine detaillierte Beschreibung hinzu, um den Kontext und die Ziele des Projekts zu dokumentieren.
            </p>
          </div>
          <div class="space-y-2">
            <label class="text-sm font-medium text-foreground">Beschreibung</label>
            <UTextarea
              v-if="formData"
              :model-value="formData?.description ?? ''"
              placeholder="Beschreibe die Ziele, den Umfang und wichtige Details deines Projekts..."
              :rows="8"
              size="xl"
              class="w-full"
              @update:model-value="formData && (formData.description = $event as string)"
            />
            <p class="text-xs text-muted-foreground">
              Optional, aber empfohlen für bessere Projektorganisation
            </p>
          </div>
        </div>

        <!-- Step 3: Dates -->
        <div v-if="currentStep === 3" class="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
          <div class="space-y-2">
            <h3 class="text-2xl font-semibold text-foreground">Zeitraum</h3>
            <p class="text-muted-foreground">
              Definiere den geplanten Zeitraum für dein Projekt. Dies hilft bei der Planung und dem Fortschrittstracking.
            </p>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-2">
              <label class="text-sm font-medium text-foreground">Startdatum</label>
              <UInput
                v-if="startDateString"
                :model-value="startDateString ?? ''"
                type="date"
                size="xl"
                class="w-full"
                @update:model-value="startDateString && (startDateString = $event as string)"
              />
            </div>
            <div class="space-y-2">
              <label class="text-sm font-medium text-foreground">
                Enddatum
                <span class="text-muted-foreground font-normal">(optional)</span>
              </label>
              <UInput
                v-if="endDateString"
                :model-value="endDateString ?? ''"
                type="date"
                size="xl"
                class="w-full"
                :min="startDateString"
                @update:model-value="endDateString && (endDateString = $event as string)"
              />
            </div>
          </div>
          <div class="p-4 bg-primary/5 border border-primary/20 rounded-lg">
            <p class="text-sm text-muted-foreground">
              <Icon name="i-lucide-info" class="w-4 h-4 inline mr-1" />
              Das Enddatum kann später jederzeit angepasst werden, wenn sich der Projektumfang ändert.
            </p>
          </div>
        </div>

        <!-- Step 4: Status -->
        <div v-if="currentStep === 4" class="space-y-6 animate-in fade-in slide-in-from-right-4 duration-300">
          <div class="space-y-2">
            <h3 class="text-2xl font-semibold text-foreground">Status</h3>
            <p class="text-muted-foreground">
              Wähle den initialen Status für dein Projekt. Du kannst den Status später jederzeit ändern.
            </p>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <UCard
              :ui="{ body: 'p-6' }"
              :class="[
                'cursor-pointer transition-all hover:shadow-lg',
                formData?.status === 'active' && 'ring-2 ring-primary'
              ]"
              @click="formData && (formData.status = 'active' as any)"
            >
              <div class="flex items-start gap-4">
                <div
                  class="shrink-0 w-12 h-12 rounded-lg flex items-center justify-center"
                  :class="formData?.status === 'active' ? 'bg-success/10' : 'bg-muted'"
                >
                  <Icon
                    name="i-lucide-play-circle"
                    class="w-6 h-6"
                    :class="formData?.status === 'active' ? 'text-success' : 'text-muted-foreground'"
                  />
                </div>
                <div class="flex-1">
                  <h4 class="font-semibold text-foreground mb-1">Aktiv</h4>
                  <p class="text-sm text-muted-foreground">
                    Das Projekt ist aktiv und wird bearbeitet
                  </p>
                </div>
                <div
                  v-if="formData?.status === 'active'"
                  class="shrink-0"
                >
                  <Icon name="i-lucide-check-circle" class="w-5 h-5 text-success" />
                </div>
              </div>
            </UCard>

            <UCard
              :ui="{ body: 'p-6' }"
              :class="[
                'cursor-pointer transition-all hover:shadow-lg',
                formData?.status === 'paused' && 'ring-2 ring-primary'
              ]"
              @click="formData && (formData.status = 'paused' as any)"
            >
              <div class="flex items-start gap-4">
                <div
                  class="shrink-0 w-12 h-12 rounded-lg flex items-center justify-center"
                  :class="formData?.status === 'paused' ? 'bg-warning/10' : 'bg-muted'"
                >
                  <Icon
                    name="i-lucide-pause-circle"
                    class="w-6 h-6"
                    :class="formData?.status === 'paused' ? 'text-warning' : 'text-muted-foreground'"
                  />
                </div>
                <div class="flex-1">
                  <h4 class="font-semibold text-foreground mb-1">Pausiert</h4>
                  <p class="text-sm text-muted-foreground">
                    Das Projekt ist vorübergehend pausiert
                  </p>
                </div>
                <div
                  v-if="formData?.status === 'paused'"
                  class="shrink-0"
                >
                  <Icon name="i-lucide-check-circle" class="w-5 h-5 text-warning" />
                </div>
              </div>
            </UCard>
          </div>
        </div>
      </div>
    </div>

    <!-- Navigation Buttons -->
    <div class="p-6 border-t border-default bg-elevated/50">
      <div class="max-w-2xl mx-auto flex items-center justify-between gap-4">
        <UButton
          v-if="currentStep > 1"
          variant="ghost"
          color="neutral"
          icon="i-lucide-arrow-left"
          size="lg"
          @click="previousStep"
        >
          Zurück
        </UButton>
        <div v-else />

        <div class="flex gap-3">
          <UButton
            v-if="currentStep < totalSteps"
            :disabled="!canProceed"
            icon="i-lucide-arrow-right"
            icon-right
            size="lg"
            @click="nextStep"
          >
            Weiter
          </UButton>
          <UButton
            v-else
            :disabled="!canProceed"
            icon="i-lucide-check"
            size="lg"
            @click="handleCreateProject"
          >
            Projekt erstellen
          </UButton>
        </div>
      </div>
    </div>

    <!-- Hidden CreateProject component for logic -->
    <CreateProject ref="createProjectRef" />
  </div>
</template>
