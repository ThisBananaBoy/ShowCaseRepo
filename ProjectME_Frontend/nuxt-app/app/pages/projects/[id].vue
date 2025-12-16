<script setup lang="ts">
const route = useRoute();
const router = useRouter();
const projectId = route.params.id as string;

const projectStore = useProjectStore();
const project = computed(() => projectStore.getProjectById(projectId));

// Redirect wenn Projekt nicht existiert
watchEffect(() => {
  if (!project.value) {
    router.push("/projects");
  }
});

onMounted(() => {
  if (!project.value) projectStore.fetchProjects();
});
</script>

<template>
  <NuxtPage />
</template>
