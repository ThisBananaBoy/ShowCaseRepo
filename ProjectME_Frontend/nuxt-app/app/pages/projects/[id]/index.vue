<script setup lang="ts">
import ProjectTasksTab from '~/components/projects/ProjectTasksTab.vue';
import ProjectTimelineTab from '~/components/projects/ProjectTimelineTab.vue';
import ProjectDashboardTab from '~/components/projects/ProjectDashboardTab.vue';
import ProjectMilestonesTab from '~/components/projects/ProjectMilestonesTab.vue';
import ProjectReviewTab from '~/components/projects/ProjectReviewTab.vue';
import { useProjectStore } from '~/stores/useProjectStore';
import { useTaskStore } from '~/stores/useTaskStore';
import { storeToRefs } from 'pinia';
import { isTaskCompleted } from '~/utils/taskHelpers'

const route = useRoute();
const router = useRouter();
const projectId = route.params.id as string;

const projectStore = useProjectStore();
const taskStore = useTaskStore();
const { projects } = storeToRefs(projectStore);
const { tasks } = storeToRefs(taskStore);

const project = computed(() => projectStore.getProjectById(projectId));
const projectTasks = computed(() => taskStore.getTasksByProject(projectId));
const projectMilestones = computed(() => project.value?.milestones || []);
const projectDeadlines = computed(() =>
  project.value?.deadlines?.slice().sort((a, b) => a.sequence - b.sequence) || []
);

const getProjectProgress = (id: string) => {
  const related = taskStore.getTasksByProject(id);
  if (!related.length) return 0;
  const completed = related.filter(t => isTaskCompleted(t)).length;
  return Math.round((completed / related.length) * 100);
};

const selectedTab = ref("dashboard");

const tabs = computed(() => [
  {
    label: "Dashboard",
    value: "dashboard",
    icon: "i-lucide-layout-dashboard",
  },
  {
    label: "Aufgaben",
    value: "tasks",
    icon: "i-lucide-check-square",
    badge: projectTasks.value.filter((t) => !isTaskCompleted(t)).length,
  },
  {
    label: "Meilensteine",
    value: "milestones",
    icon: "i-lucide-milestone",
    badge: projectMilestones.value.length,
  },
  // {
  //   label: "Timeline",
  //   value: "timeline",
  //   icon: "i-lucide-gantt-chart",
  // },
  // {
  //   label: "Review",
  //   value: "review",
  //   icon: "i-lucide-file-text",
  // },
]);


// Redirect wenn Projekt nicht existiert
watchEffect(() => {
  if (!project.value) {
    router.push("/projects");
  }
});

onMounted(() => {
  if (!projects.value.length) projectStore.fetchProjects();
  if (!tasks.value.length) taskStore.fetchTasks();
});
</script>

<template>
  <UDashboardPanel v-if="project" :id="`project-${projectId}`" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar :title="project.name">
        <template #leading>
          <UButton
            icon="i-lucide-arrow-left"
            color="neutral"
            variant="ghost"
            square
            @click="$router.push('/projects')"
          />
        </template>

        <template #right>
          <UButton
            icon="i-lucide-more-vertical"
            color="neutral"
            variant="ghost"
            square
          />
        </template>
      </UDashboardNavbar>

      <UDashboardToolbar>
        <template #left>
          <UTabs
            v-model="selectedTab"
            :items="tabs"
            size="md"
            class="pt-2"
          />
        </template>
      </UDashboardToolbar>
    </template>

    <template #body>
      <div
        class="flex-1 w-full min-h-0"
        :class="selectedTab === 'tasks' ? 'overflow-hidden' : 'overflow-y-auto'"
      >
        <!-- Dashboard Tab -->
        <ProjectDashboardTab
          v-if="selectedTab === 'dashboard'"
          :project="project"
          :project-tasks="projectTasks"
          :project-milestones="projectMilestones"
          :project-deadlines="projectDeadlines"
          :get-project-progress="getProjectProgress"
        />

        <!-- Aufgaben Tab -->
        <ProjectTasksTab
          v-else-if="selectedTab === 'tasks'"
          :project-tasks="projectTasks"
          :tasks="tasks"
          :project-id="projectId"
        />

        <!-- Meilensteine Tab -->
        <ProjectMilestonesTab
          v-else-if="selectedTab === 'milestones'"
          :project-milestones="projectMilestones"
        />

        <ProjectTimelineTab
          v-else-if="selectedTab === 'timeline'"
          :project="project"
          :project-tasks="projectTasks"
          :project-milestones="projectMilestones"
          :tasks="tasks"
        />

        <!-- Review Tab -->
        <ProjectReviewTab v-else-if="selectedTab === 'review'" />
      </div>
    </template>
  </UDashboardPanel>
</template>

<style scoped>
.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.3s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>

