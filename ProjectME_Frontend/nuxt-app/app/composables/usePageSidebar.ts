import { createSharedComposable } from '@vueuse/core'

const _usePageSidebar = () => {
  const isCollapsed = ref(true)
  const sidebarContent = ref<{ component: string, props?: Record<string, any> } | null>(null)
  const defaultWidth = ref(256) // 64 * 4 = 256px (w-64)

  function setSidebarContent(component: string, props?: Record<string, any>) {
    sidebarContent.value = { component, props }
    isCollapsed.value = true
  }

  function clearSidebarContent() {
    sidebarContent.value = null
  }

  function toggleCollapse() {
    isCollapsed.value = !isCollapsed.value
  }

  function collapse() {
    isCollapsed.value = false
  }

  function expand() {
    isCollapsed.value = true
  }

  return {
    isCollapsed,
    sidebarContent,
    defaultWidth,
    setSidebarContent,
    clearSidebarContent,
    toggleCollapse,
    collapse,
    expand
  }
}

export const usePageSidebar = createSharedComposable(_usePageSidebar)

