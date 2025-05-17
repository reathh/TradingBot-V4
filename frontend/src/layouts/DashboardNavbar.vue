<template>
  <base-nav
    v-model="showMenu"
    class="navbar-absolute top-navbar"
    type="white"
    :transparent="true"
  >
    <div slot="brand" class="navbar-wrapper">
      <div class="navbar-minimize d-inline"><sidebar-toggle-button /></div>
      <div
        class="navbar-toggle d-inline"
        :class="{ toggled: $sidebar.showSidebar }"
      >
        <button type="button" class="navbar-toggler" @click="toggleSidebar">
          <span class="navbar-toggler-bar bar1"></span>
          <span class="navbar-toggler-bar bar2"></span>
          <span class="navbar-toggler-bar bar3"></span>
        </button>
      </div>
      <a class="navbar-brand" href="#pablo">{{ routeName }}</a>
    </div>

    <ul class="navbar-nav ml-auto">
      <div class="search-bar input-group" @click="searchModalVisible = true">
        <!--
          <input type="text" class="form-control" placeholder="Search...">
          <div class="input-group-addon"><i class="tim-icons icon-zoom-split"></i></div>
        -->
        <button
          class="btn btn-link"
          id="search-button"
          data-toggle="modal"
          data-target="#searchModal"
        >
          <i class="tim-icons icon-zoom-split"></i>
        </button>
        <!-- You can choose types of search input -->
      </div>
      <modal
        :show.sync="searchModalVisible"
        class="modal-search"
        id="searchModal"
        :centered="false"
        :show-close="true"
      >
        <input
          slot="header"
          v-model="searchQuery"
          type="text"
          class="form-control"
          id="inlineFormInputGroup"
          placeholder="SEARCH"
        />
      </modal>
      <base-dropdown
        tag="li"
        menu-on-right
        title-tag="a"
        title-classes="nav-link"
        class="nav-item"
      >
        <template
          slot="title"
        >
          <div class="notification d-none d-lg-block d-xl-block"></div>
          <i class="tim-icons icon-sound-wave"></i>
          <p class="d-lg-none">New Notifications</p>
        </template>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item"
            >Mike John responded to your email</a
          >
        </li>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item">You have 5 more tasks</a>
        </li>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item"
            >Your friend Michael is in town</a
          >
        </li>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item">Another notification</a>
        </li>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item">Another one</a>
        </li>
      </base-dropdown>
      <base-dropdown
        tag="li"
        menu-on-right
        title-tag="a"
        class="nav-item"
        title-classes="nav-link"
        menu-classes="dropdown-navbar"
      >
        <template
          slot="title"
        >
          <div class="photo"><img src="/img/mike.jpg" /></div>
          <b class="caret d-none d-lg-block d-xl-block"></b>
          <p class="d-lg-none">Log out</p>
        </template>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item">Profile</a>
        </li>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item">Settings</a>
        </li>
        <div class="dropdown-divider"></div>
        <li class="nav-link">
          <a href="#" class="nav-item dropdown-item">Log out</a>
        </li>
      </base-dropdown>
    </ul>
  </base-nav>
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
