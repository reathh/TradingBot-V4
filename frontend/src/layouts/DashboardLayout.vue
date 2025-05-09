<template>
  <div class="wrapper" :class="{ 'nav-open': sidebarStore.showSidebar }">
    <sidebar-fixed-toggle-button />
    <side-bar
      :background-color="sidebarBackground"
      :short-title="shortTitle"
      :title="title"
      :sidebar-links="menuItems"
    >
    </side-bar>
    <!-- <sidebar-share v-model:backgroundColor="sidebarBackground"> </sidebar-share> -->
    <div class="main-panel" :data="sidebarBackground">
      <dashboard-navbar></dashboard-navbar>
      <div class="content" @click="toggleSidebar">
        <router-view v-slot="{ Component }">
          <fade-in-out :duration="200" mode="out-in">
            <component :is="Component" />
          </fade-in-out>
        </router-view>
      </div>
    </div>

    <Notifications />
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import { useSidebarStore } from "@/stores/sidebar";
import { useThemeStore } from "@/stores/theme";
import DashboardNavbar from "./DashboardNavbar.vue";
import SideBar from "@/components/SidebarPlugin/SideBar.vue";
import Notifications from "@/components/Notifications/Notifications.vue";
import SidebarFixedToggleButton from "./SidebarFixedToggleButton.vue";
// import SidebarShare from "./SidebarSharePlugin.vue";
import { FadeInOut } from "vue3-transitions";

const sidebarStore = useSidebarStore();
const themeStore = useThemeStore();
const sidebarBackground = ref("vue");
const title = ref("DASHBOARD");
const shortTitle = ref("DB");

// Dark mode related computed properties
const isDarkMode = computed(() => themeStore.isDarkMode);
const darkModeIcon = computed(() => 
  isDarkMode.value ? "tim-icons icon-button-power" : "tim-icons icon-bulb-63"
);

const toggleDarkMode = () => {
  themeStore.toggleDarkMode();
};

const toggleSidebar = () => {
  if (sidebarStore.showSidebar) {
    sidebarStore.displaySidebar(false);
  }
};
const menuItems = ref([
  {
    name: "Dashboard",
    icon: "tim-icons icon-chart-pie-36",
    path: "/dashboard",
  },
  {
    name: "Bots",
    icon: "tim-icons icon-settings",
    path: "/bots",
  },
  {
    name: "Pages",
    icon: "tim-icons icon-image-02",
    children: [
      { name: "Pricing", path: "/pages/pricing" },
      { name: "Login", path: "/pages/login" },
      { name: "Register", path: "/pages/register" },
      { name: "User Profile", path: "/pages/user" },
    ],
  },
  {
    name: "Forms",
    icon: "tim-icons icon-notes",
    children: [
      { name: "Regular Forms", path: "/forms/regular" },
      { name: "Extended Forms", path: "/forms/extended" },
      { name: "Validation Forms", path: "/forms/validation" },
    ],
  },
  {
    name: "Tables",
    icon: "tim-icons icon-puzzle-10",
    children: [
      { name: "Regular Tables", path: "/table-list/regular" },
      { name: "Extended Tables", path: "/table-list/extended" },
      { name: "Paginated Tables", path: "/table-list/paginated" },
    ],
  },
  {
    name: "Charts",
    icon: "tim-icons icon-chart-bar-32",
    path: "/charts",
  },
  {
    name: computed(() => isDarkMode.value ? "Dark Mode" : "Light Mode"),
    icon: darkModeIcon,
    click: toggleDarkMode
  },
]);
</script>

<style lang="scss">
$scaleSize: 0.95;
@keyframes zoomIn95 {
  from {
    opacity: 0;
    transform: scale3d($scaleSize, $scaleSize, $scaleSize);
  }
  to {
    opacity: 1;
  }
}

.main-panel .zoomIn {
  animation-name: zoomIn95;
}

@keyframes zoomOut95 {
  from {
    opacity: 1;
  }
  to {
    opacity: 0;
    transform: scale3d($scaleSize, $scaleSize, $scaleSize);
  }
}

.main-panel .zoomOut {
  animation-name: zoomOut95;
}
</style>
