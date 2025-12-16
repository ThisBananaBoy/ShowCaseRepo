<script setup lang="ts">
import { nextTick, onMounted, onUnmounted } from "vue";
import {
  getWeek,
  startOfWeek,
  format,
  addWeeks,
} from "date-fns";
import { de } from "date-fns/locale";
import type { Task, CreateTaskDto } from "~/types/Task";
import WeekCard from "~/components/tasks/WeekCard.vue";
import AddTaskButton from "~/components/tasks/AddTaskButton.vue";
import { useProjectStore } from '~/stores/useProjectStore';
import { useTaskStore } from '~/stores/useTaskStore';
import { isTaskCompleted, getTaskWeek } from '~/utils/taskHelpers'
import { storeToRefs } from 'pinia';

const route = useRoute();
const router = useRouter();
//const { setSidebarContent } = usePageSidebar();

// Parse route parameters
const projectIdParam = route.params.id;
const projectIdValue = Array.isArray(projectIdParam) ? projectIdParam[0]! : (projectIdParam as string);

const projectId = ref(projectIdValue);

// Watch for route changes
watch(() => route.params.id, (newId) => {
  const id = Array.isArray(newId) ? newId[0]! : (newId as string);
  projectId.value = id;
});

const projectStore = useProjectStore();
const taskStore = useTaskStore();
const { tasks } = storeToRefs(taskStore);
const { projects } = storeToRefs(projectStore);

// Get project tasks only
const projectTasks = computed(() => {
  return tasks.value.filter((task) => task.project_id === projectId.value);
});

// Get project info
const project = computed(() => {
  return projects.value.find((p) => p.id === projectId.value);
});

// Projekt-Farben (für visuelle Unterscheidung)
const projectColors = [
  "bg-blue-500",
  "bg-purple-500",
  "bg-pink-500",
  "bg-orange-500",
  "bg-teal-500",
  "bg-indigo-500",
  "bg-rose-500",
  "bg-amber-500",
];

const projectBorderColors = [
  "border-blue-500/30",
  "border-purple-500/30",
  "border-pink-500/30",
  "border-orange-500/30",
  "border-teal-500/30",
  "border-indigo-500/30",
  "border-rose-500/30",
  "border-amber-500/30",
];

function getProjectColor(taskProjectId: string): string {
  const projectIndex = projects.value.findIndex((p) => p.id === taskProjectId);
  if (projectIndex === -1) return "bg-neutral";
  return projectColors[projectIndex % projectColors.length]!;
}

function getProjectBorderColor(taskProjectId: string): string {
  const projectIndex = projects.value.findIndex((p) => p.id === taskProjectId);
  if (projectIndex === -1) return "border-muted-foreground/20";
  return projectBorderColors[projectIndex % projectBorderColors.length]!;
}

const draggedTask = ref<string | null>(null);

const weeks = computed(() => {
  const today = new Date();
  const current = startOfWeek(today, { weekStartsOn: 1 });
  const weeksList: Date[] = [];

  weeksList.push(current);
  for (let i = 1; i <= 2; i++) {
    weeksList.push(addWeeks(current, i));
  }

  return weeksList;
});

const weekCards = computed(() => {
  return weeks.value;
});

const tasksByWeek = computed(() => {
  const grouped: Record<number, Task[]> = {};
  projectTasks.value.forEach((task) => {
    const weekNum = getTaskWeek(task);

    if (weekNum !== undefined) {
      if (!grouped[weekNum]) {
        grouped[weekNum] = [];
      }
      grouped[weekNum]!.push(task);
    }
  });
  return grouped;
});

function getWeekNumber(date: Date) {
  return getWeek(date, { locale: de, weekStartsOn: 1 });
}

function getTasksForWeek(weekStart: Date) {
  const weekNum = getWeekNumber(weekStart);
  const weekTasks = tasksByWeek.value[weekNum] || [];
  return weekTasks.filter((task) => !isTaskCompleted(task));
}

function getCompletedTasksForWeek(weekStart: Date) {
  const weekNum = getWeekNumber(weekStart);
  const weekTasks = tasksByWeek.value[weekNum] || [];
  return weekTasks.filter((task) => isTaskCompleted(task));
}

