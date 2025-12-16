import { createSharedComposable } from '@vueuse/core'

const _useDashboardShortcuts = () => {
  const route = useRoute()
  const router = useRouter()
  const isNotificationsSlideoverOpen = ref(false)
  const { toggleSlideover } = useSlideover()

  defineShortcuts({
    'g-h': () => router.push('/'),
    'g-a': () => router.push('/tasks'),
    'g-p': () => router.push('/projects'),
    'g-t': () => router.push('/today'),
    'g-f': () => router.push('/work'),
    'g-r': () => router.push('/routines'),
    'g-s': () => router.push('/stats'),
    't': () => toggleSlideover('timeline')
  })

  watch(() => route.fullPath, () => {
    isNotificationsSlideoverOpen.value = false
  })

  return {
    isNotificationsSlideoverOpen
  }
}

export const useDashboard = createSharedComposable(_useDashboardShortcuts)
