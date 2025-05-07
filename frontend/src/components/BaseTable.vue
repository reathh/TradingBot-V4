<template>
  <table class="table tablesorter" :class="tableClass">
    <thead :class="theadClasses">
      <tr>
        <slot name="columns" :columns="columns">
          <th v-for="column in columns" :key="column">{{ column }}</th>
        </slot>
      </tr>
    </thead>
    <tbody :class="tbodyClasses">
      <tr v-for="(item, index) in data" :key="index">
        <slot :row="item" :index="index">
          <td v-for="(column, index) in columns" :key="index">
            <template v-if="hasValue(item, column)">{{
              itemValue(item, column)
            }}</template>
          </td>
        </slot>
      </tr>
    </tbody>
  </table>
</template>

<script setup>
import { computed, defineProps } from "vue";

const props = defineProps({
  columns: {
    type: Array,
    default: () => [],
    description: "Table columns",
  },
  data: {
    type: Array,
    default: () => [],
    description: "Table data",
  },
  type: {
    type: String, // striped | hover
    default: "",
    description: "Whether table is striped or hover type",
  },
  theadClasses: {
    type: String,
    default: "",
    description: "<thead> css classes",
  },
  tbodyClasses: {
    type: String,
    default: "",
    description: "<tbody> css classes",
  },
});

// Computed property for table class
const tableClass = computed(() => {
  return props.type ? `table-${props.type}` : "";
});

// Methods
const hasValue = (item, column) => {
  return item[column.toLowerCase()] !== "undefined";
};

const itemValue = (item, column) => {
  return item[column.toLowerCase()];
};
</script>

<style></style>
