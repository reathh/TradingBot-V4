<template>
  <div
    class="bootstrap-switch bootstrap-switch-wrapper bootstrap-switch-animate"
    :class="switchClass"
    @click="triggerToggle"
  >
    <div class="bootstrap-switch-container">
      <span class="bootstrap-switch-handle-on">
        <slot name="on">{{ onText }}</slot>
      </span>
      <span class="bootstrap-switch-label"></span>
      <span class="bootstrap-switch-handle-off bootstrap-switch-default">
        <slot name="off">{{ offText }}</slot>
      </span>
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps, defineEmits } from "vue";

const props = defineProps({
  modelValue: [Array, Boolean],
  onText: String,
  offText: String,
});

const emit = defineEmits(["update:modelValue"]);

const isModelOn = computed({
  get() {
    return props.modelValue;
  },
  set(value) {
    emit("update:modelValue", value);
  },
});

const switchClass = computed(() => {
  const base = "bootstrap-switch-";
  const state = isModelOn.value ? "on" : "off";
  return base + state;
});

const triggerToggle = () => {
  isModelOn.value = !isModelOn.value;
};
</script>
