<template>
  <div class="form-check" :class="[{ disabled }, inlineClass]">
    <label :for="cbId" class="form-check-label">
      <input
        :id="cbId"
        class="form-check-input"
        type="checkbox"
        :disabled="disabled"
        v-model="model"
      />
      <span class="form-check-sign"></span>
      <slot><span v-if="inline">&nbsp;</span></slot>
    </label>
  </div>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits, onBeforeMount } from "vue";

const props = defineProps({
  modelValue: {
    type: [Array, Boolean],
    default: false,
  },
  disabled: Boolean,
  inline: Boolean,
  hasError: Boolean,
});

const emit = defineEmits(["update:modelValue"]);

const cbId = ref("");
const touched = ref(false);

onBeforeMount(() => {
  cbId.value = Math.random().toString(16).slice(2);
});

const model = computed({
  get() {
    return props.modelValue;
  },
  set(val) {
    if (!touched.value) {
      touched.value = true;
    }
    emit("update:modelValue", val);
  },
});

const inlineClass = computed(() => {
  return props.inline ? "form-check-inline" : false;
});
</script>

<style scoped></style>
