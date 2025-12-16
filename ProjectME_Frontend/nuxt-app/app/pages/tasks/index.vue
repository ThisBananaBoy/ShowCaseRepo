<script setup lang="ts">
import { nextTick, onMounted, onUnmounted } from 'vue'
import {
  getWeek,
  startOfWeek,
  format,
  addWeeks,
  subWeeks
} from 'date-fns'
import { de } from 'date-fns/locale'
import RecurringTasksTab from '~/components/tasks/RecurringTasksTab.vue'
import AddTaskButton from '~/components/tasks/AddTaskButton.vue'
import TaskItem from '~/components/tasks/TaskItem.vue'
import TaskCompletionZone from '~/components/tasks/TaskCompletionZone.vue'
import type { CreateTaskDto, Task, UpdateTaskDto } from '~/types/Task'
import { useTaskStore } from '~/stores/useTaskStore'
import { useProjectStore } from '~/stores/useProjectStore'
import { isTaskCompleted, getTaskWeek } from '~/utils/taskHelpers'
import { storeToRefs } from 'pinia'

const selectedTab = ref('calendar')

const taskStore = useTaskStore()
const projectStore = useProjectStore()
const { tasks } = storeToRefs(taskStore)
const { projects } = storeToRefs(projectStore)
const { setSidebarContent } = usePageSidebar()

const tabs = [
  {
    label: "Kalender",
    value: "calendar",
    icon: "i-lucide-calendar",
  },
  {
    label: "Wiederkehrende Aufgaben",
    value: "recurring",
    icon: "i-lucide-repeat",
  },
];

const draggedTask = ref<string | null>(null);
const router = useRouter();

// Aktive Projekte
const activeProjects = computed(() =>
  projects.value.filter(p => p.status === 'active')
)

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

// Alle Aufgaben aus aktiven Projekten
const allActiveTasks = computed(() => {
  const activeProjectIds = activeProjects.value.map(p => p.id)
  return tasks.value.filter(
    task => task.project_id && activeProjectIds.includes(task.project_id)
  )
})

// Generiere viele Wochen für das Carousel (20 vor und 20 nach aktueller Woche)
const allWeeks = computed(() => {
  const today = new Date();
  const current = startOfWeek(today, { weekStartsOn: 1 });
  const weeksList: Date[] = [];

  // 20 Wochen zurück
  for (let i = 20; i >= 1; i--) {
    weeksList.push(subWeeks(current, i));
  }
  // Aktuelle Woche
  weeksList.push(current);
  // 20 Wochen voraus
  for (let i = 1; i <= 20; i++) {
    weeksList.push(addWeeks(current, i));
  }

  return weeksList;
});

// Finde den Index der aktuellen Woche
const currentWeekIndex = computed(() => {
  const today = new Date();
  const current = startOfWeek(today, { weekStartsOn: 1 });
  return allWeeks.value.findIndex(w => w.getTime() === current.getTime());
});

// Carousel Index - startet bei der aktuellen Woche
const carouselIndex = ref(currentWeekIndex.value);

// Sichtbare Wochen für Dots (aktuelle Woche ± 5 Wochen)
const visibleWeeksForDots = computed(() => {
  const current = carouselIndex.value;
  const start = Math.max(0, current - 5);
  const end = Math.min(allWeeks.value.length, current + 6);

  return allWeeks.value.slice(start, end).map((date, i) => ({
    date,
    index: start + i
  }));
});


