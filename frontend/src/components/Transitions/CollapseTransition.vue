<template>
  <component :is="tag" v-bind="$attrs">
    <transition
      :css="!noCSS"
      @before-enter="beforeEnter"
      @enter="enter"
      @after-enter="afterEnter"
      @before-leave="beforeLeave"
      @leave="leave"
      @after-leave="afterLeave"
    >
      <slot />
    </transition>
  </component>
</template>

<script setup>
import { defineProps } from "vue";

const props = defineProps({
  tag: {
    type: String,
    default: "div",
  },
  duration: {
    type: Number,
    default: 300,
  },
  noCSS: {
    type: Boolean,
    default: false,
  },
});

function beforeEnter(el) {
  el.style.height = "0";
  el.style.opacity = "0";
  el.style.overflow = "hidden";
}

function enter(el, done) {
  const scrollHeight = el.scrollHeight;
  el.style.transition = `height ${props.duration}ms ease-in-out, opacity ${props.duration}ms ease-in-out`;
  el.style.height = `${scrollHeight}px`;
  el.style.opacity = "1";

  setTimeout(() => {
    done();
  }, props.duration);
}

function afterEnter(el) {
  el.style.height = "auto";
  el.style.overflow = "visible";
  el.style.transition = "";
}

function beforeLeave(el) {
  el.style.height = `${el.scrollHeight}px`;
  el.style.overflow = "hidden";
}

function leave(el, done) {
  getComputedStyle(el).height; // Force reflow
  el.style.transition = `height ${props.duration}ms ease-in-out, opacity ${props.duration}ms ease-in-out`;
  el.style.height = "0";
  el.style.opacity = "0";

  setTimeout(() => {
    done();
  }, props.duration);
}

function afterLeave(el) {
  el.style.transition = "";
  el.style.height = "auto";
}
</script>
