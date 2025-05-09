<template>
  <div>
    <!-- Controls: Search, Page Size -->
    <div class="d-flex justify-content-between mb-3" v-if="searchable || showPageSize">
      <div class="d-flex align-items-center" v-if="showPageSize">
        <span class="mr-2">Show</span>
        <el-select
          class="select-primary pagination-select"
          v-model="localPageSize"
          :placeholder="'Per page'"
        >
          <el-option
            v-for="size in pageSizeOptions"
            :key="size"
            :label="size"
            :value="size"
          />
        </el-select>
        <span class="ml-2">entries</span>
      </div>
      <div class="form-group has-search mb-0" v-if="searchable">
        <el-input
          type="search"
          class="search-input input-primary"
          clearable
          :prefix-icon="Search"
          :placeholder="searchPlaceholder"
          v-model="localSearchQuery"
        />
      </div>
    </div>

    <!-- Table -->
    <div class="table-responsive">
      <BaseTable
        :data="pagedData"
        :columns="columns"
        :thead-classes="theadClasses"
      >
        <template #columns>
          <th
            v-for="column in columns"
            :key="column.key || column.prop"
            :class="{ 'text-right': column.align === 'right' }"
            :style="sortable && column.sortable !== false ? 'cursor:pointer;' : ''"
            @click="sortable && column.sortable !== false ? () => sortBy(column.key || column.prop) : null"
          >
            {{ column.label }}
            <i
              v-if="sortable && column.sortable !== false"
              class="tim-icons"
              :class="getSortIcon(column.key || column.prop)"
            ></i>
          </th>
        </template>
        <template #default="slotProps">
          <slot name="row" v-bind="slotProps" />
        </template>
      </BaseTable>
    </div>

    <!-- Pagination Footer -->
    <div class="d-flex justify-content-between align-items-center mt-3">
      <div>
        <slot name="footer-info">
          Showing {{ displayedEntryStart }} to {{ displayedEntryEnd }} of {{ filteredData.length }} entries
        </slot>
      </div>
      <BasePagination
        v-model="localCurrentPage"
        :per-page="localPageSize"
        :total="filteredData.length"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import BaseTable from './BaseTable.vue';
import BasePagination from './BasePagination.vue';
import { Search } from '@element-plus/icons-vue';

const props = defineProps({
  columns: { type: Array, required: true },
  data: { type: Array, required: true },
  searchable: { type: Boolean, default: true },
  searchPlaceholder: { type: String, default: 'Search records' },
  sortable: { type: Boolean, default: true },
  pageSizeOptions: { type: Array, default: () => [5, 10, 25, 50] },
  defaultPageSize: { type: Number, default: 5 },
  theadClasses: { type: String, default: 'text-primary' },
  showPageSize: { type: Boolean, default: true },
});

const emit = defineEmits(['row-action', 'page-change']);

const localSearchQuery = ref('');
const localCurrentPage = ref(1);
const localPageSize = ref(props.defaultPageSize);
const localSortKey = ref(props.columns[0]?.key || props.columns[0]?.prop || '');
const localSortDirection = ref('asc');

// Watch for page size change to reset page
watch(localPageSize, () => {
  localCurrentPage.value = 1;
});

// Filtering
const filteredData = computed(() => {
  if (!props.searchable || !localSearchQuery.value.trim()) return props.data;
  const query = localSearchQuery.value.toLowerCase();
  return props.data.filter(item => {
    return props.columns.some(col => {
      const val = item[col.key || col.prop];
      return val && String(val).toLowerCase().includes(query);
    });
  });
});

// Sorting
const sortedData = computed(() => {
  if (!props.sortable || !localSortKey.value) return filteredData.value;
  return [...filteredData.value].sort((a, b) => {
    let valueA = a[localSortKey.value];
    let valueB = b[localSortKey.value];
    if (typeof valueA === 'string') valueA = valueA.toLowerCase();
    if (typeof valueB === 'string') valueB = valueB.toLowerCase();
    if (valueA === valueB) return 0;
    const result = valueA < valueB ? -1 : 1;
    return localSortDirection.value === 'asc' ? result : -result;
  });
});

// Pagination
const pagedData = computed(() => {
  const start = (localCurrentPage.value - 1) * localPageSize.value;
  const end = start + localPageSize.value;
  return sortedData.value.slice(start, end);
});

const displayedEntryStart = computed(() => {
  if (filteredData.value.length === 0) return 0;
  return (localCurrentPage.value - 1) * localPageSize.value + 1;
});
const displayedEntryEnd = computed(() => {
  const end = localCurrentPage.value * localPageSize.value;
  return end > filteredData.value.length ? filteredData.value.length : end;
});

function sortBy(key) {
  if (localSortKey.value === key) {
    localSortDirection.value = localSortDirection.value === 'asc' ? 'desc' : 'asc';
  } else {
    localSortKey.value = key;
    localSortDirection.value = 'asc';
  }
}

function getSortIcon(key) {
  if (localSortKey.value !== key) return 'icon-minimal-down opacity-5';
  return localSortDirection.value === 'asc' ? 'icon-minimal-up' : 'icon-minimal-down';
}
</script> 