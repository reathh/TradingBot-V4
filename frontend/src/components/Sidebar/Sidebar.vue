<template>
  <div class="sidebar" data="green">
    <div class="sidebar-wrapper" ref="sidebarScrollArea">
      <div class="logo">
        <a href="#" class="simple-text logo-mini">
          <img src="img/icon-vue.png" alt="app-logo" />
        </a>
        <a href="#" class="simple-text logo-normal"> Trading Bot </a>
      </div>
      <slot></slot>
      <ul class="nav">
        <slot name="links">
          <sidebar-item
            v-for="(link, index) in sidebarLinks"
            :key="link.name + index"
            :link="link"
          >
            <sidebar-item
              v-for="(subLink, index) in link.children"
              :key="subLink.name + index"
              :link="subLink"
            >
            </sidebar-item>
          </sidebar-item>
        </slot>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import { useRoute } from "vue-router";

const route = useRoute();
const isCollapsed = ref(false);

const toggleCollapse = () => {
  isCollapsed.value = !isCollapsed.value;
};

const isLinkActive = (path) => {
  return route.path === path;
};

const navItems = ref([
  {
    name: "Dashboard",
    path: "/dashboard",
    icon: "fas fa-tachometer-alt",
  },
  {
    name: "Analytics",
    path: "/analytics",
    icon: "fas fa-chart-line",
  },
  {
    name: "Settings",
    path: "/settings",
    icon: "fas fa-cog",
  },
  {
    name: "Users",
    path: "/users",
    icon: "fas fa-users",
  },
  {
    name: "Reports",
    path: "/reports",
    icon: "fas fa-file-alt",
  },
  {
    name: "Notifications",
    path: "/notifications",
    icon: "fas fa-bell",
  },
  {
    name: "Help",
    path: "/help",
    icon: "fas fa-question-circle",
  },
  {
    name: "Charts",
    path: "/charts",
    icon: "fas fa-question-circle",
  },
]);
</script>

<style>
@media (min-width: 992px) {
  .navbar-search-form-mobile,
  .nav-mobile-menu {
    display: none;
  }
}
</style>
