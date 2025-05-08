import { createApp } from "vue";
import { createPinia } from "pinia";
import router from "./router";
import App from "./App.vue";
import NotificationPlugin from "./components/Notifications/NotificationPlugin";
import SidebarItem from "./components/SidebarPlugin/SidebarItem.vue";
import i18n from "./i18n";
import Vue3Transitions from "vue3-transitions";
import RTLPlugin from "./plugins/RTLPlugin";
import SidebarPlugin from "@/components/SidebarPlugin/index.js";
import ClickOutsideDirective from "./directives/click-outside";
// Styles
import "./assets/sass/black-dashboard-pro.scss";
import "./assets/css/nucleo-icons.css";
import ElementPlus from "element-plus";
import "element-plus/dist/index.css";

// PWA
import { registerSW } from "virtual:pwa-register";

// Register service worker
registerSW({ immediate: true });

// Create Vue app
const app = createApp(App);

// Register plugins
app.use(createPinia());
app.use(router);
app.use(ElementPlus);
app.use(Vue3Transitions);
app.use(RTLPlugin);
app.use(ClickOutsideDirective);
app.use(SidebarPlugin);
app.use(NotificationPlugin);

// Initialize the sidebar in mini mode - this class controls the full sidebar behavior
document.body.classList.add("sidebar-mini");

app.mount("#app");
