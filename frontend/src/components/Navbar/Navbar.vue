<template>
  <nav class="navbar">
    <div class="navbar-wrapper">
      <div class="navbar-toggle">
        <button @click="toggleSidebar" class="toggle-btn">
          <i class="fas fa-bars"></i>
        </button>
      </div>
      <div class="navbar-brand">
        <h4>{{ currentRouteName }}</h4>
      </div>
      <div class="navbar-items">
        <div class="navbar-search">
          <div class="search-box">
            <i class="fas fa-search"></i>
            <input type="text" placeholder="Search..." />
          </div>
        </div>
        <div class="navbar-actions">
          <div class="action-item">
            <a href="javascript:void(0)" class="nav-link">
              <i class="fas fa-bell"></i>
              <span class="badge">3</span>
            </a>
          </div>
          <div class="action-item">
            <a href="javascript:void(0)" class="nav-link">
              <i class="fas fa-cog"></i>
            </a>
          </div>
          <div class="action-item user-dropdown">
            <div class="nav-link" @click="toggleUserMenu">
              <div class="avatar">
                <img src="https://ui-avatars.com/api/?name=John+Doe&background=0D8ABC&color=fff" alt="User" />
              </div>
            </div>
            <div class="dropdown-menu" v-if="showUserMenu">
              <div class="dropdown-item">
                <i class="fas fa-user"></i> Profile
              </div>
              <div class="dropdown-item">
                <i class="fas fa-cog"></i> Settings
              </div>
              <div class="dropdown-divider"></div>
              <div class="dropdown-item" @click="logout">
                <i class="fas fa-sign-out-alt"></i> Logout
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const showUserMenu = ref(false);
const sidebarVisible = ref(true);

const currentRouteName = computed(() => {
  return route.name || 'Dashboard';
});

const toggleUserMenu = () => {
  showUserMenu.value = !showUserMenu.value;
};

const toggleSidebar = () => {
  sidebarVisible.value = !sidebarVisible.value;
  const event = new CustomEvent('toggle-sidebar', { detail: { visible: sidebarVisible.value } });
  window.dispatchEvent(event);
};

const logout = () => {
  authStore.logout();
};

// Click outside to close user menu
document.addEventListener('click', (event) => {
  const dropdown = document.querySelector('.user-dropdown');
  if (dropdown && !dropdown.contains(event.target)) {
    showUserMenu.value = false;
  }
});
</script>

<style lang="scss" scoped>
.navbar {
  position: relative;
  height: 70px;
  display: flex;
  align-items: center;
  padding: 0 30px;
  z-index: 3;
  background-color: #27293d;
  box-shadow: 0 10px 25px 0 rgba(0, 0, 0, 0.3);

  .navbar-wrapper {
    display: flex;
    align-items: center;
    width: 100%;

    .navbar-toggle {
      display: none;

      @media (max-width: 991px) {
        display: flex;
        margin-right: 15px;
      }

      .toggle-btn {
        background: transparent;
        border: none;
        color: white;
        font-size: 1.4em;
        cursor: pointer;
        
        &:focus {
          outline: none;
        }
      }
    }

    .navbar-brand {
      h4 {
        margin: 0;
        color: white;
        font-weight: 400;
      }
    }

    .navbar-items {
      margin-left: auto;
      display: flex;
      align-items: center;

      .navbar-search {
        margin-right: 20px;

        .search-box {
          position: relative;
          display: flex;
          align-items: center;
          background: #1e1e2f;
          border-radius: 20px;
          padding: 5px 15px;
          width: 240px;

          i {
            color: rgba(255, 255, 255, 0.5);
            margin-right: 10px;
          }

          input {
            background: transparent;
            border: none;
            color: white;
            padding: 5px 0;
            width: 100%;

            &:focus {
              outline: none;
            }

            &::placeholder {
              color: rgba(255, 255, 255, 0.5);
            }
          }

          @media (max-width: 767px) {
            display: none;
          }
        }
      }

      .navbar-actions {
        display: flex;
        align-items: center;

        .action-item {
          position: relative;
          margin-left: 15px;

          .nav-link {
            color: white;
            padding: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            text-decoration: none;
            
            i {
              font-size: 1.2em;
            }

            .badge {
              position: absolute;
              top: 2px;
              right: 2px;
              padding: 3px 5px;
              border-radius: 50%;
              background: #FF5252;
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

              img {
                width: 100%;
                height: 100%;
                object-fit: cover;
              }
            }
          }

          &.user-dropdown {
            position: relative;

            .dropdown-menu {
              position: absolute;
              right: 0;
              top: calc(100% + 10px);
              background: #27293d;
              border-radius: 5px;
              min-width: 200px;
              box-shadow: 0 5px 25px rgba(0, 0, 0, 0.3);
              z-index: 10;

              .dropdown-item {
                padding: 12px 20px;
                color: rgba(255, 255, 255, 0.7);
                cursor: pointer;
                transition: all 0.3s;

                i {
                  margin-right: 15px;
                  width: 20px;
                  text-align: center;
                }

                &:hover {
                  background: rgba(255, 255, 255, 0.1);
                  color: white;
                }
              }

              .dropdown-divider {
                height: 1px;
                background: rgba(255, 255, 255, 0.1);
                margin: 5px 0;
              }
            }
          }
        }
      }
    }
  }
}
</style>
