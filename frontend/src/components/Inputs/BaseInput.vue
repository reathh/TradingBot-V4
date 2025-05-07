<template>
  <div
    class="form-group"
    :class="{
      'input-group-focus': focused,
      'has-label': label,
      'has-icon': hasIcon,
    }"
  >
    <slot name="label">
      <label v-if="label">{{ label }} <span v-if="required">*</span></label>
    </slot>

    <div class="mb-0" :class="{ 'input-group': hasIcon }">
      <slot name="addonLeft">
        <span v-if="addonLeftIcon" class="input-group-prepend">
          <div class="input-group-text">
            <i :class="addonLeftIcon"></i>
          </div>
        </span>
      </slot>

      <slot>
        <input
          v-bind="$attrs"
          class="form-control"
          :value="modelValue"
          @input="onInput"
          @focus="onFocus"
          @blur="onBlur"
          aria-describedby="addon-right addon-left"
        />
      </slot>

      <slot name="addonRight">
        <span v-if="addonRightIcon" class="input-group-append">
          <div class="input-group-text">
            <i :class="addonRightIcon"></i>
          </div>
        </span>
      </slot>
    </div>

    <slot name="error" v-if="error || $slots.error">
      <label class="error">{{ error }}</label>
    </slot>
    <slot name="helperText"></slot>
  </div>
</template>

<script setup>
import { ref, computed, useSlots } from "vue";
import { useAttrs, defineProps, defineEmits } from "vue";

defineOptions({ inheritAttrs: false });

const props = defineProps({
  required: Boolean,
  label: String,
  error: {
    type: String,
    default: "",
  },
  modelValue: [String, Number],
  addonRightIcon: String,
  addonLeftIcon: String,
});

const emit = defineEmits(["update:modelValue", "focus", "blur"]);

const attrs = useAttrs();
const slots = useSlots();

const focused = ref(false);
const touched = ref(false);

const hasLeftAddon = computed(() => {
  return slots.addonLeft !== undefined || props.addonLeftIcon !== undefined;
});

const hasRightAddon = computed(() => {
  return slots.addonRight !== undefined || props.addonRightIcon !== undefined;
});

const hasIcon = computed(() => {
  return hasLeftAddon.value || hasRightAddon.value;
});

function onInput(event) {
  if (!touched.value) {
    touched.value = true;
  }
  emit("update:modelValue", event.target.value);
}

function onFocus(event) {
  focused.value = true;
  emit("focus", event);
}

function onBlur(event) {
  focused.value = false;
  emit("blur", event);
}
</script>

<style scoped></style>
