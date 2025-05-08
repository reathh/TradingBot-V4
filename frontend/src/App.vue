<template>
  <router-view />
</template>

<script setup>
import { onMounted } from "vue";
import { useAuthStore } from "@/stores/auth";

const authStore = useAuthStore();

onMounted(() => {
  authStore.checkAuth();
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

#app {
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  font-weight: 400;
}
</style>
