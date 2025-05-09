<template>
  <div class="card-body">
    <!-- Simple Table Controls -->
    <div class="d-flex justify-content-between mb-3">
      <div class="d-flex align-items-center">
        <span class="mr-2">Show</span>
        <el-select
          class="select-primary pagination-select"
          v-model="pageSize"
          placeholder="Per page"
        >
          <el-option
            class="select-primary"
            v-for="size in pageSizeOptions"
            :key="size"
            :label="size"
            :value="size"
          />
        </el-select>
        <span class="ml-2">entries</span>
      </div>
      <div>
        <div class="form-group has-search mb-0">
          <el-input
            type="search"
            class="search-input input-primary"
            clearable
            prefix-icon="el-icon-search"
            placeholder="Search records"
            v-model="searchQuery"
          />
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="table-responsive">
      <base-table
        :data="displayedData"
        :columns="tableColumns"
        thead-classes="text-primary"
      >
        <template #columns>
          <th
            v-for="column in tableColumns"
            :key="column.key"
            :class="{ 'text-right': column.align === 'right' }"
            @click="sortBy(column.key)"
          >
            {{ column.label }}
            <i class="tim-icons" :class="getSortIcon(column.key)"></i>
          </th>
        </template>

        <template #default="{ row }">
          <td>{{ row.botId }}</td>
          <td>{{ row.ticker }}</td>
          <td>{{ formatCurrency(row.entryAvgPrice) }}</td>
          <td>{{ formatCurrency(row.exitAvgPrice) }}</td>
          <td>{{ row.quantity }}</td>
          <td>{{ formatCurrency(row.entryFee + row.exitFee) }}</td>
          <td class="text-right" :class="getProfitClass(row.profit)">
            {{ formatCurrency(row.profit) }}
          </td>
          <td class="text-right">
            <base-button type="info" icon size="sm" class="btn-link">
              <i class="tim-icons icon-zoom-split"></i>
            </base-button>
            <base-button type="success" icon size="sm" class="btn-link">
              <i class="tim-icons icon-refresh-01"></i>
            </base-button>
          </td>
        </template>
      </base-table>
    </div>

    <!-- Pagination Footer -->
    <div class="d-flex justify-content-between align-items-center mt-3">
      <div>
        Showing {{ displayedEntryStart }} to {{ displayedEntryEnd }} of {{ totalFilteredItems }} entries
      </div>

      <base-pagination
        v-model="currentPage"
        :per-page="pageSize"
        :total="totalFilteredItems"
      ></base-pagination>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from "vue";
import BaseTable from "@/components/BaseTable.vue";
import BaseButton from "@/components/BaseButton.vue";
import BasePagination from "@/components/BasePagination.vue";
import { ElSelect, ElOption, ElInput } from "element-plus";
import botService from "@/services/botService";

// Data states
const botPerformanceData = ref([]);
const isLoading = ref(false);
const error = ref(null);
const searchQuery = ref('');
const displayedData = ref([]);

// Pagination state
const currentPage = ref(1);
const pageSize = ref(5);
const pageSizeOptions = [5, 10, 25, 50];

// Sorting state
const sortKey = ref('exitTime');
const sortDirection = ref('desc');

// Table columns definition
const tableColumns = [
  { key: 'botId', label: 'BOT ID' },
  { key: 'ticker', label: 'TICKER' },
  { key: 'entryAvgPrice', label: 'ENTRY AVG. PRICE' },
  { key: 'exitAvgPrice', label: 'EXIT AVG. PRICE' },
  { key: 'quantity', label: 'QUANTITY' },
  { key: 'fees', label: 'FEES' },
  { key: 'profit', label: 'PROFIT', align: 'right' },
  { key: 'actions', label: 'ACTIONS', align: 'right' }
];

// Computed properties for pagination display
const displayedEntryStart = computed(() => {
  if (totalFilteredItems.value === 0) return 0;
  return (currentPage.value - 1) * pageSize.value + 1;
});

const displayedEntryEnd = computed(() => {
  const end = currentPage.value * pageSize.value;
  return end > totalFilteredItems.value ? totalFilteredItems.value : end;
});

// Filter data based on search query
const filteredData = computed(() => {
  if (!searchQuery.value.trim()) return botPerformanceData.value;

  const query = searchQuery.value.toLowerCase();
  return botPerformanceData.value.filter(item => {
    return (
      String(item.botId).toLowerCase().includes(query) ||
      item.ticker.toLowerCase().includes(query) ||
      String(item.entryAvgPrice).includes(query) ||
      String(item.exitAvgPrice).includes(query) ||
      String(item.profit).includes(query)
    );
  });
});

