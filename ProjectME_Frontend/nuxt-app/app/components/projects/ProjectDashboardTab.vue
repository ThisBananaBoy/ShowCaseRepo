<script setup lang="ts">
import type { Project, Deadline, Milestone } from '~/types/Project';
import type { Task } from '~/types/Task';
import { isTaskCompleted } from '~/utils/taskHelpers'

const props = defineProps<{
  project: Project;
  projectTasks: Task[];
  projectMilestones: Milestone[];
  projectDeadlines: Deadline[];
  getProjectProgress: (projectId: string) => number;
}>();

const progressMode = ref<"days" | "tasks">("days");

const daysProgress = computed(() => {
  if (!props.project?.start_date || !props.project?.last_deadline_date) return 0;
  const start = new Date(props.project.start_date).getTime();
  const end = new Date(props.project.last_deadline_date).getTime();
  const now = Date.now();
  const total = end - start;
  const elapsed = now - start;
  return Math.min(Math.max(Math.round((elapsed / total) * 100), 0), 100);
});

const tasksProgress = computed(() => props.getProjectProgress(props.project.id));

const currentProgress = computed(() =>
  progressMode.value === "days" ? daysProgress.value : tasksProgress.value
);

function formatDate(date?: Date) {
  if (!date) return "-";
  return new Date(date).toLocaleDateString("de-DE", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function getDaysUntilDeadline() {
  if (!props.project?.last_deadline_date) return null;
  const now = new Date();
  const deadline = new Date(props.project.last_deadline_date);
  const diff = Math.ceil(
    (deadline.getTime() - now.getTime()) / (1000 * 60 * 60 * 24)
  );
  return diff;
}

const daysLeft = computed(() => getDaysUntilDeadline());
</script>

<template>
  <div class="p-6 space-y-6 w-full">
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Fortschritt Card -->
      <UCard>
        <template #header>
          <div class="flex items-center justify-between">
            <h3 class="font-semibold">Fortschritt</h3>
            <UButton
              :label="progressMode === 'days' ? 'Tage' : 'Tasks'"
              size="xs"
              color="neutral"
              variant="ghost"
              @click="
                progressMode = progressMode === 'days' ? 'tasks' : 'days'
              "
            />
          </div>
        </template>

        <div class="flex items-center justify-center py-6">
          <div class="relative inline-flex items-center justify-center">
            <svg class="size-32 -rotate-90">
              <circle
                cx="64"
                cy="64"
                r="56"
                stroke="currentColor"
                stroke-width="8"
                fill="none"
                class="text-muted"
              />
              <circle
                cx="64"
                cy="64"
                r="56"
                stroke="currentColor"
                stroke-width="8"
                fill="none"
                class="text-primary"
                :stroke-dasharray="`${2 * Math.PI * 56}`"
                :stroke-dashoffset="`${
                  2 * Math.PI * 56 * (1 - currentProgress / 100)
                }`"
                stroke-linecap="round"
              />
            </svg>
            <div class="absolute inset-0 flex items-center justify-center">
              <div class="text-3xl font-bold">{{ currentProgress }}%</div>
            </div>
          </div>
        </div>

        <div class="space-y-2 text-sm">
          <div class="flex justify-between">
            <span class="text-muted-foreground">Nach Tagen:</span>
            <span class="font-medium">{{ daysProgress }}%</span>
          </div>
          <div class="flex justify-between">
            <span class="text-muted-foreground">Nach Tasks:</span>
            <span class="font-medium">{{ tasksProgress }}%</span>
          </div>
        </div>
      </UCard>

      <!-- Status Card -->
      <UCard>
        <template #header>
          <h3 class="font-semibold">Status</h3>
        </template>

        <div class="space-y-4">
          <div>
            <div class="text-sm text-muted-foreground mb-1">Status</div>
            <UBadge
              :label="project.status"
              :color="project.status === 'active' ? 'success' : 'warning'"
            />
          </div>

          <div>
            <div class="text-sm text-muted-foreground mb-1">Start</div>
            <div class="font-medium">
              {{ formatDate(project.start_date) }}
            </div>
          </div>

          <div>
            <div class="text-sm text-muted-foreground mb-1">Deadline</div>
            <div class="font-medium">
              {{ formatDate(project.last_deadline_date) }}
            </div>
          </div>

          <div v-if="daysLeft !== null">
            <div class="text-sm text-muted-foreground mb-1">
              Verbleibend
            </div>
            <div
              class="font-medium"
              :class="daysLeft < 7 ? 'text-red-500' : ''"
            >
              {{ daysLeft }} Tage
            </div>
          </div>
        </div>
      </UCard>

      <!-- Übersicht Card -->
      <UCard>
        <template #header>
          <h3 class="font-semibold">Übersicht</h3>
        </template>

        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <span class="text-sm text-muted-foreground">
              Aufgaben gesamt
            </span>
            <span class="font-semibold text-lg">{{
              projectTasks.length
            }}</span>
          </div>

          <div class="flex items-center justify-between">
            <span class="text-sm text-muted-foreground">Erledigt</span>
            <span class="font-semibold text-lg text-green-500">
              {{ projectTasks.filter((t) => isTaskCompleted(t)).length }}
            </span>
          </div>

          <div class="flex items-center justify-between">
            <span class="text-sm text-muted-foreground">Offen</span>
            <span class="font-semibold text-lg">
              {{ projectTasks.filter((t) => !isTaskCompleted(t)).length }}
            </span>
          </div>

          <div class="flex items-center justify-between">
            <span class="text-sm text-muted-foreground">Meilensteine</span>
            <span class="font-semibold text-lg">{{
              projectMilestones.length
            }}</span>
          </div>
        </div>
      </UCard>
    </div>

    <!-- Deadline Historie -->
    <UCard v-if="projectDeadlines.length > 0">
      <template #header>
        <h3 class="font-semibold">Deadline-Historie</h3>
      </template>

      <div class="relative">
        <div class="absolute left-4 top-0 bottom-0 w-0.5 bg-muted" />

        <div class="space-y-4">
          <div
            v-for="(deadline, index) in projectDeadlines"
            :key="deadline.id"
            class="relative pl-10"
          >
            <div
              class="absolute left-2.5 size-3 rounded-full border-2 border-background"
              :class="
                index === projectDeadlines.length - 1
                  ? 'bg-primary'
                  : 'bg-muted-foreground'
              "
            />

            <div class="pb-4">
              <div class="flex items-center gap-2 mb-1">
                <span class="font-medium">{{
                  formatDate(deadline.deadline_date)
                }}</span>
                <UBadge
                  v-if="index === projectDeadlines.length - 1"
                  label="Aktuell"
                  size="xs"
                  color="primary"
                  variant="subtle"
                />
              </div>

              <div class="text-sm text-muted-foreground">
                {{ deadline.reason || "Initiale Deadline" }}
              </div>

              <div class="text-xs text-muted-foreground mt-1">
                {{ formatDate(deadline.created_at) }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </UCard>

    <!-- Beschreibung -->
    <UCard>
      <template #header>
        <h3 class="font-semibold">Beschreibung</h3>
      </template>
      <p class="text-muted-foreground">
        {{ project.description }}
      </p>
    </UCard>
  </div>
</template>
