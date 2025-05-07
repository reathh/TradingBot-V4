<template>
  <component
    :is="tag"
    :type="tag === 'button' ? nativeType : ''"
    :disabled="disabled || loading"
    @click="handleClick"
    class="btn"
    :class="[
      { 'btn-round': round },
      { 'btn-block': block },
      { 'btn-wd': wide },
      { 'btn-icon btn-fab': icon },
      { [`btn-${type}`]: type },
      { [`btn-${size}`]: size },
      { 'btn-simple': simple },
      { 'btn-link': link },
      { disabled: disabled && tag !== 'button' },
    ]"
  >
    <slot name="loading">
      <i v-if="loading" class="fas fa-spinner fa-spin"></i>
    </slot>
    <slot></slot>
  </component>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";

const props = defineProps({
  tag: {
    type: String,
    default: "button",
  },
  round: Boolean,
  icon: Boolean,
  block: Boolean,
  loading: Boolean,
  wide: Boolean,
  disabled: Boolean,
  type: {
    type: String,
    default: "default",
  },
  nativeType: {
    type: String,
    default: "button",
  },
  size: {
    type: String,
    default: "",
  },
  simple: Boolean,
  link: Boolean,
});

const emit = defineEmits(["click"]);

function handleClick(evt) {
  emit("click", evt);
}
</script>

<style scoped lang="scss">
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;

  i {
    padding: 0 3px;
  }
}
</style>
