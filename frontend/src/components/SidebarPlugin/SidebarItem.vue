<template>
  <component
    :is="baseComponent"
    :to="link.path ? link.path : '/'"
    :class="{ active: isActive }"
    tag="li"
  >
    <a
      v-if="isMenu"
      class="sidebar-menu-item"
      :aria-expanded="!collapsed"
      data-toggle="collapse"
      @click.prevent="collapseMenu"
    >
      <template v-if="addLink">
        <span class="sidebar-mini-icon">{{ linkPrefix }}</span>
        <span class="sidebar-normal">
          {{ link.name }} <b class="caret"></b>
        </span>
      </template>
      <template v-else>
        <i :class="link.icon"></i>
        <p>{{ link.name }} <b class="caret"></b></p>
      </template>
    </a>
    <router-link
      v-else-if="link.path"
      :to="link.path"
      :class="{ active: link.active }"
      :target="link.target"
      :href="link.path"
    >
      <template v-if="addLink">
        <span class="sidebar-mini-icon">{{ linkPrefix }}</span>
        <span class="sidebar-normal">{{ link.name }}</span>
      </template>
      <template v-else>
        <i :class="link.icon"></i>
        <p>{{ link.name }}</p>
      </template>
    </router-link>
    <div v-else>
      <template v-if="addLink">
        <span class="sidebar-mini-icon">{{ linkPrefix }}</span>
        <span class="sidebar-normal">{{ link.name }}</span>
      </template>
      <template v-else>
        <i :class="link.icon"></i>
        <p>{{ link.name }}</p>
      </template>
    </div>
    <CollapseTransition :show="isMenu && !collapsed">
      <ul v-if="isMenu" class="sidebar-menu-item">
        <sidebar-item
          v-for="(subLink, subIndex) in link.children"
          :key="subLink.name + subIndex"
          :link="subLink"
        />
      </ul>
    </CollapseTransition>
  </component>
</template>

<script setup>
import { ref, computed, inject, onMounted, onBeforeUnmount } from "vue";
import { useRoute } from "vue-router";
import { useSidebarStore } from "@/stores/sidebar.js";
import { CollapseTransition } from "./transitions.js";

const props = defineProps({
  menu: {
    type: Boolean,
    default: false,
    description:
      "Whether the item is a menu. Most of the item it's not used and should be used only if you want to override the default behavior.",
  },
  link: {
    type: Object,
    default: () => ({
      name: "",
      path: "",
      children: [],
    }),
    description:
      "Sidebar link. Can contain name, path, icon and other attributes. See examples for more info",
  },
});

const collapsed = ref(true);
const route = useRoute();
const sidebarStore = useSidebarStore();

const addLink = inject("addLink", null);
const autoClose = inject("autoClose", true);

const baseComponent = computed(() => "li");

const linkPrefix = computed(() => {
  if (props.link.name) {
    const words = props.link.name.split(" ");
    return words.map((word) => word.substring(0, 1)).join("");
  }
  return false;
});

function elementType(link, isParent = true) {
  if (link.isRoute === false) {
    return isParent ? "li" : "a";
  } else {
    return "router-link";
  }
}

const linkAbbreviation = (name) => {
  const matches = name.match(/\b(\w)/g);
  return matches ? matches.join("") : "";
};

const isMenu = computed(() => {
  return (
    (props.link.children && props.link.children.length > 0) ||
    props.menu === true
  );
});

const isActive = computed(() => {
  if (route.path) {
    const matchingRoute = route.path.startsWith(props.link.path);
    if (matchingRoute) {
      return true;
    }
    if (props.link.children) {
      return props.link.children.some((child) =>
        route.path.startsWith(child.path)
      );
    }
  }
  return false;
});

function linkClick() {
  if (autoClose && sidebarStore.showSidebar) {
    sidebarStore.setShowSidebar(false);
  }
}

function collapseMenu() {
  collapsed.value = !collapsed.value;
}

onMounted(() => {
  if (props.link.collapsed !== undefined) {
    collapsed.value = props.link.collapsed;
  }
  if (isActive.value && isMenu.value) {
    collapsed.value = false;
  }
});
</script>

<style scoped>
.sidebar-menu-item {
  cursor: pointer;
}
</style>