const tasksByWeek = computed(() => {
  const grouped: Record<number, Task[]> = {};
  allActiveTasks.value.forEach((task) => {
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
  // Nur nicht-fertige Aufgaben anzeigen
  return weekTasks.filter((task) => !isTaskCompleted(task));
}


function getCompletedTasksForWeek(weekStart: Date) {
  const weekNum = getWeekNumber(weekStart);
  const weekTasks = tasksByWeek.value[weekNum] || [];
  return weekTasks.filter((task) => isTaskCompleted(task));
}


async function handleDrop(event: DragEvent, targetWeek?: Date, isCompleteZone = false) {
  event.preventDefault()
  if (!draggedTask.value) return

  const task = tasks.value.find(t => t.id === draggedTask.value)
  if (!task?.id) return

  if (isCompleteZone) {
    await taskStore.updateTask(task.id, {
      status: 'completed',
      completed_at: new Date()
    })
  } else {
    const updates: UpdateTaskDto = {}
    if (targetWeek) {
      updates.due_date = startOfWeek(targetWeek, { weekStartsOn: 1 })
    }
    if (isTaskCompleted(task)) {
      updates.status = 'todo'
      updates.completed_at = undefined
    }

    if (Object.keys(updates).length > 0) {
      await taskStore.updateTask(task.id, updates)
    }
  }

  draggedTask.value = null
}

function handleDragStart(event: DragEvent, taskId: string) {
  if (!taskId) return
  draggedTask.value = taskId
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
  }
}

function onSidebarDragStart(task: Task) {
  if (!task.id) return
  draggedTask.value = task.id
}

function onSidebarDragEnd() {
  draggedTask.value = null
}

async function handleDropToSidebar() {
  if (!draggedTask.value) return

  const task = tasks.value.find(t => t.id === draggedTask.value)
  if (task?.id) {
    await taskStore.updateTask(task.id, {
      due_date: undefined,
      status: 'todo',
      completed_at: undefined
    })
  }

  draggedTask.value = null
}

// Tasks für Sidebar - verfügbare Tasks (ohne due_date oder nicht zugewiesen)
const availableTasksForSidebar = computed(() => {
  return tasks.value.filter(task => {
    // Nur Tasks aus aktiven Projekten oder ohne Projekt
    if (task.project_id) {
      const project = projects.value.find(p => p.id === task.project_id)
      if (!project || project.status !== 'active') return false
    }
    // Nur nicht-fertige Tasks
    if (isTaskCompleted(task)) return false
    // Tasks ohne due_date
    return !task.due_date
  })
})

// Tasks nach Projekten gruppieren für Sidebar
const tasksByProjectForSidebar = computed(() => {
  const grouped: Record<string, Task[]> = {}
  const noProjectTasks: Task[] = []

  availableTasksForSidebar.value.forEach(task => {
    if (task.project_id) {
      if (!grouped[task.project_id]) {
        grouped[task.project_id] = []
      }
      grouped[task.project_id]!.push(task)
    } else {
      noProjectTasks.push(task)
    }
  })

  return { grouped, noProjectTasks }
})

// Set sidebar content nur für calendar tab
watch(selectedTab, (tab) => {
  if (tab === 'calendar') {
    setSidebarContent('TasksSidebarContent', {
      availableTasks: availableTasksForSidebar.value,
      tasksByProject: tasksByProjectForSidebar.value,
      activeProjects: activeProjects.value,
      getProjectColor,
      getProjectBorderColor,
      onDragStart: onSidebarDragStart,
      onDragEnd: onSidebarDragEnd,
      onDropToSidebar: handleDropToSidebar,
      title: 'Verfügbare Aufgaben'
    });
  }
}, { immediate: true });

// Update sidebar when data changes (nur für calendar tab)
watch([availableTasksForSidebar, tasksByProjectForSidebar, activeProjects], () => {
  if (selectedTab.value === 'calendar') {
    setSidebarContent('TasksSidebarContent', {
      availableTasks: availableTasksForSidebar.value,
      tasksByProject: tasksByProjectForSidebar.value,
      activeProjects: activeProjects.value,
      getProjectColor,
      getProjectBorderColor,
      onDragStart: onSidebarDragStart,
      onDragEnd: onSidebarDragEnd,
      onDropToSidebar: handleDropToSidebar,
      title: 'Verfügbare Aufgaben'
    });
  }
}, { deep: true });

function handleDragOver(event: DragEvent) {
  event.preventDefault();
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

const hoveredWeek = ref<Date | null>(null);
const editingNewTask = ref(false);
const editingWeek = ref<Date | null>(null);
const newTaskName = ref("");
const showAddTask = ref(false);
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
    showAddTask.value = true;
  }, 300); // 300ms Verzögerung
}

function handleWeekMouseLeave() {
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
    hoverTimeout.value = null;
  }
  if (!editingNewTask.value && !showProjectMenu.value) {
    hoveredWeek.value = null;
    showAddTask.value = false;
  }
}


