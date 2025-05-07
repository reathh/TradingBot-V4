<template>
  <nav :class="[classes, 'navbar']">
    <div :class="containerClasses">
      <slot name="brand" />

      <slot name="toggle-button">
        <button
          class="navbar-toggler collapsed"
          v-if="hasMenu"
          type="button"
          @click="toggleMenu"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span class="navbar-toggler-bar navbar-kebab"></span>
          <span class="navbar-toggler-bar navbar-kebab"></span>
          <span class="navbar-toggler-bar navbar-kebab"></span>
        </button>
      </slot>

      <CollapseTransition
        @after-leave="onTransitionEnd"
        @before-enter="onTransitionStart"
      >
        <div
          class="collapse navbar-collapse show"
          :class="menuClasses"
          v-show="show"
        >
          <slot />
        </div>
      </CollapseTransition>
    </div>
  </nav>
</template>

<script setup>
import { computed, ref, watch, useSlots } from "vue";
import CollapseTransition from "../Transitions/CollapseTransition.vue";

const props = defineProps({
  show: {
    type: Boolean,
    default: false,
  },
  transparent: {
    type: Boolean,
    default: false,
  },
  expand: {
    type: String,
    default: "lg",
  },
  menuClasses: {
    type: [String, Object, Array],
    default: "",
  },
  containerClasses: {
    type: [String, Object, Array],
    default: "container-fluid",
  },
  type: {
    type: String,
    default: "white",
    validator: (value) =>
      [
        "dark",
        "success",
        "danger",
        "warning",
        "white",
        "primary",
        "info",
        "vue",
      ].includes(value),
  },
});

const emit = defineEmits(["change"]);

const slots = useSlots();
const transitionFinished = ref(true);

const hasMenu = computed(() => !!slots.default);

const classes = computed(() => {
  const color = `bg-${props.type}`;
  const classList = [
    { "navbar-transparent": !props.show && props.transparent },
    { [`navbar-expand-${props.expand}`]: !!props.expand },
  ];

  if (
    !props.transparent ||
    (props.show && !transitionFinished.value) ||
    (!props.show && !transitionFinished.value)
  ) {
    classList.push(color);
  }

  return classList;
});

function toggleMenu() {
  emit("change", !props.show);
}

function onTransitionStart() {
  transitionFinished.value = false;
}

function onTransitionEnd() {
  transitionFinished.value = true;
}
</script>