function moveTaskToWeek(taskId: string, weekStart: Date) {
  const monday = startOfWeek(weekStart, { weekStartsOn: 1 });
  void taskStore.updateTask(taskId, {
    due_date: monday,
  });
}

function handleDrop(event: DragEvent, targetWeek?: Date, isCompleteZone = false) {
  event.preventDefault();
  if (!draggedTask.value) return;

  const task = projectTasks.value.find((t: Task) => t.id === draggedTask.value);

  if (isCompleteZone) {
    if (task) {
      void taskStore.updateTask(task.id, { status: "completed", completed_at: new Date() });
    }
  } else {
    if (targetWeek) {
      moveTaskToWeek(draggedTask.value, targetWeek);
    }
    if (task && isTaskCompleted(task)) {
      void taskStore.updateTask(task.id, { status: "todo", completed_at: undefined });
    }
  }

  draggedTask.value = null;
}

function handleDragStart(event: DragEvent, taskId: string) {
  if (!taskId) return;
  draggedTask.value = taskId;
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = "move";
  }
}

function handleDragOver(event: DragEvent) {
  event.preventDefault();
}

const hoveredWeek = ref<Date | null>(null);
const editingNewTask = ref(false);
const editingWeek = ref<Date | null>(null);
const newTaskName = ref("");
const hoverTimeout = ref<number | null>(null);
const showProjectMenu = ref(false);
const projectMenuPosition = ref({ x: 0, y: 0 });
const selectedProjectForTask = ref<string | null>(null);
const hoveredCompleteZone = ref<{ type: 'week'; date: Date } | null>(null);
const showCompletedPanel = ref<{ type: 'week'; date: Date } | null>(null);
const draggedOverCompleteZone = ref(false);

function handleWeekMouseEnter(weekStart: Date) {
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
  }
  hoverTimeout.value = setTimeout(() => {
    hoveredWeek.value = weekStart;
  }, 300);
}

function handleWeekMouseLeave() {
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
    hoverTimeout.value = null;
  }
  if (!editingNewTask.value && !showProjectMenu.value) {
    hoveredWeek.value = null;
  }
}

function showProjectSelection(event?: MouseEvent, week?: Date) {
  if (event) {
    event.stopPropagation();
    projectMenuPosition.value = { x: event.clientX, y: event.clientY };
  }
  showProjectMenu.value = true;
  if (week) {
    editingWeek.value = week;
  }
}

function selectProject(taskProjectId: string, week?: Date) {
  selectedProjectForTask.value = taskProjectId;
  showProjectMenu.value = false;
  editingNewTask.value = true;
  editingWeek.value = week || null;
  newTaskName.value = "";
  nextTick(() => {
    const input = document.querySelector(
      ".new-task-input"
    ) as HTMLInputElement;
    input?.focus();
  });
}

function cancelNewTask() {
  editingNewTask.value = false;
  editingWeek.value = null;
  newTaskName.value = "";
  selectedProjectForTask.value = null;
  showProjectMenu.value = false;
}

function createTask() {
  if (!newTaskName.value.trim()) {
    cancelNewTask();
    return;
  }

  const newTask: CreateTaskDto = {
    name: newTaskName.value.trim(),
    status: "todo",
    priority: 2,
    project_id: selectedProjectForTask.value || projectId.value,
  };

  if (editingWeek.value) {
    newTask.due_date = startOfWeek(editingWeek.value, { weekStartsOn: 1 });
  }

  void taskStore.createTask(newTask);
  cancelNewTask();
}

function navigateToWeek(weekStart: Date) {
  const weekStartFormatted = format(weekStart, "yyyy-MM-dd");
  router.push(`/projects/${projectId.value}/tasks/${weekStartFormatted}`);
}

function handleCompleteZoneDragOver(event: DragEvent) {
  event.preventDefault();
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = "move";
  }
  draggedOverCompleteZone.value = true;
}

function handleCompleteZoneDragLeave() {
  draggedOverCompleteZone.value = false;
}

function toggleCompletedPanel(date: Date) {
  if (showCompletedPanel.value?.date.getTime() === date.getTime()) {
    showCompletedPanel.value = null;
  } else {
    showCompletedPanel.value = { type: 'week', date };
  }
}

