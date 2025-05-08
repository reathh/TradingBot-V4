<template>
  <Card class="card-stats" :show-footer-line="true">
    <div class="row">
      <div class="col-5" v-if="$slots.icon || icon">
        <div class="info-icon text-center" :class="`icon-${type}`">
          <slot name="icon">
            <i :class="icon" />
          </slot>
        </div>
      </div>
      <div class="col-7" v-if="$slots.content || title || subTitle">
        <div class="numbers">
          <p v-if="subTitle" class="card-category">{{ subTitle }}</p>
          <h3 v-if="title" class="card-title">{{ title }}</h3>
        </div>
      </div>
    </div>
    <template #footer>
      <div class="stats d-flex align-items-center">
        <i v-if="footerIcon" :class="footerIcon" class="mr-2" />
        <p v-if="footer" class="mb-0">{{ footer }}</p>
      </div>
    </template>
  </Card>
</template>

<script setup>
import Card from "./Card.vue";

defineProps({
  type: {
    type: String,
    default: "primary",
  },
  icon: String,
  title: String,
  subTitle: String,
  footer: String,
  footerIcon: String,
});
</script>
