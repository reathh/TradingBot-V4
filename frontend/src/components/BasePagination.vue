<template>
  <ul class="pagination" :class="paginationClass">
    <li
        class="page-item prev-page"
        v-if="showArrows"
        :class="{ disabled: modelValue === 1 }"
    >
      <a class="page-link" aria-label="Previous" @click="prevPage">
        <i class="tim-icons icon-double-left" aria-hidden="true"></i>
      </a>
    </li>
    <li
        class="page-item"
        v-for="item in range(minPage, maxPage)"
        :key="item"
        :class="{ active: modelValue === item }"
    >
      <a class="page-link" @click="changePage(item)">{{ item }}</a>
    </li>
    <li
        v-if="showArrows"
        class="page-item page-pre next-page"
        :class="{ disabled: modelValue === totalPages }"
    >
      <a class="page-link" aria-label="Next" @click="nextPage">
        <i class="tim-icons icon-double-right" aria-hidden="true"></i>
      </a>
    </li>
  </ul>
</template>

<script setup>
import { computed, defineProps, defineEmits, watch } from "vue";

const props = defineProps({
  modelValue: {
    type: Number,
    default: 1,
  },
  type: {
    type: String,
    default: "primary",
    validator: (value) =>
      ["default", "primary", "danger", "success", "warning", "info"].includes(
        value
      ),
  },
  pageCount: {
    type: Number,
    default: 0,
  },
  perPage: {
    type: Number,
    default: 10,
  },
  showArrows: {
    type: Boolean,
    default: true,
  },
  total: {
    type: Number,
    default: 0,
  },
  pagesToDisplay: {
    type: Number,
    default: 5,
  },
});

const emit = defineEmits(["update:modelValue"]);

const paginationClass = computed(() => `pagination-${props.type}`);

const totalPages = computed(() => {
  if (props.pageCount > 0) return props.pageCount;
  if (props.total > 0) return Math.ceil(props.total / props.perPage);
  return 1;
});

const defaultPagesToDisplay = computed(() => {
  return totalPages.value < props.pagesToDisplay
    ? totalPages.value
    : props.pagesToDisplay;
});

const minPage = computed(() => {
  if (props.modelValue >= defaultPagesToDisplay.value) {
    const pagesToAdd = Math.floor(defaultPagesToDisplay.value / 2);
    const newMaxPage = pagesToAdd + props.modelValue;
    if (newMaxPage > totalPages.value) {
      return totalPages.value - defaultPagesToDisplay.value + 1;
    }
    return props.modelValue - pagesToAdd;
  }
  return 1;
});

const maxPage = computed(() => {
  if (props.modelValue >= defaultPagesToDisplay.value) {
    const pagesToAdd = Math.floor(defaultPagesToDisplay.value / 2);
    const newMaxPage = pagesToAdd + props.modelValue;
    if (newMaxPage < totalPages.value) {
      return newMaxPage;
    }
    return totalPages.value;
  }
  return defaultPagesToDisplay.value;
});

function range(min, max) {
  const arr = [];
  for (let i = min; i <= max; i++) {
    arr.push(i);
  }
  return arr;
}

function changePage(item) {
  emit("update:modelValue", item);
}

function nextPage() {
  if (props.modelValue < totalPages.value) {
    emit("update:modelValue", props.modelValue + 1);
  }
}

function prevPage() {
  if (props.modelValue > 1) {
    emit("update:modelValue", props.modelValue - 1);
  }
}

watch([() => props.perPage, () => props.total], () => {
  emit("update:modelValue", 1);
});
</script>
