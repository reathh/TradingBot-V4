<template>
  <BaseNav
    v-model:show="showMenu"
    class="navbar-absolute top-navbar"
    type="white"
    :transparent="true"
  >
    <template #brand>
      <div class="navbar-wrapper">
        <div class="navbar-minimize d-inline">
          <SidebarToggleButton />
        </div>
        <div
          class="navbar-toggle d-inline"
          :class="{ toggled: sidebarStore.showSidebar }"
        >
          <button type="button" class="navbar-toggler" @click="toggleSidebar">
            <span class="navbar-toggler-bar bar1"></span>
            <span class="navbar-toggler-bar bar2"></span>
            <span class="navbar-toggler-bar bar3"></span>
          </button>
        </div>
        <a class="navbar-brand" href="#">{{ routeName }}</a>
      </div>
    </template>
    <template #default>
      <div class="navbar-toolbar ml-auto d-flex align-items-center" style="margin-left:auto;">
        <button
          class="btn btn-link btn-just-icon"
          :aria-label="darkMode ? 'Switch to light mode' : 'Switch to dark mode'"
          @click="themeStore.toggleDarkMode()"
        >
          <i :class="darkMode ? 'fas fa-sun sun-icon' : 'fas fa-moon moon-icon'" />
        </button>
      </div>
    </template>
  </BaseNav>
</template>

<script setup>
import { ref, computed, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";
import { useSidebarStore } from "@/stores/sidebar";

import Modal from "@/components/Modal.vue";
import BaseNav from "@/components/Navbar/BaseNav.vue";
import BaseDropdown from "@/components/BaseDropdown.vue";
import SidebarToggleButton from "./SidebarToggleButton.vue";
import BaseSwitch from "@/components/BaseSwitch.vue";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const themeStore = useThemeStore();
const sidebarStore = useSidebarStore();

const showMenu = ref(false);
const searchModalVisible = ref(false);
const searchQuery = ref("");

const darkMode = computed(() => themeStore.isDarkMode);
watch(() => themeStore.isDarkMode, (newValue) => {
  darkMode.value = newValue;
});
watch(darkMode, (newValue) => {
  themeStore.setDarkMode(newValue);
});

const routeName = computed(() => {
  const name = route.name || "";
  return name.charAt(0).toUpperCase() + name.slice(1);
});

function toggleSidebar() {
  sidebarStore.setShowSidebar(!sidebarStore.showSidebar);
}

function toggleMode(isDark) {
  themeStore.setDarkMode(isDark);
}

async function handleLogout() {
  await authStore.logout();
}
</script>

<style scoped>
.top-navbar {
  top: 0px;
}
.sun-icon {
  color: #fff !important;
  font-size: 1.5em !important;
}
.moon-icon {
  font-size: 1.5em !important;
}
</style>
