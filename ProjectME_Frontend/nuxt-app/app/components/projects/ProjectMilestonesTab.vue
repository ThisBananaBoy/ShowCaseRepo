<script setup lang="ts">
import type { Milestone } from "~/types/Project";

defineProps<{
  projectMilestones: Milestone[];
}>();

function formatDate(date?: Date) {
  if (!date) return "-";
  return new Date(date).toLocaleDateString("de-DE", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

</script>

<template>
  <div class="p-6 w-full">
    <div class="space-y-3">
      <UCard
        v-for="milestone in projectMilestones"
        :key="milestone.id"
        :ui="{ body: 'p-4' }"
      >
        <div class="flex items-start justify-between">
          <div class="flex-1">
            <h4 class="font-semibold">
              {{ milestone.name }}
            </h4>
            <p class="text-sm text-muted-foreground mt-1">
              {{ milestone.description }}
            </p>
            <div class="flex items-center gap-3 mt-2 text-xs">
              <span class="text-muted-foreground">
                Fällig: {{ formatDate(milestone.last_deadline_date) }}
              </span>
              <UBadge
                v-if="milestone.completed_at"
                label="Abgeschlossen"
                color="success"
                size="xs"
                variant="subtle"
              />
            </div>
          </div>
        </div>
      </UCard>

      <div
        v-if="projectMilestones.length === 0"
        class="text-center py-12 text-muted-foreground"
      >
        Keine Meilensteine
      </div>
    </div>
  </div>
</template>
