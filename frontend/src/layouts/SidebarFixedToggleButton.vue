<template>
  <div class="navbar-minimize-fixed" style="opacity: 1">
    <Transition name="fade">
      <div v-if="showButton">
        <SidebarToggleButton class="text-muted" />
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from "vue";
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
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>
