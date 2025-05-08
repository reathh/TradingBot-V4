import { ref, reactive, provide, inject } from "vue";
import SideBar from "./SideBar.vue";
import SidebarItem from "./SidebarItem.vue";

const sidebarStore = reactive({
  showSidebar: false,
  sidebarLinks: [],
  isMinimized: false,
  displaySidebar(value) {
    this.showSidebar = value;
  },
  toggleMinimize() {
    document.body.classList.toggle("sidebar-mini");

    const simulateWindowResize = setInterval(() => {
      window.dispatchEvent(new Event("resize"));
    }, 180);

    setTimeout(() => {
      clearInterval(simulateWindowResize);
    }, 1000);

    this.isMinimized = !this.isMinimized;
  },
});

const SidebarPlugin = {
  install(app, options) {
    if (options && options.sidebarLinks) {
      sidebarStore.sidebarLinks.push(...options.sidebarLinks);
    }

    app.provide("sidebar", sidebarStore);

    app.config.globalProperties.$sidebar = sidebarStore;

    app.component("side-bar", SideBar);
    app.component("sidebar-item", SidebarItem);
  },
};

export function useSidebar() {
  return inject("sidebar");
}

export default SidebarPlugin;
