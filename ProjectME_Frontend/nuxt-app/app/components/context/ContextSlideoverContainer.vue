<script setup lang="ts">
import { computed, resolveComponent } from 'vue'
import { useSlideover } from '~/composables/useSlideover'

const { activeSlideover, slideoverStates, slideoverConfigs, closeSlideover } = useSlideover()

const currentConfig = computed(() => {
  if (!activeSlideover.value) return null
  return slideoverConfigs.value[activeSlideover.value] || null
})

const slideoverUI = computed(() => {
  if (!currentConfig.value?.width) return undefined
  // Verwende 'content' um die Breite des Slideover-Content zu setzen
  return {
    content: `w-[${currentConfig.value.width}] max-w-[${currentConfig.value.width}]`
  }
})

const isOpen = computed({
  get: () => {
    if (!activeSlideover.value) return false
    return slideoverStates.value[activeSlideover.value] || false
  },
  set: (value) => {
    if (!activeSlideover.value) return
    if (!value) {
      closeSlideover(activeSlideover.value)
    }
  }
})

const currentComponent = computed(() => {
  if (!currentConfig.value) return null

  const component = currentConfig.value.component
  if (typeof component === 'string') {
    return resolveComponent(component)
  }
  return component
})
</script>

<template>
  <ClientOnly>
    <USlideover
      v-if="currentConfig"
      v-model:open="isOpen"
      :title="currentConfig.title"
      :description="currentConfig.description"
      :side="currentConfig.side || 'right'"
      :transition="true"
      :ui="slideoverUI"
    >
      <template #body>
        <component
          :is="currentComponent"
          v-if="currentComponent"
          v-bind="currentConfig.props || {}"
        />
      </template>
    </USlideover>
    <template #fallback>
      <div />
    </template>
  </ClientOnly>
</template>

