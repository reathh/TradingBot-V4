<template>
  <component
    :is="tag"
    class="dropdown"
    :class="[{ show: isOpen.value }, `drop${direction}`]"
    @click="toggleDropDown"
    v-click-outside="closeDropDown"
  >
    <slot name="title-container" :is-open="isOpen.value">
      <component
        :is="titleTag"
        class="dropdown-toggle no-caret"
        :class="titleClasses"
        :aria-label="title || 'dropdown'"
        :aria-expanded="isOpen.value"
        data-toggle="dropdown"
      >
        <slot name="title" :is-open="isOpen.value">
          <i :class="icon"></i> {{ title }}
        </slot>
      </component>
    </slot>

    <ul
      class="dropdown-menu"
      :class="[
        { show: isOpen.value },
        { 'dropdown-menu-right': menuOnRight },
        menuClasses,
      ]"
    >
      <slot />
    </ul>
  </component>
</template>

<script setup>
import { ref } from "vue";

const props = defineProps({
  tag: {
    type: String,
    default: "div",
  },
  titleTag: {
    type: String,
    default: "button",
  },
  title: String,
  direction: {
    type: String,
    default: "down",
  },
  icon: String,
  titleClasses: {
    type: [String, Object, Array],
    default: "",
  },
  menuClasses: {
    type: [String, Object, Array],
    default: "",
  },
  menuOnRight: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["change"]);
const isOpen = ref(false);

function toggleDropDown() {
  isOpen.value = !isOpen.value;
  emit("change", isOpen.value);
}

function closeDropDown() {
  isOpen.value = false;
  emit("change", false);
}
</script>

<style scoped lang="scss">
.dropdown {
  cursor: pointer;
  user-select: none;
}
</style>
