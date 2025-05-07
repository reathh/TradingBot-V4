<template>
  <div class="navbar-minimize-fixed" style="opacity: 1">
    <FadeTransition>
      <SidebarToggleButton v-if="showButton" class="text-muted" />
    </FadeTransition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from "vue";
import { FadeTransition } from "vue3-transitions";
import SidebarToggleButton from "./SidebarToggleButton.vue";

const showScrollThreshold = 50;
const currentScroll = ref(0);
const scrollTicking = ref(false);

const showButton = computed(() => currentScroll.value > showScrollThreshold);

function handleScroll() {
  currentScroll.value = window.scrollY;

  if (!scrollTicking.value) {
    window.requestAnimationFrame(() => {
      scrollTicking.value = false;
    });
    scrollTicking.value = true;
  }
}

onMounted(() => {
  window.addEventListener("scroll", handleScroll);
});

onUnmounted(() => {
  window.removeEventListener("scroll", handleScroll);
});
</script>

<style scoped>
/* Add styles if needed */
</style>
