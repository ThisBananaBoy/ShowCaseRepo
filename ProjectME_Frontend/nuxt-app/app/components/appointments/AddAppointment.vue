<script setup lang="ts">
import type { CreateAppointmentDto } from '~/types/Appointment'

const props = withDefaults(defineProps<{
  initialStartTime?: Date
  initialEndTime?: Date
}>(), {
  initialStartTime: () => {
    const now = new Date()
    now.setMinutes(Math.round(now.getMinutes() / 15) * 15, 0, 0)
    return now
  },
  initialEndTime: () => {
    const end = new Date()
    end.setHours(end.getHours() + 1)
    end.setMinutes(Math.round(end.getMinutes() / 15) * 15, 0, 0)
    return end
  }
})

const emit = defineEmits<{
  (e: 'create', appointment: CreateAppointmentDto): void
  (e: 'cancel'): void
}>()

const open = defineModel<boolean>('open', { default: false })

const formState = reactive({
  title: '',
  description: '',
  location: '',
  startTime: new Date(props.initialStartTime),
  endTime: new Date(props.initialEndTime),
  color: 'bg-blue-500',
  project_id: undefined as string | undefined
})

// Aktualisiere Formular wenn Props sich ändern
watch(() => props.initialStartTime, (newTime) => {
  if (newTime) {
    formState.startTime = new Date(newTime)
  }
}, { immediate: true })

watch(() => props.initialEndTime, (newTime) => {
  if (newTime) {
    formState.endTime = new Date(newTime)
  }
}, { immediate: true })

// Verfügbare Farben für Appointments
const colors = [
  { value: 'bg-blue-500', label: 'Blau', class: 'bg-blue-500' },
  { value: 'bg-purple-500', label: 'Lila', class: 'bg-purple-500' },
  { value: 'bg-pink-500', label: 'Rosa', class: 'bg-pink-500' },
  { value: 'bg-orange-500', label: 'Orange', class: 'bg-orange-500' },
  { value: 'bg-teal-500', label: 'Türkis', class: 'bg-teal-500' },
  { value: 'bg-indigo-500', label: 'Indigo', class: 'bg-indigo-500' },
  { value: 'bg-rose-500', label: 'Rose', class: 'bg-rose-500' },
  { value: 'bg-amber-500', label: 'Amber', class: 'bg-amber-500' }
]

// Format für Datum/Zeit Inputs
function formatDateTimeForInput(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${year}-${month}-${day}T${hours}:${minutes}`
}

function parseDateTimeFromInput(dateTimeString: string): Date {
  return new Date(dateTimeString)
}

const startDateTimeString = computed({
  get: () => formatDateTimeForInput(formState.startTime),
  set: (value) => {
    formState.startTime = parseDateTimeFromInput(value)
  }
})

const endDateTimeString = computed({
  get: () => formatDateTimeForInput(formState.endTime),
  set: (value) => {
    formState.endTime = parseDateTimeFromInput(value)
  }
})

// Validierung
const canSubmit = computed(() => {
  return formState.title.trim().length > 0 &&
         formState.startTime < formState.endTime
})

function handleSubmit() {
  if (!canSubmit.value) return

  emit('create', {
    title: formState.title.trim(),
    description: formState.description.trim() || undefined,
    location: formState.location.trim() || undefined,
    start_time: new Date(formState.startTime),
    end_time: new Date(formState.endTime),
    color: formState.color,
    project_id: formState.project_id
  })

  // Reset form
  formState.title = ''
  formState.description = ''
  formState.location = ''
  formState.startTime = new Date(props.initialStartTime)
  formState.endTime = new Date(props.initialEndTime)
  formState.color = 'bg-blue-500'
  formState.project_id = undefined

  open.value = false
}

function handleCancel() {
  emit('cancel')
  open.value = false
}

// Reset form when modal opens
watch(open, (isOpen) => {
  if (isOpen) {
    formState.startTime = new Date(props.initialStartTime)
    formState.endTime = new Date(props.initialEndTime)
  }
})
</script>

<template>
  <UModal
    v-model:open="open"
    title="Neuer Termin"
    description="Erstelle einen neuen Termin"
    :ui="{ content: 'w-full sm:max-w-md' }"
  >
    <template #body>
      <div class="space-y-4">
        <!-- Titel -->
        <UFormField label="Titel" required>
          <UInput
            v-model="formState.title"
            placeholder="z.B. Team Meeting"
            class="w-full"
            autofocus
          />
        </UFormField>

        <!-- Beschreibung -->
        <UFormField label="Beschreibung">
          <UTextarea
            v-model="formState.description"
            placeholder="Optionale Beschreibung"
            class="w-full"
            :rows="3"
          />
        </UFormField>

        <!-- Ort -->
        <UFormField label="Ort">
          <UInput
            v-model="formState.location"
            placeholder="z.B. Konferenzraum A"
            class="w-full"
          />
        </UFormField>

        <!-- Startzeit -->
        <UFormField label="Startzeit" required>
          <UInput
            v-model="startDateTimeString"
            type="datetime-local"
            class="w-full"
          />
        </UFormField>

        <!-- Endzeit -->
        <UFormField label="Endzeit" required>
          <UInput
            v-model="endDateTimeString"
            type="datetime-local"
            class="w-full"
          />
        </UFormField>

        <!-- Farbe -->
        <UFormField label="Farbe">
          <div class="flex gap-2 flex-wrap">
            <button
              v-for="color in colors"
              :key="color.value"
              type="button"
              class="w-8 h-8 rounded-full border-2 transition-all"
              :class="[
                color.class,
                formState.color === color.value ? 'border-foreground scale-110' : 'border-transparent hover:scale-105'
              ]"
              :title="color.label"
              @click="formState.color = color.value"
            />
          </div>
        </UFormField>

        <!-- Validierungsfehler -->
        <div
          v-if="formState.startTime >= formState.endTime"
          class="text-sm text-red-500"
        >
          Endzeit muss nach Startzeit liegen
        </div>

        <!-- Buttons -->
        <div class="flex justify-end gap-2 pt-2">
          <UButton
            label="Abbrechen"
            color="neutral"
            variant="subtle"
            @click="handleCancel"
          />
          <UButton
            label="Erstellen"
            color="primary"
            variant="solid"
            :disabled="!canSubmit"
            @click="handleSubmit"
          />
        </div>
      </div>
    </template>
  </UModal>
</template>
