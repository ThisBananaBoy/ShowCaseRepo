<script setup lang="ts">
interface Props {
  modelValue?: string
  isEditing?: boolean
  placeholder?: string
  size?: 'sm' | 'md'
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  isEditing: false,
  placeholder: 'Neue Aufgabe hinzufügen',
  size: 'md'
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'update:isEditing': [value: boolean]
  'start-editing': [event?: MouseEvent]
  'submit': [value: string]
  'cancel': []
}>()

const localValue = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const localEditing = computed({
  get: () => props.isEditing,
  set: (value) => emit('update:isEditing', value)
})

function handleStartEditing(event?: MouseEvent) {
  localEditing.value = true
  emit('start-editing', event)
  nextTick(() => {
    const input = document.querySelector('.new-task-input') as HTMLInputElement
    input?.focus()
  })
}

function handleSubmit() {
  if (localValue.value.trim()) {
    emit('submit', localValue.value.trim())
  } else {
    handleCancel()
  }
}

function handleCancel() {
  localValue.value = ''
  localEditing.value = false
  emit('cancel')
}

const paddingClass = props.size === 'sm' ? 'px-2 py-1' : 'px-2 py-1.5'
</script>

<template>
  <div @click.stop>
    <!-- Add Task Button -->
    <div
      v-if="!isEditing"
      class="cursor-pointer"
      @click.stop="handleStartEditing"
    >
      <div
        :class="[
          paddingClass,
          'rounded border border-dashed border-muted-foreground/30 hover:border-primary/50 hover:bg-primary/5 transition-colors'
        ]"
      >
        <p class="text-xs text-muted-foreground">
          {{ placeholder }}
        </p>
      </div>
    </div>

    <!-- Editing New Task -->
    <div
      v-else
      :class="[
        paddingClass,
        'rounded border border-dashed border-primary/50 bg-primary/5'
      ]"
      @click.stop
    >
      <form @submit.prevent="handleSubmit" @click.stop>
        <UInput
          v-model="localValue"
          class="new-task-input text-xs bg-transparent border-0 focus:ring-0 focus:ring-offset-0 p-0 h-auto"
          @blur="handleSubmit"
          @keydown.esc="handleCancel"
          @click.stop
        />
      </form>
    </div>
  </div>
</template>

