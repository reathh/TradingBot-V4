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
          <slot>
            <p v-if="subTitle" class="card-category">{{ subTitle }}</p>
            <h3 v-if="title" class="card-title">{{ title }}</h3>
          </slot>
        </div>
      </div>
    </div>
    <template #footer>
      <div class="stats" v-if="$slots.footer">
        <slot name="footer" />
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
});
</script>
