<template>
  <BreadCrumb>
    <BreadCrumbItem
      v-for="(route, index) in matchedRoutes"
      :key="route.name"
      style="display: inline-block"
    >
      <router-link
        v-if="index < matchedRoutes.length - 1"
        :to="{ name: route.name }"
        class="breadcrumb-link"
      >
        {{ route.name }}
      </router-link>
      <span v-else class="breadcrumb-current">{{ route.name }}</span>
    </BreadCrumbItem>
  </BreadCrumb>
</template>

<script setup>
import { useRoute } from "vue-router";
import BreadCrumb from "./Breadcrumb.vue";
import BreadCrumbItem from "./BreadcrumbItem.vue";

const route = useRoute();
const matchedRoutes = route.matched.slice();
</script>

<style scoped></style>
