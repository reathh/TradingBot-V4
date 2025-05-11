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
      <div v-if="effectiveLoading" class="text-center py-4">
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
          <span v-if="totalCount > 0">
            Showing {{ displayedEntryStart }} to {{ displayedEntryEnd }} of {{ totalCount }} entries
          </span>
          <span v-else>
            No entries found
          </span>
        </slot>
      </div>
      <BasePagination
        v-model="localCurrentPage" 
        :per-page="localPageSize"
        :total="totalCount"
        @update:modelValue="handlePageChange"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, defineExpose } from 'vue';
import BaseTable from './BaseTable.vue';
import BasePagination from './BasePagination.vue';
import { Search } from '@element-plus/icons-vue';

const props = defineProps({
  columns: { type: Array, required: true },
  fetchData: { type: Function, required: true },
  searchable: { type: Boolean, default: true },
  searchPlaceholder: { type: String, default: 'Search records' },
  sortable: { type: Boolean, default: true },
  pageSizeOptions: { type: Array, default: () => [5, 10, 25, 50] },
  defaultPageSize: { type: Number, default: 5 },
  theadClasses: { type: String, default: 'text-primary' },
  showPageSize: { type: Boolean, default: true },
  serverSide: { type: Boolean, default: false },
  isLoading: { type: Boolean, default: false }
});

const emit = defineEmits(['update:page', 'update:pageSize', 'search', 'sort']);

// Local state
const localSearchQuery = ref('');
const localCurrentPage = ref(1);
const localPageSize = ref(props.defaultPageSize);
const localSortKey = ref('');
const localSortDirection = ref('asc');
const internalLoading = ref(false);
const items = ref([]);
const totalCount = ref(0);

const effectiveLoading = computed(() => {
  return props.fetchData ? internalLoading.value : props.isLoading;
});

// Fetch data from parent fetchData
async function loadData({ page = localCurrentPage.value, pageSize = localPageSize.value, sortKey = localSortKey.value, sortDirection = localSortDirection.value } = {}) {
  internalLoading.value = true;
  try {
    const result = await props.fetchData({
      page,
      pageSize,
      searchQuery: localSearchQuery.value,
      sortKey,
      sortDirection
    });
    items.value = result.items;
    totalCount.value = result.totalCount;
  } finally {
    internalLoading.value = false;
  }
}

// Pagination events
async function handlePageChange(page) {
  localCurrentPage.value = page;
  await loadData({ page });
}

async function handlePageSizeChange(pageSize) {
  localPageSize.value = pageSize;
  localCurrentPage.value = 1;
  await loadData({ page: 1, pageSize });
}

// Sorting (server-side only)
function sortBy(key) {
  if (props.serverSide) {
    if (props.fetchData) {
      if (localSortKey.value === key) {
        localSortDirection.value = localSortDirection.value === 'asc' ? 'desc' : 'asc';
      } else {
        localSortKey.value = key;
        localSortDirection.value = 'asc';
      }
      loadData({
        page: localCurrentPage.value,
        pageSize: localPageSize.value,
        sortKey: localSortKey.value,
        sortDirection: localSortDirection.value
      });
    } else {
      emit('sort', { key, direction: localSortDirection.value === 'asc' ? 'desc' : 'asc' });
    }
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

// Filtering (client-side only)
const filteredData = computed(() => {
  if (props.serverSide) return items.value;
  if (!props.searchable || !localSearchQuery.value.trim()) return items.value;
  const query = localSearchQuery.value.toLowerCase();
  return items.value.filter(item => {
    return props.columns.some(col => {
      const key = col.key || col.prop;
      const val = item[key];
      return val && String(val).toLowerCase().includes(query);
    });
  });
});

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

const displayedData = computed(() => {
  if (props.serverSide) return items.value;
  const start = (localCurrentPage.value - 1) * localPageSize.value;
  const end = start + localPageSize.value;
  return sortedData.value.slice(start, end);
});

const displayedEntryStart = computed(() => {
  if (totalCount.value === 0) return 0;
  return (localCurrentPage.value - 1) * localPageSize.value + 1;
});

const displayedEntryEnd = computed(() => {
  const end = localCurrentPage.value * localPageSize.value;
  return Math.min(end, totalCount.value);
});

// Watch for search query changes
watch(localSearchQuery, () => {
  localCurrentPage.value = 1;
  loadData({ page: 1 });
});

// Initial load
onMounted(() => {
  loadData();
});

defineExpose({
  refresh: loadData
});
</script> 