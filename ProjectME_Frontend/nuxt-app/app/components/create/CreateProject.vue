<script setup lang="ts">
import { StatusTypes, type CreateProjectDto } from '~/types/Project'
import { useProjectStore } from '~/stores/useProjectStore'
const router = useRouter()
const toast = useToast()

// Form data
const formData = ref({
  name: '',
  description: '',
  status: StatusTypes.ACTIVE
})

function formatDateForInput(date: Date): string {
  return date.toISOString().split('T')[0]!
}

function parseDateFromInput(dateString: string): Date {
  return new Date(dateString)
}

const startDateString = ref(formatDateForInput(new Date()))
const endDateString = ref('')

// Validation
const canProceed = computed(() => {
  return formData.value.name?.trim().length > 0
})

const projectStore = useProjectStore()

// Create project function
async function createProject() {
  if (!formData.value.name?.trim()) {
    toast.add({
      title: 'Bitte gib einen Projektnamen ein',
      color: 'error'
    })
    return
  }

  const payload: CreateProjectDto = {
    name: formData.value.name.trim(),
    description: formData.value.description?.trim() || '',
    status: formData.value.status,
    start_date: startDateString.value ? parseDateFromInput(startDateString.value) : new Date(),
    last_deadline_date: endDateString.value ? parseDateFromInput(endDateString.value) : undefined
  }

  const created = await projectStore.createProject(payload)
  if (!created) return

  toast.add({
    title: 'Projekt erstellt',
    description: `${created.name} wurde erfolgreich erstellt.`,
    color: 'success'
  })

  // Navigate to project detail page
  router.push(`/projects/${created.id}`)

  return created
}

// Reset form
function resetForm() {
  formData.value = {
    name: '',
    description: '',
    status: StatusTypes.ACTIVE
  }
  startDateString.value = formatDateForInput(new Date())
  endDateString.value = ''
}

// Expose methods and data
defineExpose({
  formData,
  startDateString,
  endDateString,
  canProceed,
  createProject,
  resetForm
})
</script>

<template>
  <div />
</template>
