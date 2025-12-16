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
import { isTaskCompleted, getTaskWeek } from '~/utils/taskHelpers'
import { useTaskStore } from '~/stores/useTaskStore'

const props = defineProps<{
  projectTasks: Task[];
  tasks: Task[];
  projectId?: string;
}>();

const router = useRouter();
const route = useRoute();

// Get projectId from route if not provided as prop
const projectId = computed(() => props.projectId || (route.params.id as string));

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
  props.projectTasks.forEach((task) => {
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

const taskStore = useTaskStore()

function moveTaskToWeek(taskId: string, weekStart: Date) {
  const monday = startOfWeek(weekStart, { weekStartsOn: 1 });
  void taskStore.updateTask(taskId, {
    due_date: monday
  });
}

function handleDrop(event: DragEvent, targetWeek?: Date, isCompleteZone = false) {
  event.preventDefault();
  if (!draggedTask.value) return;

  const task = props.projectTasks.find((t: Task) => t.id === draggedTask.value);

  if (isCompleteZone) {
    // Aufgabe als fertig markieren
    if (task) {
      void taskStore.updateTask(task.id, {
        status: "completed",
        completed_at: new Date()
      });
    }
  } else {
    // Normale Verschiebung
    if (targetWeek) {
      moveTaskToWeek(draggedTask.value, targetWeek);
    }
    // Wenn Aufgabe wieder aktiviert wird
    if (task && isTaskCompleted(task)) {
      void taskStore.updateTask(task.id, {
        status: "todo",
        completed_at: undefined
      });
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

function selectProject(projectId: string, week?: Date) {
  selectedProjectForTask.value = projectId;
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
    project_id: projectId.value,
  };

  if (editingWeek.value) {
    newTask.due_date = startOfWeek(editingWeek.value, { weekStartsOn: 1 });
  }

  void taskStore.createTask(newTask);
  cancelNewTask();
}

function navigateToWeek(weekStart: Date) {
  const weekStartFormatted = format(weekStart, "yyyy-MM-dd");
  const routePath = `/projects/${projectId.value}/tasks/${weekStartFormatted}`;
  router.push(routePath);
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
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
  }
});
</script>

<template>
  <div class="h-full overflow-hidden w-full flex flex-col">
    <div class="p-6 flex-1 flex flex-col min-h-0 overflow-hidden">
      <!-- Week Cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 flex-1 min-h-0 overflow-hidden">
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
