<script setup lang="ts">
import { nextTick } from "vue";
import type { RecurringTask } from "~/types/RecurringTask";
import AddTaskButton from "./AddTaskButton.vue";

interface Props {
  recurringTasks?: Task[];
  onDragStart?: (task: Task, date?: Date) => void;
  onDragEnd?: () => void;
  onCreateTask?: (name: string) => void;
  onDropToContextSidebar?: (event: DragEvent) => void; // Drop zur rechten ContextSidebar
  onDragOver?: (event: DragEvent) => void;
  onDragLeave?: () => void;
}

const props = withDefaults(defineProps<Props>(), {
  recurringTasks: () => [],
  onCreateTask: () => {},
  onDropToContextSidebar: () => {},
  onDragOver: () => {},
  onDragLeave: () => {},
});

const newTaskName = ref("");
const editingNewTask = ref(false);
const dragOverContextSidebar = ref(false); // Drag-Over für die rechte ContextSidebar

function handleDragStart(event: DragEvent, task: RecurringTask) {
  props.onDragStart?.(task);
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = "move";
  }
}

function handleDragOver(event: DragEvent) {
  event.preventDefault();
  dragOverContextSidebar.value = true;
  props.onDragOver?.(event);
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = "move";
  }
}

function handleDragLeave() {
  dragOverContextSidebar.value = false;
  props.onDragLeave?.();
}

function handleDrop(event: DragEvent) {
  event.preventDefault();
  props.onDropToContextSidebar?.(event);
}

function createTask() {
  if (!newTaskName.value.trim()) {
    editingNewTask.value = false;
    return;
  }
  props.onCreateTask(newTaskName.value.trim());
  newTaskName.value = "";
  editingNewTask.value = false;
}

function startEditing() {
  editingNewTask.value = true;
  nextTick(() => {
    const input = document.querySelector(".new-task-input") as HTMLInputElement;
    input?.focus();
  });
}

function cancelEditing() {
  editingNewTask.value = false;
  newTaskName.value = "";
}
</script>

<template>
  <div
    class="p-4 overflow-y-auto h-full"
    :class="{
      'bg-primary/5': dragOverContextSidebar,
    }"
    @drop="handleDrop"
    @dragover="handleDragOver"
    @dragleave="handleDragLeave"
  >
    <h3 class="font-semibold mb-4 text-sm uppercase text-muted-foreground">
      Wiederkehrende Aufgaben
    </h3>

    <!-- Neue Aufgabe erstellen -->
    <div class="mb-4">
      <AddTaskButton
        v-model="newTaskName"
        :is-editing="editingNewTask"
        placeholder="Neue Aufgabe"
        @update:is-editing="editingNewTask = $event"
        @start-editing="startEditing"
        @submit="createTask"
        @cancel="cancelEditing"
      />
    </div>

    <div class="space-y-1">
      <div
        v-for="task in recurringTasks"
        :key="task.id"
        class="cursor-move"
        :draggable="true"
        @dragstart="handleDragStart($event, task)"
        @dragend="onDragEnd?.()"
      >
        <div
          class="px-2 py-1.5 rounded border hover:bg-muted/50 transition-colors flex items-center gap-2 border-muted-foreground/20"
        >
          <p class="text-xs flex-1 truncate">
            {{ task.name }}
          </p>
        </div>
      </div>

      <div
        v-if="recurringTasks.length === 0"
        class="text-center py-8 text-muted-foreground"
      >
        <UIcon name="i-lucide-repeat" class="size-12 mx-auto mb-2 opacity-50" />
        <p class="text-xs">Keine wiederkehrenden Aufgaben</p>
      </div>
    </div>
  </div>
</template>
