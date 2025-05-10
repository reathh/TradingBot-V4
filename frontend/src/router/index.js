import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "@/stores/auth";

// Layouts
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import AuthLayout from "@/layouts/AuthLayout.vue";

// Pages
import Dashboard from "@/pages/Dashboard/Dashboard.vue";
import Login from "@/pages/Login.vue";
import Register from "@/pages/Register.vue";
import ChartsPage from "@/pages/Charts.vue";
import UserProfile from "@/pages/UserProfile.vue";
import Pricing from "@/pages/Pricing.vue";
import Bots from "@/pages/Bots.vue";
import BotDetails from "@/pages/BotDetails.vue";
import Trades from "@/pages/Trades.vue";

// Tables
import RegularTables from "@/pages/Tables/RegularTables.vue";
import ExtendedTables from "@/pages/Tables/ExtendedTables.vue";
import PaginatedTables from "@/pages/Tables/PaginatedTables.vue";

// Forms
import RegularForms from "@/pages/Forms/RegularForms.vue";
import ExtendedForms from "@/pages/Forms/ExtendedForms.vue";
import ValidationForms from "@/pages/Forms/ValidationForms.vue";

const routes = [
  {
    path: "/",
    component: DashboardLayout,
    redirect: "/dashboard",
    children: [
      {
        path: "dashboard",
        name: "Dashboard",
        component: Dashboard,
        meta: { requiresAuth: true },
      },
      {
        path: "bots",
        name: "Bots",
        component: Bots,
        meta: { requiresAuth: true },
      },
      {
        path: "bot/:id",
        name: "BotDetails",
        component: BotDetails,
        meta: { requiresAuth: true },
      },
      {
        path: "trades",
        name: "Trades",
        component: Trades,
        meta: { requiresAuth: true },
      },
      {
        path: "pages/user",
        name: "UserProfile",
        component: UserProfile,
        meta: { requiresAuth: true },
      },
      {
        path: "charts",
        name: "Charts",
        component: ChartsPage,
        meta: { requiresAuth: true },
      },
    ],
  },
  {
    path: "/pages",
    component: AuthLayout,
    redirect: "/pages/login",
    children: [
      {
        path: "login",
        name: "Login",
        component: Login,
        meta: { guestOnly: true },
      },
      {
        path: "register",
        name: "Register",
        component: Register,
        meta: { guestOnly: true },
      },
      {
        path: "pricing",
        name: "Pricing",
        component: Pricing,
        meta: { guestOnly: true },
      },
    ],
  },
  {
    path: "/forms",
    component: DashboardLayout,
    redirect: "/forms/regular",
    name: "Forms",
    children: [
      {
        path: "regular",
        name: "Regular Forms",
        components: { default: RegularForms },
      },
      {
        path: "extended",
        name: "Extended Forms",
        components: { default: ExtendedForms },
      },
      {
        path: "validation",
        name: "Validation Forms",
        components: { default: ValidationForms },
      },
    ],
  },
  {
    path: "/table-list",
    component: DashboardLayout,
    redirect: "/table-list/regular",
    name: "Tables",
    children: [
      {
        path: "regular",
        name: "Regular Tables",
        components: { default: RegularTables },
      },
      {
        path: "extended",
        name: "Extended Tables",
        components: { default: ExtendedTables },
      },
      {
        path: "paginated",
        name: "Paginated Tables",
        components: { default: PaginatedTables },
      },
    ],
  },
  {
    path: "/:pathMatch(.*)*",
    redirect: "/",
  },
];

const router = createRouter( {
  history: createWebHistory(),
  routes,
  linkActiveClass: "active",
} );

router.beforeEach( ( to, from, next ) =>
{
  const authStore = useAuthStore();
  const requiresAuth = to.matched.some( ( record ) => record.meta.requiresAuth );
  const guestOnly = to.matched.some( ( record ) => record.meta.guestOnly );
  next();
  // if (requiresAuth && !authStore.isAuthenticated) {
  //   next("/auth/login");
  // } else if (guestOnly && authStore.isAuthenticated) {
  //   next("/dashboard");
  // } else {
  //   next();
  // }
} );

export default router;