// Total count for pagination
const totalFilteredItems = computed(() => filteredData.value.length);

// Format currency with $ symbol and 2 decimal places
const formatCurrency = (value) => {
  return `$${parseFloat(value).toFixed(2)}`;
};

// Get CSS class based on profit value
const getProfitClass = (profit) => {
  if (!profit) return '';
  return profit > 0 ? 'text-success' : 'text-danger';
};

// Sorting functions
const sortBy = (column) => {
  if (sortKey.value === column) {
    // Toggle direction if clicking the same column
    sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc';
  } else {
    // Set new column and default to ascending
    sortKey.value = column;
    sortDirection.value = 'asc';
  }

  updateDisplayedData();
};

const getSortIcon = (column) => {
  if (sortKey.value !== column) return 'icon-minimal-down opacity-5';
  return sortDirection.value === 'asc' ? 'icon-minimal-up' : 'icon-minimal-down';
};

// Update displayed data with sorting and pagination
const updateDisplayedData = () => {
  // Apply sorting
  const sorted = [...filteredData.value].sort((a, b) => {
    let valueA = a[sortKey.value];
    let valueB = b[sortKey.value];

    // Special case for fees which is calculated
    if (sortKey.value === 'fees') {
      valueA = a.entryFee + a.exitFee;
      valueB = b.entryFee + b.exitFee;
    }

    // Handle different data types
    if (typeof valueA === 'string') valueA = valueA.toLowerCase();
    if (typeof valueB === 'string') valueB = valueB.toLowerCase();

    if (valueA === valueB) return 0;

    const result = valueA < valueB ? -1 : 1;
    return sortDirection.value === 'asc' ? result : -result;
  });

  // Apply pagination
  const start = (currentPage.value - 1) * pageSize.value;
  const end = start + pageSize.value;
  displayedData.value = sorted.slice(start, end);
};

// Fetch bot performance data
const fetchBotPerformance = async () => {
  isLoading.value = true;
  error.value = null;

  try {
    const response = await botService.getBotProfits({
      page: currentPage.value,
      pageSize: pageSize.value
    });

    botPerformanceData.value = response.data;
    updateDisplayedData();

  } catch (err) {
    console.error('Error fetching bot performance:', err);
    error.value = 'Failed to load bot performance data';
    botPerformanceData.value = [];
    displayedData.value = [];
  } finally {
    isLoading.value = false;
  }
};

// Watch for changes that require refetching or updating data
watch([currentPage, pageSize], () => {
  fetchBotPerformance();
});

watch([searchQuery, sortKey, sortDirection, () => filteredData.value], () => {
  currentPage.value = 1; // Reset to first page when filter changes
  updateDisplayedData();
});

// Auto-refresh data every 60 seconds
let autoRefreshInterval;

onMounted(() => {
  fetchBotPerformance();

  // Set up auto-refresh
  autoRefreshInterval = setInterval(() => {
    fetchBotPerformance();
  }, 60000); // 60 seconds
});

// Clean up the interval when component is unmounted
onUnmounted(() => {
  if (autoRefreshInterval) {
    clearInterval(autoRefreshInterval);
  }
});

// Expose methods to parent components
defineExpose({
  fetchBotPerformance
});
</script>

<style>
.text-success {
  color: #00f2c3 !important;
}

.text-danger {
  color: #fd5d93 !important;
}

.opacity-5 {
  opacity: 0.5;
}

.pagination-select {
  width: 80px !important;
}

.search-input {
  width: 250px !important;
}

/* Dark theme overrides for Element UI inputs */
.input-primary.el-input .el-input__wrapper {
  background-color: #27293d !important;
  box-shadow: 0 0 0 1px #2b3553 inset !important;
  border-radius: 0.4285rem !important;
}

.input-primary.el-input .el-input__inner {
  background-color: transparent !important;
  color: white !important;
}

.input-primary.el-input .el-input__suffix,
.input-primary.el-input .el-input__prefix {
  color: rgba(255, 255, 255, 0.7) !important;
}

/* Remove extra outline/border */
.form-group.has-search {
  border: none !important;
  background: transparent !important;
  box-shadow: none !important;
}

.form-group.has-search .el-input {
  border: none !important;
  outline: none !important;
}

.form-group.has-search .el-input__wrapper {
  border: none !important;
}
</style>