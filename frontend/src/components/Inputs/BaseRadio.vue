<template>
  <div class="form-check form-check-radio" :class="[inlineClass, { disabled }]">
    <label :for="cbId" class="form-check-label">
      <input
        :id="cbId"
        class="form-check-input"
        type="radio"
        :disabled="disabled"
        :value="name"
        v-model="model"
      />
      <slot></slot>
      <span class="form-check-sign"></span>
    </label>
  </div>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits, onBeforeMount } from "vue";

const props = defineProps({
  name: {
    type: [String, Number],
    required: true,
  },
  modelValue: {
    type: [String, Boolean, Number],
    required: true,
  },
  disabled: Boolean,
  inline: Boolean,
});

const emit = defineEmits(["update:modelValue"]);

const cbId = ref("");

onBeforeMount(() => {
  cbId.value = Math.random().toString(16).slice(2);
});

const model = computed({
  get: () => props.modelValue,
  set: (val) => emit("update:modelValue", val),
});

const inlineClass = computed(() => (props.inline ? "form-check-inline" : ""));
</script>

<style scoped>
/* Optional scoped styling */
</style>
