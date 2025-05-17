<template>
  <div class="wrapper" :class="{ 'nav-open': $sidebar.showSidebar }">
    <notifications></notifications>
    <sidebar-fixed-toggle-button />
    <side-bar
      :background-color="sidebarBackground"
      :short-title="shortTitle"
      :title="title"
    >
      <template slot="links">
        <sidebar-item
          :link="{
            name: 'Dashboard',
            icon: 'tim-icons icon-chart-pie-36',
            path: '/dashboard'
          }"
        >
        </sidebar-item>
        <sidebar-item
          :link="{ name: 'Pages', icon: 'tim-icons icon-image-02' }"
        >
          <sidebar-item
            :link="{ name: 'Pricing', path: '/pricing' }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'RTL', path: '/pages/rtl' }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Timeline', path: '/pages/timeline' }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Login', path: '/login' }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Register', path: '/register' }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Lock', path: '/lock' }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'User Profile', path: '/pages/user' }"
          ></sidebar-item>
        </sidebar-item>
        <sidebar-item
          :link="{
            name: 'Components',
            icon: 'tim-icons icon-molecule-40'
          }"
        >
          <sidebar-item :link="{ name: 'Multi Level Collapse' }">
            <sidebar-item
              :link="{
                name: 'Example',
                isRoute: false,
                path: 'https://google.com',
                target: '_blank'
              }"
            ></sidebar-item>
          </sidebar-item>

          <sidebar-item
            :link="{ name: 'Buttons', path: '/components/buttons' }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Grid System',
              path: '/components/grid-system'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Panels', path: '/components/panels' }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Sweet Alert',
              path: '/components/sweet-alert'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Notifications',
              path: '/components/notifications'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Icons', path: '/components/icons' }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Typography',
              path: '/components/typography'
            }"
          ></sidebar-item>
        </sidebar-item>
        <sidebar-item
          :link="{ name: 'Forms', icon: 'tim-icons icon-notes' }"
        >
          <sidebar-item
            :link="{ name: 'Regular Forms', path: '/forms/regular' }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Extended Forms',
              path: '/forms/extended'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Validation Forms',
              path: '/forms/validation'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Wizard', path: '/forms/wizard' }" 
          ></sidebar-item>
        </sidebar-item>
        <sidebar-item
          :link="{
            name: 'Tables',
            icon: 'tim-icons icon-puzzle-10'
          }"
        >
          <sidebar-item
            :link="{
              name: 'Regular Tables',
              path: '/table-list/regular'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Extended Tables',
              path: '/table-list/extended'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Paginated Tables',
              path: '/table-list/paginated'
            }"
          ></sidebar-item>
        </sidebar-item>
        <sidebar-item
          :link="{ name: 'Maps', icon: 'tim-icons icon-pin' }"
        >
          <sidebar-item
            :link="{ name: 'Google Maps', path: '/maps/google' }"
          ></sidebar-item>
          <sidebar-item
            :link="{
              name: 'Full Screen Maps',
              path: '/maps/full-screen'
            }"
          ></sidebar-item>
          <sidebar-item
            :link="{ name: 'Vector Maps', path: '/maps/vector-map' }"
          ></sidebar-item>
        </sidebar-item>
        <sidebar-item
          :link="{
            name: 'Widgets',
            icon: 'tim-icons icon-settings',
            path: '/widgets'
          }"
        ></sidebar-item>
        <sidebar-item
          :link="{
            name: 'Charts',
            icon: 'tim-icons icon-chart-bar-32',
            path: '/charts'
          }"
        ></sidebar-item>
        <sidebar-item
          :link="{
            name: 'Calendar',
            icon: 'tim-icons icon-time-alarm',
            path: '/calendar'
          }"
        ></sidebar-item>
      </template>
    </side-bar>
    <div class="main-panel" :data="sidebarBackground">
      <dashboard-navbar></dashboard-navbar>
      <router-view name="header"></router-view>

      <div
        :class="{ content: !route.meta.hideContent }"
        @click="toggleSidebar"
      >
        <router-view v-slot="{ Component }">
          <Transition name="zoom-center" mode="out-in">
            <component :is="Component" />
          </Transition>
        </router-view>
      </div>
      <content-footer v-if="!route.meta.hideFooter"></content-footer>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from "vue";
import { useRoute } from "vue-router";
import { useSidebarStore } from "@/stores/sidebar";
import { useThemeStore } from "@/stores/theme";
import DashboardNavbar from "./DashboardNavbar.vue";
import SideBar from "@/components/SidebarPlugin/SideBar.vue";
import Notifications from "@/components/Notifications/Notifications.vue";
import SidebarFixedToggleButton from "./SidebarFixedToggleButton.vue";
// import SidebarShare from "./SidebarSharePlugin.vue";
import { FadeInOut } from "vue3-transitions";
import signalrService from "@/services/signalrService";
import ContentFooter from "@/layouts/ContentFooter.vue";
import SidebarItem from "@/components/SidebarPlugin/SidebarItem.vue";

const route = useRoute();
const sidebarStore = useSidebarStore();
const themeStore = useThemeStore();
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
    name: "Trades",
    icon: "tim-icons icon-chart-bar-32",
    path: "/trades",
  },
  {
    name: "Orders",
    icon: "tim-icons icon-cart",
    path: "/orders",
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
    icon: "tim-icons icon-chart-pie-36",
    path: "/charts",
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

// Add zoom-center transition for router-view
.zoom-center-enter-active, .zoom-center-leave-active {
  transition: all 0.2s cubic-bezier(.55,0,.1,1);
}
.zoom-center-enter-from, .zoom-center-leave-to {
  opacity: 0;
  transform: scale(0.95);
}
.zoom-center-enter-to, .zoom-center-leave-from {
  opacity: 1;
  transform: scale(1);
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
