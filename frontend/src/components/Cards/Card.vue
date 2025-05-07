<template>
  <div class="card" :class="[type && `card-${type}`]">
    <div class="card-image" v-if="$slots.image">
      <slot name="image" />
    </div>

    <div
      class="card-header"
      v-if="$slots.header || title"
      :class="headerClasses"
    >
      <slot name="header">
        <h4 class="card-title">{{ title }}</h4>
        <p class="card-category" v-if="subTitle">{{ subTitle }}</p>
      </slot>
    </div>

    <div class="card-body" v-if="$slots.default" :class="bodyClasses">
      <slot />
    </div>

    <div class="card-image" v-if="$slots['image-bottom']">
      <slot name="image-bottom" />
    </div>

    <slot name="raw-content" />

    <div class="card-footer" :class="footerClasses" v-if="$slots.footer">
      <hr v-if="showFooterLine" />
      <slot name="footer" />
    </div>
  </div>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    default: "",
    description: "Card title",
  },
  subTitle: {
    type: String,
    default: "",
    description: "Card subtitle",
  },
  type: {
    type: String,
    default: "",
    description: "Card type (e.g. primary, danger, etc.)",
  },
  showFooterLine: {
    type: Boolean,
    default: false,
  },
  headerClasses: {
    type: [String, Object, Array],
    default: "",
    description: "CSS classes for the card header",
  },
  bodyClasses: {
    type: [String, Object, Array],
    default: "",
    description: "CSS classes for the card body",
  },
  footerClasses: {
    type: [String, Object, Array],
    default: "",
    description: "CSS classes for the card footer",
  },
});
</script>
