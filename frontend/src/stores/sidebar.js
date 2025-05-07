// store/sidebar.js (using Pinia)
import { defineStore } from "pinia";

export const useSidebarStore = defineStore("sidebar", {
  state: () => ({
    showSidebar: false,
    isMinimized: false,
  }),
  actions: {
    setShowSidebar(value) {
      this.showSidebar = value;
    },
    toggleMinimize() {
      document.body.classList.toggle("sidebar-mini");
      // we simulate the window Resize so the charts will get updated in realtime.
      const simulateWindowResize = setInterval(() => {
        window.dispatchEvent(new Event("resize"));
      }, 180);

      // we stop the simulation of Window Resize after the animations are completed
      setTimeout(() => {
        clearInterval(simulateWindowResize);
      }, 1000);

      this.isMinimized = !this.isMinimized;
    },
  },
});
