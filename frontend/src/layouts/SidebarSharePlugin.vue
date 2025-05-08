<template>
  <div class="fixed-plugin" v-click-outside="closeDropDown">
    <div class="dropdown show-dropdown" :class="{ show: isOpen }">
      <a class="settings-icon" @click="toggleDropDown">
        <i class="fa fa-cog fa-2x"></i>
      </a>

      <ul class="dropdown-menu" :class="{ show: isOpen }">
        <li class="header-title">Sidebar Background</li>
        <li class="adjustments-line">
          <a class="switch-trigger background-color">
            <div class="badge-colors text-center">
              <span
                v-for="item in sidebarColors"
                :key="item.color"
                class="badge filter"
                :class="[`badge-${item.color}`, { active: item.active }]"
                :data-color="item.color"
                @click="changeSidebarBackground(item)"
              ></span>
            </div>
            <div class="clearfix"></div>
          </a>
        </li>

        <li class="header-title">Sidebar Mini</li>
        <li class="adjustments-line">
          <div class="togglebutton switch-sidebar-mini">
            <span class="label-switch">OFF</span>
            <BaseSwitch
              v-model="sidebarMiniLocal"
              @update:modelValue="minimizeSidebar"
            />
            <span class="label-switch label-right">ON</span>
          </div>

          <div class="togglebutton switch-change-color mt-3">
            <span class="label-switch">LIGHT MODE</span>
            <BaseSwitch v-model="darkMode" @update:modelValue="toggleMode" />
            <span class="label-switch label-right">DARK MODE</span>
          </div>
        </li>

        <li class="button-container mt-4">
          <a
            href="https://demos.creative-tim.com/vue-black-dashboard-pro/documentation"
            target="_blank"
            rel="noopener"
            class="btn btn-default btn-block btn-round"
          >
            Documentation
          </a>
          <a
            href="https://creative-tim.com/product/vue-black-dashboard-pro"
            target="_blank"
            rel="noopener"
            class="btn btn-primary btn-block btn-round"
          >
            Buy for $59
          </a>
          <a
            href="https://demos.creative-tim.com/vue-black-dashboard"
            target="_blank"
            rel="noopener"
            class="btn btn-info btn-block btn-round"
          >
            Free Version
          </a>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from "vue";
import BaseSwitch from "@/components/BaseSwitch.vue";
import { useSidebar } from "@/components/SidebarPlugin/index.js";

const props = defineProps({
  backgroundColor: String,
});

const emit = defineEmits(["update:backgroundColor"]);

const isOpen = ref(false);
const sidebarMiniLocal = ref(false); // Use a local ref for the switch
const darkMode = ref(true);

const sidebarColors = ref([
  { color: "primary", active: false, value: "primary" },
  { color: "vue", active: true, value: "vue" },
  { color: "info", active: false, value: "blue" },
  { color: "success", active: false, value: "green" },
  { color: "warning", active: false, value: "orange" },
  { color: "danger", active: false, value: "red" },
]);

const sidebar = useSidebar();

function toggleDropDown() {
  isOpen.value = !isOpen.value;
}

function closeDropDown() {
  isOpen.value = false;
}

function toggleList(list, itemToActivate) {
  list.forEach((item) => {
    item.active = false;
  });
  itemToActivate.active = true;
}

function changeSidebarBackground(item) {
  emit("update:backgroundColor", item.value);
  toggleList(sidebarColors.value, item);
}

function toggleMode(isDark) {
  const docClasses = document.body.classList;
  if (isDark) {
    docClasses.remove("white-content");
  } else {
    docClasses.add("white-content");
  }
}

function minimizeSidebar(value) {
  sidebarMiniLocal.value = value;
  if (sidebar && typeof sidebar.toggleMinimize === "function") {
    sidebar.toggleMinimize();
  }
}
</script>

<style scoped lang="scss">
@import "@/assets/sass/dashboard/custom/_variables.scss";

.settings-icon {
  cursor: pointer;
}

.badge-vue {
  background-color: $vue;
}
</style>
