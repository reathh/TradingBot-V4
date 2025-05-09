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

<style lang="scss">
body {
  margin: 0;
  padding: 0;
  font-family: "Poppins", sans-serif;
  background-color: #1e1e2f;
  color: #ffffff;
}

body.white-content {
  background-color: #f5f6fa;
  color: #525f7f;
}

#app {
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  font-weight: 400;
}
</style>