function showProjectSelection(event?: MouseEvent, week?: Date) {
  if (event) {
    event.stopPropagation();
    projectMenuPosition.value = { x: event.clientX, y: event.clientY };
  }
  showProjectMenu.value = true;
  showAddTask.value = false;
  // Store week for when project is selected
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

async function createTask() {
  if (!newTaskName.value.trim()) {
    cancelNewTask()
    return
  }

  const newTask: CreateTaskDto = {
    name: newTaskName.value.trim(),
    status: 'todo',
    priority: 2,
    project_id: selectedProjectForTask.value || undefined
  }

  if (editingWeek.value) {
    newTask.due_date = startOfWeek(editingWeek.value, { weekStartsOn: 1 })
  }

  await taskStore.createTask(newTask)
  cancelNewTask()
}

function navigateToWeek(weekStart: Date) {
  const weekStartFormatted = format(weekStart, "yyyy-MM-dd");
  router.push(`/tasks/${weekStartFormatted}`);
}

function handleWeekContainerClick(event: MouseEvent, weekStart: Date) {
  // Nicht navigieren, wenn gerade eine Task bearbeitet wird
  if (editingNewTask.value && editingWeek.value?.getTime() === weekStart.getTime()) {
    return;
  }

  // Prüfe, ob auf interaktive Elemente geklickt wurde
  const target = event.target as HTMLElement;
  const currentTarget = event.currentTarget as HTMLElement;

  // Wenn das Target gleich dem CurrentTarget ist, wurde direkt auf den Container geklickt
  if (target === currentTarget) {
    navigateToWeek(weekStart);
    return;
  }

  // Prüfe auf TaskItem (TaskItem hat @click.stop, also sollte das Event hier nicht ankommen)
  if (target.closest('.cursor-move')) {
    return;
  }

  // Prüfe auf Buttons, Inputs und andere interaktive Elemente
  if (target.closest('button, input, [role="button"], [data-completion-zone]')) {
    return;
  }

  // Navigiere zur Wochenansicht (wurde auf leeren Bereich geklickt)
  navigateToWeek(weekStart);
}

// Carousel Navigation
const carousel = useTemplateRef('carousel');

function onCarouselSelect(selectedIndex: number) {
  // selectedIndex ist der Index im allWeeks Array
  carouselIndex.value = selectedIndex;
}

// Pfeiltasten Navigation
function handleKeydown(event: KeyboardEvent) {
  if (selectedTab.value !== 'calendar') return;

  if (event.key === 'ArrowLeft') {
    event.preventDefault();
    carousel.value?.emblaApi?.scrollPrev();
  } else if (event.key === 'ArrowRight') {
    event.preventDefault();
    carousel.value?.emblaApi?.scrollNext();
  }
}

onMounted(() => {
  taskStore.fetchTasks()
  projectStore.fetchProjects()
  document.addEventListener('click', handleClickOutside);
  document.addEventListener('keydown', handleKeydown);
  // Setze initialen Index
  if (currentWeekIndex.value !== -1) {
    carouselIndex.value = currentWeekIndex.value;
    nextTick(() => {
      carousel.value?.emblaApi?.scrollTo(carouselIndex.value);
    });
  }
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  document.removeEventListener('keydown', handleKeydown);
  if (hoverTimeout.value) {
    clearTimeout(hoverTimeout.value);
  }
});

// Click outside handler für Projekt-Menü
function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement;
  if (showProjectMenu.value && !target.closest('.project-menu')) {
    showProjectMenu.value = false;
    if (!editingNewTask.value) {
      hoveredWeek.value = null;
      showAddTask.value = false;
    }
  }
}

</script>

