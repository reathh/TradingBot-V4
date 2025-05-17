<template>
  <el-tooltip
    content="Sidebar toggle"
    effect="light"
    :open-delay="300"
    placement="right"
  >
    <button
      class="minimize-sidebar btn btn-link btn-just-icon"
      @click="minimizeSidebar"
      rel="tooltip"
      data-original-title="Sidebar toggle"
      data-placement="right"
    >
      <i class="tim-icons icon-align-center visible-on-sidebar-regular"></i>
      <i class="tim-icons icon-bullet-list-67 visible-on-sidebar-mini"></i>
    </button>
  </el-tooltip>
</template>

<script setup>
import { getCurrentInstance } from "vue";
import { useSidebarStore } from "@/stores/sidebar";

const { proxy } = getCurrentInstance();
const sidebarStore = useSidebarStore();

function minimizeSidebar() {
  const isMinimized = sidebarStore.isMinimized;
  const status = isMinimized ? "deactivated" : "activated";

  proxy.$notify({
    type: "primary",
    message: `Sidebar mini ${status}...`,
    icon: "tim-icons icon-bell-55",
  });

  sidebarStore.toggleMinimize();
}
</script>

<style scoped>
/* Add any custom styles here */
</style>
