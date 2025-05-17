<template>
  <router-view />
</template>

<script setup>
import { onMounted } from "vue";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";

const authStore = useAuthStore();
const themeStore = useThemeStore();

onMounted(() => {
  authStore.checkAuth();
  // Theme is automatically initialized when store is created
});
</script>