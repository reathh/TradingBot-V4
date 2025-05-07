<template>
  <router-view />
</template>

<script setup>
import { onMounted, inject, computed } from "vue";
import { useAuthStore } from "@/stores/auth";
import { useRoute } from "vue-router";

const authStore = useAuthStore();
const route = useRoute();
// const $rtl = inject("$rtl");
// const isRTL = computed(() => $rtl?.isRTL.value); // Access the reactive isRTL

onMounted(() => {
  authStore.checkAuth();

  // if (route.query.rtl === "true" && $rtl) {
  //   $rtl.enableRTL();
  // } else if ($rtl) {
  //   $rtl.disableRTL();
  // }
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
body[dir="rtl"] {
  direction: rtl;
  /* ... RTL specific body styles ... */
}
/* ... RTL specific component styles ... */
</style>
