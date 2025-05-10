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
        <td>{{ formatNumber(row.quantity) }}</td>
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

    <!-- Using the new TradeDetails component -->
    <TradeDetails
      v-model="showDetailsModal"
      :trade="selectedTrade"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, defineProps, defineEmits, watch } from "vue";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import TradeDetails from "@/components/TradeDetails.vue";
import apiClient from "@/services/api";
import { formatCurrency, formatNumber, getProfitClass } from "@/util/formatters";

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

/* This style is now in TradeDetails component */
/* .trade-detail-container {
  padding: 16px;
} */

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