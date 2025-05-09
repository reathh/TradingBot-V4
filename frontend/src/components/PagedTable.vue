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
          @change="handlePageSizeChange"
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
      <div v-if="isLoading" class="text-center py-4">
        <div class="spinner-border text-primary" role="status">
          <span class="sr-only">Loading...</span>
        </div>
      </div>
      <BaseTable
        v-else
        :data="displayedData"
        :columns="columns"
        :thead-classes="theadClasses"
      >
        <template #columns>
          <th
            v-for="column in columns"
            :key="column.key || column.prop"
            :class="{ 'text-right': column.align === 'right' }"
            :style="sortable && column.sortable !== false ? 'cursor:pointer;' : ''"
            @click="sortable && column.sortable !== false ? sortBy(column.key || column.prop) : null"
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
          Showing {{ displayedEntryStart }} to {{ displayedEntryEnd }} of {{ effectiveTotalCount }} entries
        </slot>
      </div>
      <BasePagination
        v-model="localCurrentPage" 
        :per-page="localPageSize"
        :total="effectiveTotalCount"
        @update:modelValue="handlePageChange"
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
  // Data and columns
  columns: { type: Array, required: true },
  data: { type: Array, required: true },
  
  // Pagination from backend (for server-side pagination)
  page: { type: Number, default: 1 },
  pageSize: { type: Number, default: 10 },
  totalPages: { type: Number, default: 0 },
  totalCount: { type: Number, default: 0 },
  
  // Local options
  searchable: { type: Boolean, default: true },
  searchPlaceholder: { type: String, default: 'Search records' },
  sortable: { type: Boolean, default: true },
  pageSizeOptions: { type: Array, default: () => [5, 10, 25, 50] },
  defaultPageSize: { type: Number, default: 10 },
  theadClasses: { type: String, default: 'text-primary' },
  showPageSize: { type: Boolean, default: true },
  
  // Server-side options
  serverSide: { type: Boolean, default: false },
  
  // Loading state
  isLoading: { type: Boolean, default: false }
});

const emit = defineEmits([
  'update:page', 
  'update:pageSize', 
  'search', 
  'sort'
]);

// Local state
const localSearchQuery = ref('');
const localCurrentPage = ref(props.page);
const localPageSize = ref(props.pageSize || props.defaultPageSize);
const localSortKey = ref('');
const localSortDirection = ref('asc');

// Sync with props
watch(() => props.page, (newVal) => {
  localCurrentPage.value = newVal;
});

watch(() => props.pageSize, (newVal) => {
  if (newVal) localPageSize.value = newVal;
});

// Handle pagination events
function handlePageChange(page) {
  localCurrentPage.value = page;
  if (props.serverSide) {
    emit('update:page', page);
  }
}

function handlePageSizeChange(pageSize) {
  localPageSize.value = pageSize;
  localCurrentPage.value = 1; // Reset to first page
  if (props.serverSide) {
    emit('update:pageSize', pageSize);
  }
}

// If server-side, we use the provided data directly
// If client-side, we filter, sort, and paginate the data locally
const filteredData = computed(() => {
  if (props.serverSide) return props.data;
  
  if (!props.searchable || !localSearchQuery.value.trim()) return props.data;
  const query = localSearchQuery.value.toLowerCase();
  return props.data.filter(item => {
    return props.columns.some(col => {
      const key = col.key || col.prop;
      const val = item[key];
      return val && String(val).toLowerCase().includes(query);
    });
  });
});

// Sorting (client-side only)
const sortedData = computed(() => {
  if (props.serverSide) return filteredData.value;
  
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

// Pagination (client-side only)
const displayedData = computed(() => {
  if (props.serverSide) return props.data;
  
  const start = (localCurrentPage.value - 1) * localPageSize.value;
  const end = start + localPageSize.value;
  return sortedData.value.slice(start, end);
});

// Calculate display values
const effectiveTotalCount = computed(() => 
  props.serverSide ? props.totalCount : filteredData.value.length
);

const displayedEntryStart = computed(() => {
  if (effectiveTotalCount.value === 0) return 0;
  return (localCurrentPage.value - 1) * localPageSize.value + 1;
});

const displayedEntryEnd = computed(() => {
  const end = localCurrentPage.value * localPageSize.value;
  return end > effectiveTotalCount.value ? effectiveTotalCount.value : end;
});

// Sorting functions
function sortBy(key) {
  if (props.serverSide) {
    emit('sort', { key, direction: localSortDirection.value === 'asc' ? 'desc' : 'asc' });
    return;
  }
  
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