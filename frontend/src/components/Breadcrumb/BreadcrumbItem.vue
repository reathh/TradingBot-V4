<template>
  <li class="breadcrumb-item" :class="{ active }">
    <slot></slot>
  </li>
</template>

<script setup>
defineProps({
  active: {
    type: Boolean,
    default: false,
    description: "Whether breadcrumb item is active",
  },
});
</script>

<style scoped></style>
