<template>
  <WorldMap :data="data" />
</template>

<script setup>
import { defineAsyncComponent } from "vue";

// Lazy load WorldMap component
const WorldMap = defineAsyncComponent({
  loader: () => import("./WorldMap.vue"),
  delay: 200,
  // Optional: add loading component if you want to display something while loading
  // loadingComponent: LoadingPanel
});

// Define props
defineProps({
  data: {
    type: Object,
    default: () => ({}),
  },
});
</script>

<style scoped></style>
