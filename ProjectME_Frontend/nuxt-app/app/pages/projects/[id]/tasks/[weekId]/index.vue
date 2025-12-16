<script setup lang="ts">
import { nextTick, onMounted, onUnmounted } from "vue";
import {
  getWeek,
  startOfWeek,
  endOfWeek,
  format,
  eachDayOfInterval,
} from "date-fns";
import { de } from "date-fns/locale";
import type { Task } from "~/types/Task";
import type { Appointment } from "~/types/Appointment";
import CalendarTimeTable from "~/components/CalendarTimeTable.vue";
import { useTaskStore } from '~/stores/useTaskStore';
import { useProjectStore } from '~/stores/useProjectStore';
import { useAppointmentStore } from '~/stores/useAppointmentStore';
import { storeToRefs } from 'pinia';
import { isTaskCompleted } from '~/utils/taskHelpers';

const route = useRoute();
const router = useRouter();
const { setSidebarContent } = usePageSidebar();

// Parse route parameters - Nuxt handles nested dynamic routes automatically
// According to Nuxt docs: route.params.id and route.params.weekId should be available
// For nested routes like /projects/[id]/tasks/[weekId], both params are available
function getProjectId(): string {
  const id = route.params.id;
  if (Array.isArray(id)) {
    return id[0] || '';
  }
  return (id as string) || '';
}

function getWeekId(): string {
  const id = route.params.weekId;
  if (Array.isArray(id)) {
    return id[0] || '';
  }
  return (id as string) || '';
}

const projectId = ref(getProjectId());
const weekId = ref(getWeekId());

// Watch for route changes
watch(() => route.params, () => {
  projectId.value = getProjectId();
  weekId.value = getWeekId();
}, { deep: true });

const weekStartDate = computed(() => {
  try {
    const date = new Date(weekId.value);
    return startOfWeek(date, { weekStartsOn: 1 });
  } catch {
    // Fallback to current week if invalid
    return startOfWeek(new Date(), { weekStartsOn: 1 });
  }
});

const taskStore = useTaskStore();
const projectStore = useProjectStore();
const appointmentStore = useAppointmentStore();
const { tasks } = storeToRefs(taskStore);
const { projects } = storeToRefs(projectStore);
const { appointments } = storeToRefs(appointmentStore);

// Get project tasks only
const projectTasks = computed(() => {
  const pid = projectId.value;
  if (!pid) return [];
  return tasks.value.filter((task) => task.project_id === pid);
});

// Aktive Projekte
const activeProjects = computed(() =>
  projects.value.filter((p) => p.status === "active")
);

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

function getProjectColor(projectId: string): string {
  const projectIndex = activeProjects.value.findIndex((p) => p.id === projectId);
  if (projectIndex === -1) return "bg-neutral";
  return projectColors[projectIndex % projectColors.length]!;
}

function getProjectBorderColor(projectId: string): string {
  const projectIndex = activeProjects.value.findIndex((p) => p.id === projectId);
  if (projectIndex === -1) return "border-muted-foreground/20";
  return projectBorderColors[projectIndex % projectBorderColors.length]!;
}

const days = computed(() => {
  const start = weekStartDate.value;
  const end = endOfWeek(start, { weekStartsOn: 1 });
  return eachDayOfInterval({ start, end });
});

// tasksByDay wird jetzt in CalendarTimeTable berechnet

function getWeekNumber(date: Date) {
  return getWeek(date, { locale: de, weekStartsOn: 1 });
}

// Diese Funktionen sind jetzt in CalendarTimeTable

const applyTaskUpdate = async (taskId: string, partial: Partial<Task>) => {
  const existing = tasks.value.find(t => t.id === taskId);
  if (!existing) return;
  const { id, ...updateData } = { ...existing, ...partial };
  await taskStore.updateTask(taskId, updateData);
};

// Handler für externe Task-Drops (z.B. von Sidebar)
const handleExternalTaskDrop = async (taskId: string, day: Date, isCompleteZone = false) => {
  const task = tasks.value.find(t => t.id === taskId);
  if (!task) return;

  if (isCompleteZone) {
    await applyTaskUpdate(taskId, { status: 'completed', completed_at: new Date() });
  } else {
    await applyTaskUpdate(taskId, {
      due_date: day,
      status: 'todo',
      completed_at: undefined
    });
  }
};

// moveTaskToDay wird jetzt in CalendarTimeTable gehandhabt

const draggedTask = ref<string | null>(null);

// Diese Handler sind jetzt in CalendarTimeTable

// Timeline Resize und Drag Handler sind jetzt in CalendarTimeTable

