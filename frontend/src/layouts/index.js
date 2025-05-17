import SidebarItem from "./SidebarItem.vue";
import SideBar from "./SideBar.vue";
import { useSidebarStore } from "@/stores/sidebar.js";

// Add a useSidebar function to get the sidebar state
export function useSidebar() {
  const sidebarStore = useSidebarStore();
  return sidebarStore;
}

const SidebarPlugin = {
  install(app) {
    app.component("sidebar-item", SidebarItem);
    app.component("side-bar", SideBar);
  },
};

export default SidebarPlugin;
export { SidebarItem, SideBar }; 