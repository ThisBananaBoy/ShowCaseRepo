<script setup lang="ts">
// Sidebar wird automatisch vom Layout gehandhabt
</script>

<template>
  <UDashboardPanel id="routines">
    <template #header>
      <UDashboardNavbar title="Routinen">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex items-center justify-center h-full text-muted-foreground">
        <div class="text-center">
          <UIcon name="i-lucide-repeat" class="size-24 mx-auto mb-4 opacity-50" />
          <p class="text-lg">
            Routinen – Coming Soon
          </p>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
