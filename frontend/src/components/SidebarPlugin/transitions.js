// src/components/transitions.js

import { h, Transition } from "vue";

// ... (your other transition components like FadeTransition, ZoomCenterTransition, etc.)

export const CollapseTransition = {
  name: "collapse-transition",
  props: ["show"],
  setup(props, { slots }) {
    const transitionHooks = {
      onBeforeEnter(el) {
        el.style.height = "0";
        el.style.opacity = "0";
      },
      onEnter(el) {
        el.style.transition = "height 0.3s ease, opacity 0.3s ease";
        el.style.height = el.scrollHeight + "px";
        el.style.opacity = "1";
      },
      onAfterEnter(el) {
        el.style.height = "";
        el.style.opacity = "";
      },
      onBeforeLeave(el) {
        el.style.height = el.scrollHeight + "px";
        el.style.opacity = "1";
      },
      onLeave(el) {
        el.style.transition = "height 0.3s ease, opacity 0.3s ease";
        el.offsetHeight;
        el.style.height = "0";
        el.style.opacity = "0";
      },
      onAfterLeave(el) {
        el.style.height = "";
        el.style.opacity = "";
      },
    };

    return () =>
      h(Transition, transitionHooks, () =>
        props.show ? h("div", slots.default && slots.default()) : null
      );
  },
};