// Click outside handler für Projekt-Menü
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement;
  if (showProjectMenu.value && !target.closest('.project-menu')) {
    showProjectMenu.value = false;
    if (!editingNewTask.value) {
      hoveredWeek.value = null;
    }
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
  if (!projects.value.length) projectStore.fetchProjects();
  if (!tasks.value.length) taskStore.fetchTasks();
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
  }
});
</script>

<template>
  <UDashboardPanel :id="`project-tasks-${projectId}`" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar :title="project?.name ? `${project.name} - Aufgaben` : 'Aufgaben'">
        <template #leading>
          <UButton
            icon="i-lucide-arrow-left"
            color="neutral"
            variant="ghost"
            square
            @click="router.push(`/projects/${projectId}`)"
          />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex-1 overflow-hidden w-full min-h-0 flex flex-col">
        <div class="p-6 flex-1 flex flex-col min-h-0">
          <!-- Week Cards -->
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 flex-1 min-h-0">
            <WeekCard
              v-for="weekStart in weekCards"
              :key="weekStart.getTime()"
              :week-start="weekStart"
              :tasks="getTasksForWeek(weekStart)"
              :completed-tasks="getCompletedTasksForWeek(weekStart)"
              :is-editing="editingNewTask && editingWeek?.getTime() === weekStart.getTime()"
              :is-hovered="hoveredCompleteZone?.type === 'week' && hoveredCompleteZone?.date.getTime() === weekStart.getTime()"
              :is-dragged-over="draggedOverCompleteZone"
              :show-completed-panel="showCompletedPanel?.type === 'week' && showCompletedPanel?.date.getTime() === weekStart.getTime()"
              :get-project-color="getProjectColor"
              :get-project-border-color="getProjectBorderColor"
              @navigate="navigateToWeek"
              @drop="(event, week, isCompleteZone) => handleDrop(event, week, isCompleteZone)"
              @drag-over="handleDragOver"
              @mouse-enter="handleWeekMouseEnter"
              @mouse-leave="handleWeekMouseLeave"
              @task-drag-start="(task, event) => handleDragStart(event, task.id || '')"
              @task-drag-end="() => {}"
              @toggle-completed-panel="toggleCompletedPanel"
              @complete-zone-drag-over="handleCompleteZoneDragOver"
              @complete-zone-drag-leave="handleCompleteZoneDragLeave"
              @complete-zone-mouse-enter="(week) => hoveredCompleteZone = { type: 'week', date: week }"
              @complete-zone-mouse-leave="hoveredCompleteZone = null; draggedOverCompleteZone = false"
            >
              <template #add-task-button>
                <AddTaskButton
                  v-if="!(editingNewTask && editingWeek?.getTime() !== weekStart.getTime()) && !showProjectMenu"
                  v-model="newTaskName"
                  :is-editing="editingNewTask && editingWeek?.getTime() === weekStart.getTime()"
                  @update:is-editing="editingNewTask = $event; if ($event) editingWeek = weekStart; else editingWeek = null"
                  @start-editing="showProjectSelection($event, weekStart)"
                  @submit="createTask"
                  @cancel="cancelNewTask"
                />
              </template>
            </WeekCard>
          </div>
        </div>

        <!-- Project Selection Menu -->
        <Teleport to="body">
          <Transition name="fade">
            <div
              v-if="showProjectMenu"
              class="project-menu fixed z-50 bg-background border rounded-lg shadow-lg p-2 min-w-[200px]"
              :style="{
                left: `${projectMenuPosition.x}px`,
                top: `${projectMenuPosition.y}px`,
                transform: 'translate(-50%, 10px)',
              }"
              @click.stop
            >
              <div class="space-y-1">
                <div
                  class="flex items-center gap-2 px-3 py-2 rounded hover:bg-muted/50 cursor-pointer transition-colors border-t border-default mt-1 pt-1"
                  @click="selectProject(projectId, editingWeek || undefined)"
                >
                  <span class="text-sm text-muted-foreground">Aktuelles Projekt</span>
                </div>
              </div>
            </div>
          </Transition>
        </Teleport>
      </div>
    </template>
  </UDashboardPanel>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>

