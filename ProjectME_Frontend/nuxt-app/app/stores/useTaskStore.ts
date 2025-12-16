// stores/taskStore.ts
import { defineStore } from "pinia";
import { useAuthFetch } from "~/composables/useAuthFetch";
import type { CreateTaskDto, Task, UpdateTaskDto } from "~/types/Task";

export const useTaskStore = defineStore("tasks", () => {
  // ✅ Nur State - kein useFetch im Setup!
  const tasks = ref<Task[]>([]);
  const loading = ref(false);
  const error = ref<Error | null>(null);

  // Separate loading states für CRUD
  const isCreating = ref(false);
  const isUpdating = ref(false);
  const isDeleting = ref(false);

  const getTasksByProject = (projectId: string) =>
    tasks.value.filter((task) => task.project_id === projectId);

  const removeLocalTask = (id: string) => {
    tasks.value = tasks.value.filter((t) => t.id !== id);
  };

  // ✅ useFetch/useAuthFetch NUR in Actions
  const fetchTasks = async () => {
    loading.value = true;
    error.value = null;

    try {
      const { data, error: fetchError } = await useAuthFetch<Task[]>("/tasks");

      if (fetchError.value) {
        error.value = fetchError.value;
        return;
      }

      tasks.value = data.value || [];
    } catch (e: unknown) {
      error.value = e as Error;
      console.error("Fetch failed:", e);
    } finally {
      loading.value = false;
    }
  };

  // CREATE mit Optimistic Update
  const createTask = async (taskData: CreateTaskDto) => {
    isCreating.value = true;
    error.value = null;

    // Optimistic Update: Erstelle temporäre Task mit generierter ID
    const tempId = `temp-${Date.now()}`;
    const optimisticTask: Task = {
      ...taskData,
      id: tempId,
    } as Task;

    // Optimistisch hinzufügen
    tasks.value = [...tasks.value, optimisticTask];

    try {
      const { data: newTask, error: fetchError } = await useAuthFetch<Task>(
        "/tasks",
        {
          method: "POST",
          body: taskData,
        }
      );

      if (fetchError.value) {
        // Rollback: Entferne optimistische Task
        tasks.value = tasks.value.filter((t) => t.id !== tempId);
        error.value = fetchError.value;
        return;
      }

      if (newTask.value) {
        // Ersetze optimistische Task mit Server-Response
        const index = tasks.value.findIndex((t) => t.id === tempId);
        if (index !== -1) {
          tasks.value[index] = newTask.value;
        }
      }

      return newTask.value;
    } catch (e: unknown) {
      // Rollback: Entferne optimistische Task
      tasks.value = tasks.value.filter((t) => t.id !== tempId);
      error.value = e as Error;
      return;
    } finally {
      isCreating.value = false;
    }
  };

  // UPDATE mit Optimistic Update
  const updateTask = async (id: string, updates: UpdateTaskDto) => {
    isUpdating.value = true;
    error.value = null;

    const index = tasks.value.findIndex((t) => t.id === id);
    if (index === -1) {
      isUpdating.value = false;
      return;
    }

    // Speichere ursprünglichen Zustand für Rollback
    const originalTask = { ...tasks.value[index] };

    // Optimistic Update: Wende Updates sofort an
    const { id: _, ...updatesWithoutId } = updates as Task;
    tasks.value[index] = { ...tasks.value[index], ...updatesWithoutId, id };

    try {
      const { data: updatedTask, error: fetchError } = await useAuthFetch<Task>(
        `/tasks/${id}`,
        {
          method: "PUT",
          body: updates,
        }
      );

      if (fetchError.value) {
        // Rollback: Stelle ursprünglichen Zustand wieder her
        tasks.value[index] = originalTask as Task;
        error.value = fetchError.value;
        return;
      }

      if (updatedTask.value) {
        // Ersetze mit Server-Response
        tasks.value[index] = updatedTask.value;
      }

      return updatedTask.value;
    } catch (e: unknown) {
      // Rollback: Stelle ursprünglichen Zustand wieder her
      tasks.value[index] = originalTask as Task;
      error.value = e as Error;
      return;
    } finally {
      isUpdating.value = false;
    }
  };

  // DELETE mit Optimistic Update
  const deleteTask = async (id: string) => {
    isDeleting.value = true;
    error.value = null;

    const index = tasks.value.findIndex((t) => t.id === id);
    if (index === -1) {
      isDeleting.value = false;
      return;
    }

    // Speichere Task für Rollback
    const deletedTask = tasks.value[index] as Task;

    // Optimistic Update: Entferne Task sofort
    tasks.value = tasks.value.filter((t) => t.id !== id);

    try {
      const { error: fetchError } = await useAuthFetch(`/tasks/${id}`, {
        method: "DELETE",
      });

      if (fetchError.value) {
        // Rollback: Füge Task wieder hinzu
        tasks.value = [
          ...tasks.value.slice(0, index),
          deletedTask,
          ...tasks.value.slice(index),
        ];
        error.value = fetchError.value;
        return;
      }
    } catch (e: unknown) {
      // Rollback: Füge Task wieder hinzu
      tasks.value = [
        ...tasks.value.slice(0, index),
        deletedTask,
        ...tasks.value.slice(index),
      ];
      error.value = e as Error;
      return;
    } finally {
      isDeleting.value = false;
    }
  };

  // ✅ Computed
  const completedTasks = computed(() =>
    tasks.value.filter((t) => t.completed_at)
  );

  const pendingTasks = computed(() =>
    tasks.value.filter((t) => !t.completed_at)
  );

  return {
    // State
    tasks: readonly(tasks),
    loading: readonly(loading),
    error: readonly(error),
    isCreating: readonly(isCreating),
    isUpdating: readonly(isUpdating),
    isDeleting: readonly(isDeleting),

    // Computed
    completedTasks,
    pendingTasks,

    // Actions
    fetchTasks,
    createTask,
    updateTask,
    deleteTask,
    // Local helpers
    getTasksByProject,
    removeLocalTask,
  };
});
