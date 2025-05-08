<template>
  <div class="sidebar" :data="backgroundColor">
    <div class="sidebar-wrapper" ref="sidebarScrollArea">
      <div class="logo">
        <router-link to="/" class="simple-text logo-mini">
          <img :src="logo" alt="app-logo" />
        </router-link>
        <router-link to="/" class="simple-text logo-normal">
          {{ title }}
        </router-link>
      </div>
      <slot> </slot>
      <ul class="nav">
        <SidebarItem
          v-for="(link, index) in sidebarLinks"
          :key="link.name + index"
          :link="link"
        >
          <SidebarItem
            v-for="(subLink, subIndex) in link.children"
            :key="subLink.name + subIndex"
            :link="subLink"
          />
        </SidebarItem>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { ref, provide, onBeforeUnmount } from "vue";
import SidebarItem from "./SidebarItem.vue";
import { useSidebarStore } from "@/stores/sidebar.js";

const props = defineProps({
  title: {
    type: String,
    default: "Creative Tim",
    description: "Sidebar title",
  },
  shortTitle: {
    type: String,
    default: "CT",
    description: "Sidebar short title",
  },
  logo: {
    type: String,
    default: "img/icon-vue.png",
    description: "Sidebar app logo",
  },
  backgroundColor: {
    type: String,
    default: "vue",
    validator: (value) => {
      const acceptedValues = [
        "",
        "vue",
        "blue",
        "green",
        "orange",
        "red",
        "primary",
      ];
      return acceptedValues.includes(value);
    },
    description: "Sidebar background color (vue|blue|green|orange|red|primary)",
  },
  sidebarLinks: {
    type: Array,
    default: () => [],
    description:
      "List of sidebar links as an array if you don't want to use components for these.",
  },
  autoClose: {
    type: Boolean,
    default: true,
    description:
      "Whether sidebar should autoclose on mobile when clicking an item",
  },
});

const sidebarScrollArea = ref(null);
const sidebarStore = useSidebarStore();

provide("autoClose", props.autoClose);

const minimizeSidebar = () => {
  sidebarStore.toggleMinimize();
};

onBeforeUnmount(() => {
  if (sidebarStore.showSidebar) {
    sidebarStore.setShowSidebar(false);
  }
});
</script>

<style scoped>
@media (min-width: 992px) {
  .navbar-search-form-mobile,
  .nav-mobile-menu {
    display: none;
  }
}
</style>
