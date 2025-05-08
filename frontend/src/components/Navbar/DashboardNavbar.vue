<template>
  <nav class="navbar">
    <div class="navbar-wrapper">
      <div class="navbar-brand">
        <div class="navbar-toggle d-inline" @click="toggleSidebar">
          <button type="button" class="navbar-toggler">
            <span class="navbar-toggler-bar bar1"></span>
            <span class="navbar-toggler-bar bar2"></span>
            <span class="navbar-toggler-bar bar3"></span>
          </button>
        </div>
        <h4>{{ currentRouteName }}</h4>
      </div>
      <div class="navbar-items">
        <div class="navbar-search">
          <div class="search-box">
            <i class="tim-icons icon-zoom-split"></i>
            <input type="text" placeholder="Search..." />
          </div>
        </div>
        <div class="navbar-actions">
          <div class="action-item">
            <a href="#" class="nav-link">
              <i class="tim-icons icon-bell-55"></i>
              <span class="badge" v-if="notificationCount > 0">{{
                notificationCount
              }}</span>
            </a>
          </div>
          <div class="action-item">
            <a href="#" class="nav-link">
              <i class="tim-icons icon-settings"></i>
            </a>
          </div>
          <div class="action-item user-dropdown">
            <div class="nav-link" @click="toggleUserMenu">
              <div class="avatar">
                <img :src="userAvatar" alt="User" />
              </div>
            </div>
            <div class="dropdown-menu" v-if="showUserMenu">
              <div class="dropdown-item">
                <i class="tim-icons icon-single-02"></i> Profile
              </div>
              <div class="dropdown-item">
                <i class="tim-icons icon-settings"></i> Settings
              </div>
              <div class="dropdown-divider"></div>
              <div class="dropdown-item" @click="logout">
                <i class="tim-icons icon-button-power"></i> Logout
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { useSidebarStore } from "@/stores/sidebar";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const sidebarStore = useSidebarStore();

const showUserMenu = ref(false);
const notificationCount = ref(3);

const userAvatar = computed(() => {
  return "https://ui-avatars.com/api/?name=John+Doe&background=0D8ABC&color=fff";
});

const currentRouteName = computed(() => {
  return route.name || "Dashboard";
});

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value;
};

const logout = () => {
  authStore.logout();
};

const toggleSidebar = () => {
  sidebarStore.displaySidebar(!sidebarStore.showSidebar);
};

// Click outside to close user menu
const closeUserMenuOnClickOutside = (event) => {
  const dropdown = document.querySelector(".user-dropdown");
  if (dropdown && !dropdown.contains(event.target)) {
    showUserMenu.value = false;
  }
};

onMounted(() => {
  document.addEventListener("click", closeUserMenuOnClickOutside);
});

onUnmounted(() => {
  document.removeEventListener("click", closeUserMenuOnClickOutside);
});
</script>

<style scoped>
.navbar {
  height: 0px;
  position: relative;
  display: flex;
  align-items: center;
  padding: 0 30px;
  z-index: 2;
  box-shadow: 0 10px 25px 0 rgba(0, 0, 0, 0.3);
  background: linear-gradient(
    to bottom,
    #1a1e34,
    #1f2437
  ) !important; /* Match original template gradient */
}

.navbar-wrapper {
  display: flex;
  align-items: center;
  width: 100%;
}

.navbar-brand {
  display: flex;
  align-items: center;
}

.navbar-brand h4 {
  margin: 0;
  color: white;
  font-weight: 400;
}

.navbar-toggle {
  margin-right: 15px;
  cursor: pointer;
}

.navbar-toggler {
  padding: 0.25rem 0.75rem;
  font-size: 0.99925rem;
  line-height: 1;
  background-color: transparent;
  border: 1px solid transparent;
  border-radius: 0.2857rem;
  cursor: pointer;
}

.navbar-toggler-bar {
  display: block;
  position: relative;
  width: 22px;
  height: 1px;
  border-radius: 1px;
  background: white;
  transition: all 0.2s;
}

.navbar-toggler-bar + .navbar-toggler-bar {
  margin-top: 7px;
}

.navbar-toggler:hover .navbar-toggler-bar {
  background: rgba(255, 255, 255, 0.8);
}

.navbar-items {
  margin-left: auto;
  display: flex;
  align-items: center;
}

.navbar-search {
  margin-right: 20px;
}

.search-box {
  position: relative;
  display: flex;
  align-items: center;
  background: #1e1e2f;
  border-radius: 20px;
  padding: 5px 15px;
  width: 240px;
}

.search-box i {
  color: rgba(255, 255, 255, 0.5);
  margin-right: 10px;
}

.search-box input {
  background: transparent;
  border: none;
  color: white;
  padding: 5px 0;
  width: 100%;
}

.search-box input:focus {
  outline: none;
}

.search-box input::placeholder {
  color: rgba(255, 255, 255, 0.5);
}

.navbar-actions {
  display: flex;
  align-items: center;
}

.action-item {
  position: relative;
  margin-left: 15px;
}

.action-item .nav-link {
  color: white;
  padding: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  text-decoration: none;
}

.action-item .nav-link i {
  font-size: 1.2em;
}

.action-item .badge {
  position: absolute;
  top: 2px;
  right: 2px;
  padding: 3px 5px;
  border-radius: 50%;
  background: #ff5252;
  color: white;
  font-size: 10px;
  min-width: 15px;
  height: 15px;
  line-height: 10px;
  text-align: center;
}

.avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  overflow: hidden;
}

.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.user-dropdown {
  position: relative;
}

.dropdown-menu {
  position: absolute;
  right: 0;
  top: calc(100% + 10px);
  background: #27293d;
  border-radius: 5px;
  min-width: 200px;
  box-shadow: 0 5px 25px rgba(0, 0, 0, 0.3);
  z-index: 10;
}

.dropdown-item {
  padding: 12px 20px;
  color: rgba(255, 255, 255, 0.7);
  cursor: pointer;
  transition: all 0.3s;
}

.dropdown-item i {
  margin-right: 15px;
  width: 20px;
  text-align: center;
}

.dropdown-item:hover {
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

.dropdown-divider {
  height: 1px;
  background: rgba(255, 255, 255, 0.1);
  margin: 5px 0;
}

@media (max-width: 767px) {
  .search-box {
    display: none;
  }
}
</style>
