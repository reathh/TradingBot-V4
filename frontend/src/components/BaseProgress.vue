<template>
  <div
    class="progress-container"
    :class="{
      [`progress-${type}`]: type,
      [`progress-${size}`]: size,
    }"
  >
    <span class="progress-badge" v-if="label">{{ label }}</span>
    <div class="progress">
      <span class="progress-value" v-if="showValue && valuePosition === 'left'">
        {{ value }}%
      </span>
      <div
        class="progress-bar"
        :class="computedClasses"
        role="progressbar"
        :aria-valuenow="value"
        aria-valuemin="0"
        aria-valuemax="100"
        :style="`width: ${value}%;`"
      >
        <slot>
          <span
            v-if="showValue && valuePosition === 'right'"
            class="progress-value"
          >
            {{ value }}%
          </span>
        </slot>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps } from "vue";

const props = defineProps({
  striped: Boolean,
  showValue: {
    type: Boolean,
    default: true,
  },
  animated: Boolean,
  label: String,
  valuePosition: {
    type: String,
    default: "left", // left | right
  },
  height: {
    type: Number,
    default: 1,
  },
  type: {
    type: String,
    default: "default",
  },
  size: {
    type: String,
    default: "sm",
  },
  value: {
    type: Number,
    default: 0,
    validator: (value) => value >= 0 && value <= 100,
  },
});

// Computed property for progress-bar classes
const computedClasses = computed(() => [
  { "progress-bar-striped": props.striped },
  { "progress-bar-animated": props.animated },
]);
</script>

<style scoped></style>
