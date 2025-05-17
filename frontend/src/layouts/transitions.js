// This module replicates vue2-transitions components in Vue 3
// You can import each transition individually or register all globally

import { h, Transition } from "vue";

const createTransition = (name, classes) => {
  return {
    name,
    setup(_, { slots }) {
      return () => h(Transition, { name }, slots);
    },
    style: classes,
  };
};

export const FadeTransition = createTransition(
  "fade",
  `
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s ease;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}`
);

export const ZoomCenterTransition = createTransition(
  "zoom-center",
  `
.zoom-center-enter-active, .zoom-center-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.zoom-center-enter-from, .zoom-center-leave-to {
  transform: scale(0.8);
  opacity: 0;
}`
);

export const ZoomXTransition = createTransition(
  "zoom-x",
  `
.zoom-x-enter-active, .zoom-x-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.zoom-x-enter-from, .zoom-x-leave-to {
  transform: scaleX(0.8);
  opacity: 0;
}`
);

export const ZoomYTransition = createTransition(
  "zoom-y",
  `
.zoom-y-enter-active, .zoom-y-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.zoom-y-enter-from, .zoom-y-leave-to {
  transform: scaleY(0.8);
  opacity: 0;
}`
);

export const ScaleTransition = createTransition(
  "scale",
  `
.scale-enter-active, .scale-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.scale-enter-from, .scale-leave-to {
  transform: scale(0);
  opacity: 0;
}`
);

export const SlideXLeftTransition = createTransition(
  "slide-x-left",
  `
.slide-x-left-enter-active, .slide-x-left-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.slide-x-left-enter-from, .slide-x-left-leave-to {
  transform: translateX(-100%);
  opacity: 0;
}`
);

export const SlideXRightTransition = createTransition(
  "slide-x-right",
  `
.slide-x-right-enter-active, .slide-x-right-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.slide-x-right-enter-from, .slide-x-right-leave-to {
  transform: translateX(100%);
  opacity: 0;
}`
);

export const SlideYUpTransition = createTransition(
  "slide-y-up",
  `
.slide-y-up-enter-active, .slide-y-up-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.slide-y-up-enter-from, .slide-y-up-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}`
);

export const SlideYDownTransition = createTransition(
  "slide-y-down",
  `
.slide-y-down-enter-active, .slide-y-down-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.slide-y-down-enter-from, .slide-y-down-leave-to {
  transform: translateY(100%);
  opacity: 0;
}`
);

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