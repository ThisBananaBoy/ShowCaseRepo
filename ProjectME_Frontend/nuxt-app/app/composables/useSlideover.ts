import { createSharedComposable } from '@vueuse/core'
import { markRaw } from 'vue'
import type { Component } from 'vue'

export interface SlideoverConfig {
  name: string
  component: Component | string
  props?: Record<string, any>
  title?: string
  description?: string
  side?: 'left' | 'right' | 'top' | 'bottom'
  width?: string
}

const _useSlideover = () => {
  const activeSlideover = ref<string | null>(null)
  const slideoverStates = ref<Record<string, boolean>>({})
  const slideoverConfigs = ref<Record<string, SlideoverConfig>>({})

  function registerSlideover(config: SlideoverConfig) {
    // Mark component as raw to prevent reactivity overhead
    const processedConfig = {
      ...config,
      component: typeof config.component === 'string' 
        ? config.component 
        : markRaw(config.component)
    }
    slideoverConfigs.value[config.name] = processedConfig
  }

  function openSlideover(name: string, config?: SlideoverConfig) {
    if (config) {
      registerSlideover(config)
    }
    activeSlideover.value = name
    slideoverStates.value[name] = true
  }

  function closeSlideover(name: string) {
    slideoverStates.value[name] = false
    // Wait for animation to complete before removing the component
    // USlideover uses 200ms transition, so we wait slightly longer
    if (activeSlideover.value === name) {
      setTimeout(() => {
        if (activeSlideover.value === name && !slideoverStates.value[name]) {
          activeSlideover.value = null
        }
      }, 250)
    }
  }

  function toggleSlideover(name: string, config?: SlideoverConfig) {
    if (slideoverStates.value[name]) {
      closeSlideover(name)
    } else {
      openSlideover(name, config)
    }
  }

  const isSlideoverOpen = (name: string) => {
    return computed(() => slideoverStates.value[name] || false)
  }

  const getSlideoverConfig = (name: string) => {
    return computed(() => slideoverConfigs.value[name] || null)
  }

  return {
    activeSlideover,
    slideoverStates,
    slideoverConfigs,
    registerSlideover,
    openSlideover,
    closeSlideover,
    toggleSlideover,
    isSlideoverOpen,
    getSlideoverConfig
  }
}

export const useSlideover = createSharedComposable(_useSlideover)
