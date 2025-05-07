<template>
  <div
    class="tab-pane fade"
    role="tabpanel"
    :id="tabId"
    :aria-hidden="!active.value"
    :aria-labelledby="`step-${tabId}`"
    :class="{ 'active show': active.value }"
    v-show="active.value"
  >
    <slot></slot>
  </div>
</template>

<script setup>
import { ref, inject, onMounted, onBeforeUnmount, computed } from "vue";

const props = defineProps({
  label: String,
  id: String,
  beforeChange: Function,
});

const addTab = inject("addTab");
const removeTab = inject("removeTab");

const active = ref(false);
const checked = ref(false);
const hasError = ref(false);

// Generate tabId from label or fallback to prop id
const tabId = computed(() => {
  if (props.id) return props.id;
  return `${(props.label || "tab").replace(/\s+/g, "")}-${Math.random()
    .toString(36)
    .substring(2, 8)}`;
});

onMounted(() => {
  addTab?.({
    ...props,
    active: active.value,
    checked: checked.value,
    hasError: hasError.value,
    tabId: tabId.value,
    beforeChange: props.beforeChange,
    // Let parent wizard update reactive refs
    get active() {
      return active.value;
    },
    set active(val) {
      active.value = val;
    },
    get checked() {
      return checked.value;
    },
    set checked(val) {
      checked.value = val;
    },
    get hasError() {
      return hasError.value;
    },
    set hasError(val) {
      hasError.value = val;
    },
  });
});

onBeforeUnmount(() => {
  removeTab?.({
    tabId: tabId.value,
  });
});
</script>

<style scoped></style>
