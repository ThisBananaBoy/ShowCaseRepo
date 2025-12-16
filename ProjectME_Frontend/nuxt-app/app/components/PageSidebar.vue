<script setup lang="ts">
import RecurringTasksSidebarContent from './tasks/RecurringTasksSidebarContent.vue'

const { isCollapsed, sidebarContent, defaultWidth, toggleCollapse, expand } = usePageSidebar()

const sidebarWidth = computed(() => isCollapsed.value ? defaultWidth.value : 0)

// Ensure sidebar is expanded when content is set
watch(sidebarContent, (newContent) => {
  if (newContent && !isCollapsed.value) {
    expand()
  }
}, { immediate: true })
</script>

<template>
  <ClientOnly>
    <div
      class="flex-shrink-0 border-r border-default bg-elevated/25 overflow-hidden transition-all duration-200"
      :style="{ width: `${sidebarWidth}px` }"
    >
      <div class="h-full flex flex-col">
        <!-- Header with collapse button -->
        <div class="flex items-center justify-between p-3 border-b border-default">
          <h3 class="text-sm font-semibold text-muted-foreground">
            <span v-if="isCollapsed && sidebarContent">Sidebar</span>
          </h3>
          <UButton
            v-if="sidebarContent"
            :icon="isCollapsed ? 'i-lucide-panel-left-close' : 'i-lucide-panel-left-open'"
            size="xs"
            color="neutral"
            variant="ghost"
            @click="toggleCollapse"
          />
        </div>

        <!-- Content -->
        <div v-if="isCollapsed && sidebarContent" class="flex-1 overflow-y-auto">
          <WorkSidebarContent
            v-if="sidebarContent.component === 'WorkSidebarContent'"
            v-bind="sidebarContent.props || {}"
          />
          <TodaySidebarContent
            v-else-if="sidebarContent.component === 'TodaySidebarContent'"
            v-bind="sidebarContent.props || {}"
          />
          <TasksSidebarContent
            v-else-if="sidebarContent.component === 'TasksSidebarContent'"
            v-bind="sidebarContent.props || {}"
          />
          <RecurringTasksSidebarContent
            v-else-if="sidebarContent.component === 'RecurringTasksSidebarContent'"
            v-bind="sidebarContent.props || {}"
          />
        </div>
      </div>
    </div>
    <template #fallback>
      <div class="flex-shrink-0 border-r border-default bg-elevated/25 overflow-hidden transition-all duration-200" style="width: 0px" />
    </template>
  </ClientOnly>
</template>

<style scoped>
.sidebar-enter-active,
.sidebar-leave-active {
  transition: width 0.2s ease;
}

.sidebar-enter-from,
.sidebar-leave-to {
  width: 0;
}
</style>

