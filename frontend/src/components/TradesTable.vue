<template>
  <div class="card-body">
    <PagedTable
      :columns="tableColumns"
      :data="pagedResult.items"
      :page="currentPage"
      :total-pages="pagedResult.totalPages"
      :total-count="pagedResult.totalCount"
      :searchable="true"
      :sortable="true"
      :server-side="true"
      :fetch-data="fetchTradeData"
      thead-classes="text-primary"
    >
      <template #row="{ row }">
        <td>{{ row.id }}</td>
        <td>{{ row.symbol }}</td>
        <td>{{ formatCurrency(row.entryPrice) }}</td>
        <td>{{ formatCurrency(row.exitPrice) }}</td>
        <td>{{ row.quantity ? row.quantity.toFixed(4) : '0.0000' }}</td>
        <td>{{ formatCurrency(row.entryFee + row.exitFee) }}</td>
        <td class="text-right" :class="getProfitClass(row.profit)">
          {{ formatCurrency(row.profit) }}
        </td>
        <td class="text-center">
          <BaseButton 
            type="info" 
            icon 
            size="sm" 
            class="btn-link"
            @click="viewTradeDetails(row)"
          >
            <i class="tim-icons icon-zoom-split"></i>
          </BaseButton>
        </td>
      </template>
    </PagedTable>

    <!-- Trade Details Modal -->
    <el-dialog
      title="Trade Details"
      v-model="showDetailsModal"
      width="600px"
    >
      <div v-if="selectedTrade" class="trade-detail-container">
        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Trade ID:</strong> {{ selectedTrade.id }}
          </div>
          <div class="col-md-6">
            <strong>Symbol:</strong> {{ selectedTrade.symbol }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Direction:</strong>
            <span :class="selectedTrade.direction === 'Buy' ? 'text-success' : 'text-danger'">
              {{ selectedTrade.direction }}
            </span>
          </div>
          <div class="col-md-6">
            <strong>Profit:</strong>
            <span :class="selectedTrade.profit > 0 ? 'text-success' : 'text-danger'">
              {{ formatCurrency(selectedTrade.profit) }}
            </span>
          </div>
        </div>

        <h5 class="mt-4">Entry Details</h5>
        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Price:</strong> {{ formatCurrency(selectedTrade.entryPrice) }}
          </div>
          <div class="col-md-6">
            <strong>Average Fill:</strong> {{ formatCurrency(selectedTrade.entryAvgFill || selectedTrade.entryPrice) }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Quantity:</strong> {{ selectedTrade.quantity?.toFixed(4) ?? 'N/A' }}
          </div>
          <div class="col-md-6">
            <strong>Fee:</strong> {{ selectedTrade.entryFee?.toFixed(4) ?? 'N/A' }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-12">
            <strong>Entry Time:</strong> {{ formatDate(selectedTrade.entryTime) }}
          </div>
        </div>

        <h5 class="mt-4">Exit Details</h5>
        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Price:</strong> {{ formatCurrency(selectedTrade.exitPrice) }}
          </div>
          <div class="col-md-6">
            <strong>Average Fill:</strong> {{ formatCurrency(selectedTrade.exitAvgFill || selectedTrade.exitPrice) }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Fee:</strong> {{ selectedTrade.exitFee?.toFixed(4) ?? 'N/A' }}
          </div>
          <div class="col-md-6">
            <strong>Status:</strong> {{ selectedTrade.isCompleted ? 'Completed' : 'Open' }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-12">
            <strong>Exit Time:</strong> {{ formatDate(selectedTrade.exitTime) }}
          </div>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted, defineProps, defineEmits, watch } from "vue";
import { ElDialog } from "element-plus";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import apiClient from "@/services/api";

const props = defineProps({
  botId: {
    type: Number,
    default: null
  },
  period: {
    type: String,
    default: "month"
  },
  searchQuery: {
    type: String,
    default: ""
  }
});

const emit = defineEmits(['update:loading']);

// Data states
const isLoading = ref(false);
const pagedResult = ref({
  page: 1,
  pageSize: 10,
  totalPages: 0,
  totalCount: 0,
  items: []
});

const currentPage = ref(1);
const error = ref(null);
const selectedTrade = ref(null);
const showDetailsModal = ref(false);

// Table columns - matching structure with BotTable
const tableColumns = [
  { key: 'id', label: 'ID' },
  { key: 'symbol', label: 'TICKER' },
  { key: 'entryPrice', label: 'ENTRY PRICE' },
  { key: 'exitPrice', label: 'EXIT PRICE' },
  { key: 'quantity', label: 'QUANTITY' },
  { key: 'fees', label: 'FEES' },
  { key: 'profit', label: 'PROFIT', align: 'right' },
  { key: 'actions', label: 'ACTIONS', align: 'center' }
];

// Format currency with $ symbol and 2 decimal places
const formatCurrency = (value) => {
  if (value === undefined || value === null) return 'N/A';
  return `$${parseFloat(value).toFixed(2)}`;
};

// Get CSS class based on profit value
const getProfitClass = (profit) => {
  if (!profit) return '';
  return profit > 0 ? 'text-success' : 'text-danger';
};

// Format date helper
function formatDate(dateString) {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString();
}

// View trade details
function viewTradeDetails(trade) {
  selectedTrade.value = trade;
  showDetailsModal.value = true;
}

// Fetch trade data with pagination
const fetchTradeData = async (params) => {
  isLoading.value = true;
  emit('update:loading', true);
  
  try {
    const response = await apiClient.get('trades', {
      params: {
        page: params?.page || currentPage.value,
        pageSize: params?.pageSize || 10,
        botId: props.botId || undefined,
        period: props.period,
        searchQuery: params?.searchQuery || props.searchQuery || undefined,
        sortKey: params?.sortKey,
        sortDirection: params?.sortDirection
      }
    });
    
    // Update local state with response data
    pagedResult.value = response.data;
    
    // Sync the current page and page size
    if (params && params.page) {
      currentPage.value = params.page || currentPage.value;
    }
  } catch (e) {
    error.value = e;
    console.error('Error fetching trade data:', e);
  } finally {
    isLoading.value = false;
    emit('update:loading', false);
  }
};

// Watch for prop changes
watch(() => props.botId, () => {
  currentPage.value = 1; // Reset to first page on filter change
  fetchTradeData();
});

watch(() => props.period, () => {
  currentPage.value = 1; // Reset to first page on filter change
  fetchTradeData();
});

watch(() => props.searchQuery, () => {
  currentPage.value = 1; // Reset to first page on filter change
  fetchTradeData();
});

// Fetch data on mount
onMounted(() => fetchTradeData());
</script>

<style>
.text-success {
  color: #00f2c3 !important;
}

.text-danger {
  color: #fd5d93 !important;
}

.trade-detail-container {
  padding: 16px;
}

.opacity-5 {
  opacity: 0.5;
}

.pagination-select {
  width: 80px !important;
}

/* Direct style override for the select background */
.el-select .el-select__wrapper {
  background-color: #27293d !important;
}

.white-content .el-select .el-select__wrapper {
  background-color: #ffffff !important;
}

/* Force override for the fill-color-blank variable */
:deep(.el-select) {
  --el-fill-color-blank: #27293d !important;
}

.white-content :deep(.el-select) {
  --el-fill-color-blank: #ffffff !important;
}
</style> 