<template>
  <div>
    <card :card-body-classes="cardBodyClasses">
      <h4 v-if="title" slot="header" class="card-title">{{ title }}</h4>
      <div>
        <!-- Controls: Search, Page Size -->
        <div class="col-12 d-flex justify-content-center justify-content-sm-between flex-wrap">
          <el-select
            v-if="showPageSize"
            class="select-primary mb-3 pagination-select"
            v-model="localPageSize"
            placeholder="Per page"
            @change="handlePageSizeChange"
          >
            <el-option
              v-for="size in pageSizeOptions"
              :key="size"
              :label="size"
              :value="size"
            />
          </el-select>

          <base-input v-if="searchable">
            <el-input
              type="search"
              class="mb-3 search-input"
              clearable
              :prefix-icon="Search"
              :placeholder="searchPlaceholder"
              v-model="localSearchQuery"
              aria-controls="datatables"
            />
          </base-input>
        </div>

        <!-- Table -->
        <div v-if="effectiveLoading" class="text-center py-4">
          <div class="spinner-border text-primary" role="status">
            <span class="sr-only">Loading...</span>
          </div>
        </div>
        <el-table v-else :data="displayedData">
          <el-table-column
            v-for="column in columns"
            :key="column.label || column.key || column.prop"
            :prop="column.prop || column.key"
            :label="column.label"
            :min-width="column.minWidth"
            :align="column.align"
            :sortable="sortable && column.sortable !== false ? 'custom' : false"
            @sort-change="handleSort"
          />
          <slot name="actions" />
        </el-table>
      </div>

      <!-- Pagination Footer -->
      <div
        slot="footer"
        class="col-12 d-flex justify-content-center justify-content-sm-between flex-wrap"
      >
        <div>
          <p class="card-category">
            <slot name="footer-info">
              <span v-if="totalCount > 0">
                Showing {{ displayedEntryStart }} to {{ displayedEntryEnd }} of {{ totalCount }} entries
              </span>
              <span v-else>
                No entries found
              </span>
            </slot>
          </p>
        </div>
        <base-pagination
          class="pagination-no-border"
          v-model="localCurrentPage" 
          :per-page="localPageSize"
          :total="totalCount"
          @update:modelValue="handlePageChange"
        />
      </div>
    </card>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, defineExpose } from 'vue';
import { Search } from '@element-plus/icons-vue';
import BaseInput from "@/components/Inputs/BaseInput.vue";
import Card from "@/components/Cards/Card.vue";
import BasePagination from "@/components/BasePagination.vue";

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
  isLoading: { type: Boolean, default: false },
  title: { type: String, default: '' },
  cardBodyClasses: { type: String, default: 'table-full-width' }
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

// Sorting handling for Element-UI table
function handleSort(sort) {
  if (sort && sort.prop) {
    localSortKey.value = sort.prop;
    localSortDirection.value = sort.order === 'ascending' ? 'asc' : 'desc';
    
    if (props.serverSide) {
      loadData({
        sortKey: localSortKey.value, 
        sortDirection: localSortDirection.value
      });
    }
  } else {
    localSortKey.value = '';
    localSortDirection.value = 'asc';
    
    if (props.serverSide) {
      loadData();
    }
  }
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
  if (props.serverSide) {
    loadData({ page: 1 });
  }
});

// Initial load
onMounted(() => {
  loadData();
});

defineExpose({
  refresh: loadData
});
</script> 