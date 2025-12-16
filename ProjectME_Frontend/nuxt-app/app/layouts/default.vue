<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import ContextTimeline from '~/components/context/ContextTimeline.vue'
import ContextSidebar from '~/components/context/ContextSidebar.vue'
import ContextAddProjekt from '~/components/context/ContextAddProjekt.vue'
import ContextSlideoverContainer from '~/components/context/ContextSlideoverContainer.vue'

const route = useRoute()
const toast = useToast()
const { collapse, clearSidebarContent } = usePageSidebar()

const open = ref(false)

// Pages that should have sidebar content
const pagesWithSidebar = ['/work', '/today', '/tasks']

// Pages that should have collapsed sidebar (no content)
const pagesWithoutSidebar = ['/', '/projects', '/stats', '/routines']

// Handle sidebar state based on route
watch(() => route.path, (newPath) => {
  // Check if it's a project detail page
  const isProjectDetail = /^\/projects\/[^/]+$/.test(newPath)

  if (isProjectDetail || pagesWithoutSidebar.some(path => newPath === path || newPath.startsWith(path + '/'))) {
    // Collapse sidebar for pages without sidebar
    collapse()
    clearSidebarContent()
  } else if (pagesWithSidebar.includes(newPath)) {
    // Pages with sidebar will set their own content via setSidebarContent
    // We just ensure it's not collapsed initially
  }
}, { immediate: true })

const links = [[{
  label: 'Dashboard',
  icon: 'i-lucide-layout-dashboard',
  to: '/',
  onSelect: () => {
    open.value = false
  }
}, {
  label: 'Aufgaben',
  icon: 'i-lucide-calendar-days',
  to: '/tasks',
  onSelect: () => {
    open.value = false
  }
}, {
  label: 'Projekte',
  icon: 'i-lucide-folder-kanban',
  to: '/projects',
  onSelect: () => {
    open.value = false
  }
}, {
  label: 'Heute',
  icon: 'i-lucide-check-square',
  to: '/today',
  onSelect: () => {
    open.value = false
  }
}, {
  label: 'Arbeitsplatz',
  icon: 'i-lucide-briefcase',
  to: '/work',
  onSelect: () => {
    open.value = false
  }
},
// {
//   label: 'Routinen',
//   icon: 'i-lucide-repeat',
//   to: '/routines',
//   onSelect: () => {
//     open.value = false
//   }
// }, {
//   label: 'Statistik',
//   icon: 'i-lucide-chart-bar',
//   to: '/stats',
//   onSelect: () => {
//     open.value = false
//   }
// }

]] satisfies NavigationMenuItem[][]

const groups = computed(() => [{
  id: 'links',
  label: 'Go to',
  items: links.flat()
}, {
  id: 'code',
  label: 'Code',
  items: [{
    id: 'source',
    label: 'View page source',
    icon: 'i-simple-icons-github',
    to: `https://github.com/nuxt-ui-templates/dashboard/blob/main/app/pages${route.path === '/' ? '/index' : route.path}.vue`,
    target: '_blank'
  }]
}])

const { registerSlideover, openSlideover, toggleSlideover } = useSlideover()

// Keyboard shortcuts
defineShortcuts({
  'i': () => {
    toggleSlideover('tools')
  },
  'p': () => {
    toggleSlideover('add-project')
  }
})

onMounted(async () => {
  // Register timeline slideover
  registerSlideover({
    name: 'timeline',
    component: ContextTimeline,
    title: 'Zeitplan',
    description: 'Zeitplan mit Terminen und Aufgaben',
    side: 'right',
    width: '800px' // Breite für Timeline (384px) + Tasks Sidebar (320px) + Padding
  })

  // Register tools slideover
  registerSlideover({
    name: 'tools',
    component: ContextSidebar,
    title: 'Tools',
    description: 'Timer und Tools',
    side: 'right',
    width: '384px' // w-96 = 384px
  })

  // Register add project slideover
  registerSlideover({
    name: 'add-project',
    component: ContextAddProjekt,
    title: 'Neues Projekt',
    description: 'Projekt erstellen',
    side: 'right',
    width: '1000px'
  })

  // Open timeline slideover by default
  openSlideover('timeline')

  const cookie = useCookie('cookie-consent')
  if (cookie.value === 'accepted') {
    return
  }

  toast.add({
    title: 'We use first-party cookies to enhance your experience on our website.',
    duration: 0,
    close: false,
    actions: [{
      label: 'Accept',
      color: 'neutral',
      variant: 'outline',
      onClick: () => {
        cookie.value = 'accepted'
      }
    }, {
      label: 'Opt out',
      color: 'neutral',
      variant: 'ghost'
    }]
  })
})
</script>

<template>
  <UDashboardGroup unit="rem">
    <UDashboardSidebar
      id="default"
      v-model:open="open"
      collapsible
      resizable
      class="bg-elevated/25"
      :ui="{ footer: 'lg:border-t lg:border-default' }"
    >
      <template #header="{ collapsed }">
        <TeamsMenu :collapsed="collapsed" />
      </template>

      <template #default="{ collapsed }">
        <UDashboardSearchButton :collapsed="collapsed" class="bg-transparent ring-default" />

        <UNavigationMenu
          :collapsed="collapsed"
          :items="links[0]"
          orientation="vertical"
          tooltip
          popover
        />
      </template>

      <template #footer="{ collapsed }">
        <UserMenu :collapsed="collapsed" />
      </template>
    </UDashboardSidebar>

    <UDashboardSearch :groups="groups" />

    <div class="flex h-full w-full overflow-hidden">
      <PageSidebar />
      <div class="flex-1 min-w-0 h-full w-full overflow-hidden">
        <slot />
      </div>
    </div>

    <ContextSlideoverContainer />
  </UDashboardGroup>
</template>
