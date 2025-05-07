<template>
  <div
    class="choice"
    :class="{ active: modelValue }"
    data-toggle="wizard-checkbox"
    @click="updateValue"
  >
    <input
      type="checkbox"
      :name="name"
      :disabled="disabled"
      :checked="modelValue"
    />
    <div class="icon">
      <slot name="icon">
        <i :class="icon"></i>
      </slot>
    </div>
    <slot name="title">
      <h6>{{ title }}</h6>
    </slot>
  </div>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";

const props = defineProps({
  modelValue: {
    type: Boolean,
    default: false,
  },
  name: String,
  title: String,
  icon: String,
  disabled: Boolean,
});

const emit = defineEmits(["update:modelValue"]);

const updateValue = () => {
  if (!props.disabled) {
    emit("update:modelValue", !props.modelValue);
  }
};
</script>
