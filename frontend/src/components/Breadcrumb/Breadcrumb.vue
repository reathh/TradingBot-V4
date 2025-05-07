<template>
  <ul class="breadcrumb" :class="{ 'bg-transparent': transparent }">
    <slot></slot>
  </ul>
</template>

<script setup>
defineProps({
  transparent: {
    type: Boolean,
    default: true,
  },
});
</script>

<style scoped></style>
