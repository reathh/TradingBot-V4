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
          :class="{ toggled: $sidebar?.showSidebar }"
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
      <div class="search-bar input-group" @click="searchModalVisible = true">
        <button class="btn btn-link" id="search-button">
          <i class="tim-icons icon-zoom-split"></i>
        </button>
      </div>

      <Modal
        v-model:show="searchModalVisible"
        class="modal-search"
        id="searchModal"
        :centered="false"
        :show-close="true"
      >
        <template #header>
          <input
            v-model="searchQuery"
            type="text"
            class="form-control"
            id="inlineFormInputGroup"
            placeholder="SEARCH"
          />
        </template>
      </Modal>

      <BaseDropdown
        tag="li"
        menu-on-right
        title-tag="a"
        title-classes="nav-link"
        class="nav-item"
      >
        <template #title>
          <div class="notification d-none d-lg-block d-xl-block"></div>
          <i class="tim-icons icon-sound-wave"></i>
          <p class="d-lg-none">New Notifications</p>
        </template>
        <a href="#" class="nav-item dropdown-item">Mike John responded to your email</a>
        <a href="#" class="nav-item dropdown-item">You have 5 more tasks</a>
        <a href="#" class="nav-item dropdown-item">Your friend Michael is in town</a>
        <a href="#" class="nav-item dropdown-item">Another notification</a>
        <a href="#" class="nav-item dropdown-item">Another one</a>
      </BaseDropdown>

      <BaseDropdown
        tag="li"
        menu-on-right
        title-tag="a"
        title-classes="nav-link"
        class="nav-item"
        menu-classes="dropdown-navbar"
        icon="tim-icons icon-settings-gear-63"
        title=""
      >
        <a href="#" class="nav-item dropdown-item">Profile</a>
        <a href="#" class="nav-item dropdown-item">Settings</a>
        <a href="#" class="nav-item dropdown-item toggle-theme" @click="toggleDarkMode">
          <i class="mr-2" :class="darkModeIcon"></i>
          {{ isDarkMode ? 'Light Mode' : 'Dark Mode' }}
        </a>
        <div class="dropdown-divider"></div>
        <a href="#" class="nav-item dropdown-item">Log out</a>
      </BaseDropdown>
    </ul>
  </BaseNav>
</template>

<script setup>
import { ref, computed, inject } from "vue";
import { useRoute } from "vue-router";
import { useThemeStore } from "@/stores/theme";

import Modal from "@/components/Modal.vue";
import BaseNav from "@/components/Navbar/BaseNav.vue";
import BaseDropdown from "@/components/BaseDropdown.vue";
import SidebarToggleButton from "./SidebarToggleButton.vue";

const route = useRoute();
const $sidebar = inject("$sidebar", {
  showSidebar: false,
  displaySidebar: () => {},
});
const themeStore = useThemeStore();

const showMenu = ref(false);
const searchModalVisible = ref(false);
const searchQuery = ref("");

// Dark mode related computed properties
const isDarkMode = computed(() => themeStore.isDarkMode);
const darkModeIcon = computed(() =>
  isDarkMode.value ? "tim-icons icon-bulb-63" : "tim-icons icon-button-power"
);

const toggleDarkMode = () => {
  themeStore.toggleDarkMode();
};

const routeName = computed(() => {
  const name = route.name || "";
  return name.charAt(0).toUpperCase() + name.slice(1);
});

function toggleSidebar() {
  $sidebar.displaySidebar(!$sidebar.showSidebar);
}
</script>

<style scoped>
.top-navbar {
  top: 0px;
}

.toggle-theme {
  display: flex;
  align-items: center;
}

.toggle-theme i {
  margin-right: 5px;
}
