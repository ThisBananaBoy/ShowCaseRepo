<script setup lang="ts">
interface Props {
  selectedProject?: string | null
  selectedTool?: string
  tools?: Array<{ id: string, label: string, icon: string }>
  onToolSelect?: (toolId: string) => void
  projectOptions?: Array<{ value: string, label: string }>
  onProjectChange?: (projectId: string | null) => void
}

const props = defineProps<Props>()
</script>

<template>
  <div class="p-4 space-y-4">
    <div class="space-y-2">
      <h3 class="text-sm font-semibold text-muted-foreground mb-4 px-2">
        Tools
      </h3>
      <UButton
        v-for="tool in props.tools"
        :key="tool.id"
        :variant="props.selectedTool === tool.id ? 'solid' : 'ghost'"
        :color="props.selectedTool === tool.id ? 'primary' : 'neutral'"
        class="w-full justify-start"
        :icon="tool.icon"
        @click="props.onToolSelect?.(tool.id)"
      >
        {{ tool.label }}
      </UButton>
    </div>

    <!-- Project Selection -->
    <div class="pt-4 border-t border-default">
      <h3 class="text-sm font-semibold text-muted-foreground mb-3 px-2">
        Projekt zuordnen
      </h3>
      <USelectMenu
        :model-value="props.selectedProject"
        :options="props.projectOptions || []"
        option-attribute="label"
        value-attribute="value"
        placeholder="Projekt auswählen (optional)"
        size="md"
        @update:model-value="(value) => props.onProjectChange?.(value as string | null)"
      />
    </div>
  </div>
</template>

