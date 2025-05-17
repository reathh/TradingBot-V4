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
      <div class="content" @click="sidebarStore.showSidebar ? sidebarStore.setShowSidebar(false) : null">
        <router-view v-slot="{ Component }">
          <fade-in-out :duration="200" mode="out-in">
            <component :is="Component" />
          </fade-in-out>
        </router-view>
      </div>

      <!-- SignalR connection status indicator -->
      <div v-if="!signalRConnected" class="signalr-indicator">
        <i class="tim-icons icon-simple-remove"></i> Reconnecting...
      </div>
    </div>

    <Notifications />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from "vue";
import { useSidebarStore } from "@/stores/sidebar";
import { useThemeStore } from "@/stores/theme";
import { useAuthStore } from "@/stores/auth";
import DashboardNavbar from "./DashboardNavbar.vue";
import SideBar from "./SideBar.vue";
import Notifications from "@/components/Notifications/Notifications.vue";
import SidebarFixedToggleButton from "./SidebarFixedToggleButton.vue";
// import SidebarShare from "./SidebarSharePlugin.vue";
import { FadeInOut } from "vue3-transitions";
import signalrService from "@/services/signalrService";

const sidebarStore = useSidebarStore();
const themeStore = useThemeStore();
const authStore = useAuthStore();
const sidebarBackground = ref("vue");
const title = ref("DASHBOARD");
const shortTitle = ref("DB");

// SignalR connection status
const signalRConnected = computed(() => signalrService.isConnected.value);

// Initialize SignalR connection on component mount
onMounted(async () => {
  try {
    await signalrService.start();
  } catch (error) {
    console.error("Failed to start SignalR connection:", error);
  }
});

// Stop SignalR connection when component is unmounted
onBeforeUnmount(async () => {
  await signalrService.stop();
});

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
const menuItems = computed(() => [
  {
    name: "Dashboard",
    icon: "tim-icons icon-chart-pie-36",
    path: "/dashboard",
  },
  {
    name: "Logout",
    icon: "tim-icons icon-button-power",
    click: () => authStore.logout(),
    position: "bottom"
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

.signalr-indicator {
  position: fixed;
  bottom: 20px;
  right: 20px;
  background-color: rgba(231, 76, 60, 0.9);
  color: white;
  padding: 8px 16px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 20px 0 rgba(0, 0, 0, 0.14);
  z-index: 1000;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    opacity: 0.7;
  }
  50% {
    opacity: 1;
  }
  100% {
    opacity: 0.7;
  }
}
</style>
