<template>
  <div>
    <BaseNav
      v-model:showMenu="showMenu"
      type="white"
      :transparent="true"
      menu-classes="justify-between w-full items-center"
      class="auth-navbar fixed-top"
    >
      <template #brand>
        <div class="navbar-wrapper">
          <a class="navbar-brand" href="#" v-if="title">{{ title }}</a>
        </div>
      </template>

      <ul class="navbar-nav flex items-center space-x-3">
        <li class="nav-item">
          <RouterLink to="/dashboard" class="nav-link text-primary">
            <i class="tim-icons icon-minimal-left"></i> Back to Dashboard
          </RouterLink>
        </li>
        <li class="nav-item">
          <RouterLink to="/register" class="nav-link">
            <i class="tim-icons icon-laptop"></i> Register
          </RouterLink>
        </li>
        <li class="nav-item">
          <RouterLink to="/login" class="nav-link">
            <i class="tim-icons icon-single-02"></i> Login
          </RouterLink>
        </li>
        <li class="nav-item">
          <RouterLink to="/pricing" class="nav-link">
            <i class="tim-icons icon-coins"></i> Pricing
          </RouterLink>
        </li>
        <li class="nav-item">
          <RouterLink to="/lock" class="nav-link">
            <i class="tim-icons icon-lock-circle"></i> Lock
          </RouterLink>
        </li>
      </ul>
    </BaseNav>

    <div class="wrapper wrapper-full-page">
      <div class="full-page" :class="pageClass">
        <div class="content">
          <Transition name="zoomIn" mode="out-in">
            <RouterView />
          </Transition>
        </div>
        <footer class="footer">
          <div class="container-fluid">
            <nav>
              <ul class="nav">
                <li class="nav-item">
                  <a
                    href="https://www.creative-tim.com"
                    target="_blank"
                    rel="noopener"
                    class="nav-link"
                  >
                    Creative Tim
                  </a>
                </li>
                <li class="nav-item">
                  <a
                    href="http://presentation.creative-tim.com"
                    target="_blank"
                    rel="noopener"
                    class="nav-link"
                  >
                    About Us
                  </a>
                </li>
                <li class="nav-item">
                  <a
                    href="http://blog.creative-tim.com"
                    target="_blank"
                    rel="noopener"
                    class="nav-link"
                  >
                    Blog
                  </a>
                </li>
              </ul>
            </nav>
            <div class="copyright">
              &copy; {{ year }}, made with
              <i class="tim-icons icon-heart-2"></i> by
              <a
                href="https://www.creative-tim.com/?ref=vbdp-vuejs"
                target="_blank"
                rel="noopener"
                >Creative Tim</a
              >
              for a better web.
            </div>
          </div>
        </footer>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import BaseNav from "@/components/Navbar/BaseNav.vue";

const showMenu = ref(false);
const year = new Date().getFullYear();
const route = useRoute();
const router = useRouter();
const pageClass = ref("login-page");

const title = computed(() => `${route.name ?? "Auth"} Page`);

const toggleNavbar = () => {
  document.body.classList.toggle("nav-open");
  showMenu.value = !showMenu.value;
};

const closeMenu = () => {
  document.body.classList.remove("nav-open");
  showMenu.value = false;
};

const setPageClass = () => {
  pageClass.value = `${route.name ?? "auth"}-page`.toLowerCase();
};

onMounted(() => {
  setPageClass();
});

watch(route, () => {
  setPageClass();
});
</script>

<style lang="scss">
.navbar.auth-navbar {
  top: 0;
}

$scaleSize: 0.8;

@keyframes zoomIn8 {
  from {
    opacity: 0;
    transform: scale3d($scaleSize, $scaleSize, $scaleSize);
  }
  to {
    opacity: 1;
  }
}

.zoomIn-enter-active {
  animation: zoomIn8 0.3s ease-out;
}

@keyframes zoomOut8 {
  from {
    opacity: 1;
  }
  to {
    opacity: 0;
    transform: scale3d($scaleSize, $scaleSize, $scaleSize);
  }
}

.zoomIn-leave-active {
  animation: zoomOut8 0.3s ease-in;
}
</style>