<template>
  <UDashboardPanel id="tasks" class="h-full flex flex-col">
    <template #header>
      <UDashboardNavbar title="Aufgaben">
        <template #leading>
          <UDashboardSidebarCollapse />
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
      <!-- Kalender Tab -->
      <div v-if="selectedTab === 'calendar'" class="flex-1 overflow-hidden w-full min-h-0 flex flex-col">
        <div class="p-6 flex-1 flex flex-col min-h-0">
        <!-- Week Cards Carousel -->
        <div class="flex-1 flex flex-col min-h-0">
          <UCarousel
            ref="carousel"
            v-slot="{ item: weekStart }"
            :items="allWeeks"
            :start-index="currentWeekIndex"
            :slides-to-scroll="1"
            :watch-drag="false"
            :ui="{
              item: 'basis-full md:basis-1/2 lg:basis-1/3 h-full',
              container: 'h-full',
              viewport: 'h-full'
            }"
            arrows
            class="flex-1 min-h-0 h-full"
            @select="onCarouselSelect"
          >
            <div
              :key="weekStart.getTime()"
              :class="[
                'border rounded-lg p-4 h-full bg-muted/30 transition-all duration-300 hover:shadow-lg relative group flex flex-col w-full mx-2',
                editingNewTask && editingWeek?.getTime() === weekStart.getTime() ? '' : 'cursor-pointer'
              ]"
              @click.stop="handleWeekContainerClick($event, weekStart)"
              @drop="handleDrop($event, weekStart)"
              @dragover="handleDragOver"
              @mouseenter="handleWeekMouseEnter(weekStart)"
              @mouseleave="handleWeekMouseLeave"
            >
              <div class="font-semibold mb-3 text-sm">
                KW {{ getWeekNumber(weekStart) }}
                <span class="text-muted-foreground text-xs ml-2">
                  {{ format(weekStart, "dd.MM.", { locale: de }) }}
                </span>
              </div>
              <div class="space-y-1 flex-1 overflow-y-auto">
                <!-- Existing Tasks -->
                  <TaskItem
                    v-for="task in getTasksForWeek(weekStart)"
                    :key="task.id"
                    :task="task"
                    :get-project-color="getProjectColor"
                    :get-project-border-color="getProjectBorderColor"
                    @drag-start="(task, event) => { if (event) handleDragStart(event, task.id || ''); }"
                    @drag-end="onSidebarDragEnd"
                  />

                <!-- Add Task Button -->
                  <AddTaskButton
                    v-if="!(editingNewTask && editingWeek?.getTime() !== weekStart.getTime()) && !showProjectMenu"
                    v-model="newTaskName"
                    :is-editing="editingNewTask && editingWeek?.getTime() === weekStart.getTime()"
                    @update:is-editing="editingNewTask = $event; if ($event) editingWeek = weekStart; else editingWeek = null"
                    @start-editing="showProjectSelection($event, weekStart)"
                    @submit="createTask"
                    @cancel="cancelNewTask"
                  />
              </div>

              <!-- Complete Zone (Drop Zone für fertige Aufgaben) -->
              <TaskCompletionZone
                :completed-tasks="getCompletedTasksForWeek(weekStart)"
                :is-hovered="hoveredCompleteZone?.type === 'week' && hoveredCompleteZone?.date.getTime() === weekStart.getTime()"
                :is-dragged-over="draggedOverCompleteZone"
                :is-open="showCompletedPanel?.type === 'week' && showCompletedPanel?.date.getTime() === weekStart.getTime()"
                :get-project-color="getProjectColor"
                size="md"
                :show-count="false"
                data-completion-zone
                @drop="handleDrop($event, weekStart, true)"
                @dragover="handleCompleteZoneDragOver"
                @dragleave="handleCompleteZoneDragLeave"
                @mouseenter="hoveredCompleteZone = { type: 'week', date: weekStart }"
                @mouseleave="hoveredCompleteZone = null; draggedOverCompleteZone = false"
                @click="editingNewTask && editingWeek?.getTime() === weekStart.getTime() ? null : toggleCompletedPanel(weekStart)"
                @task-drag-start="(task, e) => handleDragStart(e, task.id || '')"
              />
            </div>
          </UCarousel>

          <!-- Custom Dots mit KW-Beschriftungen -->
          <div class="flex items-center justify-center gap-2 mt-4 flex-wrap">
            <button
              v-for="week in visibleWeeksForDots"
              :key="week.date.getTime()"
              :class="[
                'px-3 py-1 rounded text-xs transition-colors',
                carouselIndex === week.index
                  ? 'bg-primary text-primary-foreground font-semibold'
                  : 'bg-muted hover:bg-muted/80 text-muted-foreground'
              ]"
              @click="carousel?.emblaApi?.scrollTo(week.index)"
            >
              KW {{ getWeekNumber(week.date) }}
            </button>
          </div>
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
                v-for="project in activeProjects"
                :key="project.id"
                class="flex items-center gap-2 px-3 py-2 rounded hover:bg-muted/50 cursor-pointer transition-colors"
                @click="selectProject(project.id, editingWeek || undefined)"
              >
                <div
                  class="w-3 h-3 rounded-full shrink-0"
                  :class="getProjectColor(project.id)"
                />
                <span class="text-sm">{{ project.name }}</span>
              </div>
              <div
                class="flex items-center gap-2 px-3 py-2 rounded hover:bg-muted/50 cursor-pointer transition-colors border-t border-default mt-1 pt-1"
                @click="selectProject('', editingWeek || undefined)"
              >
                <span class="text-sm text-muted-foreground">Kein Projekt</span>
              </div>
            </div>
          </div>
        </Transition>
      </Teleport>
        </div>

      <!-- Wiederkehrende Aufgaben Tab -->
      <RecurringTasksTab v-else-if="selectedTab === 'recurring'" />
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

.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
  overflow: hidden;
}

.slide-down-enter-from {
  opacity: 0;
  max-height: 0;
  transform: translateY(-10px);
}

.slide-down-leave-to {
  opacity: 0;
  max-height: 0;
  transform: translateY(-10px);
}
</style>

