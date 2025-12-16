<script setup lang="ts">
// Sidebar wird automatisch vom Layout gehandhabt
</script>

<template>
  <UDashboardPanel id="stats">
    <template #header>
      <UDashboardNavbar title="Statistik">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex items-center justify-center h-full text-muted-foreground">
        <div class="text-center">
          <UIcon name="i-lucide-chart-bar" class="size-24 mx-auto mb-4 opacity-50" />
          <p class="text-lg">
            Statistik – Coming Soon
          </p>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
