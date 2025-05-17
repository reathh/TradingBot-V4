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
        <a class="navbar-brand" href="#pablo">{{ routeName }}</a>
      </div>
    </template>

    <ul class="navbar-nav ml-auto">
      <BaseDropdown
        tag="li"
        menu-on-right
        title-tag="a"
        title-classes="nav-link"
        class="nav-item"
        menu-classes="dropdown-navbar"
      >
        <template #title>
          <div class="photo"><img src="/img/mike.jpg" /></div>
          <b class="caret d-none d-lg-block d-xl-block"></b>
          <p class="d-lg-none">Log out</p>
        </template>
        <li class="nav-link px-3 py-2 d-flex align-items-center">
          <span class="me-2">Dark Mode</span>
          <BaseSwitch v-model="darkMode" @update:modelValue="toggleMode" />
        </li>
        <div class="dropdown-divider"></div>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item" @click.prevent="handleLogout">Log out</a>
        </li>
      </BaseDropdown>
    </ul>
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

const darkMode = ref(themeStore.isDarkMode);
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
</style>