// Diese Funktionen sind jetzt in CalendarTimeTable - hier nur für Kompatibilität
function handleTimelineResize(_task: Task, _edge: 'top' | 'bottom', _event: MouseEvent) {
  // Wird jetzt in CalendarTimeTable gehandhabt
}

function handleTimelineDragTimedItem(_event: MouseEvent, _task?: Task, _appointment?: Appointment) {
  // Wird jetzt in CalendarTimeTable gehandhabt
}

// Cleanup wird jetzt in CalendarTimeTable gehandhabt

const hoveredDay = ref<Date | null>(null);
const editingNewTask = ref(false);
const editingDay = ref<Date | null>(null);
const newTaskName = ref("");
const showAddTask = ref(false);
const hoverTimeout = ref<number | null>(null);
const showProjectMenu = ref(false);
const projectMenuPosition = ref({ x: 0, y: 0 });
const selectedProjectForTask = ref<string | null>(null);
// draggedOverTimeline wird jetzt in CalendarTimeTable gehandhabt

function selectProject(taskProjectId: string, day?: Date) {
  selectedProjectForTask.value = taskProjectId;
  showProjectMenu.value = false;
  editingNewTask.value = true;
  editingDay.value = day || null;
  newTaskName.value = "";
  nextTick(() => {
    const input = document.querySelector(
      ".new-task-input"
    ) as HTMLInputElement;
    input?.focus();
  });
}

// Tasks für Sidebar - verfügbare Tasks (ohne due_date oder nicht zugewiesen)
const availableTasksForSidebar = computed(() => {
  return projectTasks.value.filter((task) => {
    // Nur nicht-fertige Tasks
    if (isTaskCompleted(task)) return false;
    // Tasks ohne due_date
    return !task.due_date;
  });
});

// Tasks nach Projekten gruppieren für Sidebar
const tasksByProjectForSidebar = computed(() => {
  const grouped: Record<string, Task[]> = {};
  const noProjectTasks: Task[] = [];

  availableTasksForSidebar.value.forEach((task) => {
    if (task.project_id) {
      if (!grouped[task.project_id]) {
        grouped[task.project_id] = [];
      }
      grouped[task.project_id]!.push(task);
    } else {
      noProjectTasks.push(task);
    }
  });

  return { grouped, noProjectTasks };
});

// Set sidebar content
watch([availableTasksForSidebar, tasksByProjectForSidebar, activeProjects], () => {
  setSidebarContent('TasksSidebarContent', {
    availableTasks: availableTasksForSidebar.value,
    tasksByProject: tasksByProjectForSidebar.value,
    activeProjects: activeProjects.value,
    getProjectColor,
    getProjectBorderColor,
    onDragStart: (_task: Task) => {
      // Task-ID wird in TasksSidebarContent über dataTransfer gesetzt
    },
    onDragEnd: () => { draggedTask.value = null; },
    onDropToSidebar: async () => {
      if (!draggedTask.value) return;
      const task = projectTasks.value.find((t: Task) => t.id === draggedTask.value);
      if (task) {
        const payload: Partial<Task> = { due_date: undefined };
        if (isTaskCompleted(task)) {
          payload.status = 'todo';
          payload.completed_at = undefined;
        }
        await applyTaskUpdate(task.id, payload);
      }
      draggedTask.value = null;
    },
    title: 'Verfügbare Aufgaben'
  });
}, { deep: true, immediate: true });

// Click outside handler für Projekt-Menü
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement;
  if (showProjectMenu.value && !target.closest('.project-menu')) {
    showProjectMenu.value = false;
    if (!editingNewTask.value) {
      hoveredDay.value = null;
      showAddTask.value = false;
    }
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
  if (!projects.value.length) projectStore.fetchProjects();
  if (!tasks.value.length) taskStore.fetchTasks();
  if (!appointments.value.length) appointmentStore.fetchAppointments();
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
  }
});
</script>

<template>
  <UDashboardPanel :id="`project-tasks-week-${weekId}`" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar :title="`KW ${getWeekNumber(weekStartDate)} - Tagesansicht`">
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
      <div class="flex-1 overflow-hidden w-full min-h-0">
        <div class="p-6 h-full flex flex-col min-h-0">
          <Transition name="fade-slide" mode="out-in">
            <div
              :key="weekStartDate.getTime()"
              class="flex-1 min-h-0 h-full"
            >
              <CalendarTimeTable
                :days="days"
                :project-id="projectId"
                :on-external-task-drop="handleExternalTaskDrop"
              />
            </div>
          </Transition>
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
                @click="selectProject(projectId, editingDay || undefined)"
              >
                <span class="text-sm text-muted-foreground">Aktuelles Projekt</span>
              </div>
            </div>
          </div>
        </Transition>
      </Teleport>
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

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>

