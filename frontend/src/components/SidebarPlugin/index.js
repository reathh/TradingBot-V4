import Sidebar from "./SideBar.vue";
import SidebarItem from "./SidebarItem.vue";

export default {
  install: (app, options) => {
    app.component("side-bar", Sidebar);
    app.component("sidebar-item", SidebarItem);
  },
};
