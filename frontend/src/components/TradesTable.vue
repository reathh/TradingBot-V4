<template>
  <div class="card-body">
    <PagedTable
      :columns="tableColumns"
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
          {{ formatCurrency(row.profit, '$', 4) }}
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

const isLoading = ref(false);
const error = ref(null);
const selectedTrade = ref(null);
const showDetailsModal = ref(false);

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

function viewTradeDetails(trade) {
  selectedTrade.value = trade;
  showDetailsModal.value = true;
}

const fetchTradeData = async (params) => {
  isLoading.value = true;
  emit('update:loading', true);
  try {
    const response = await apiClient.get('trades', {
      params: {
        page: params?.page || 1,
        pageSize: params?.pageSize || 10,
        botId: props.botId || undefined,
        period: props.period,
        searchQuery: params?.searchQuery || props.searchQuery || undefined,
        sortKey: params?.sortKey,
        sortDirection: params?.sortDirection
      }
    });
    return {
      items: response.data.items,
      totalCount: response.data.totalCount
    };
  } catch (e) {
    error.value = e;
    console.error('Error fetching trade data:', e);
    return { items: [], totalCount: 0 };
  } finally {
    isLoading.value = false;
    emit('update:loading', false);
  }
};

watch(() => props.botId, () => {
  // No need to manage currentPage, PagedTable will handle
});

watch(() => props.period, () => {
  // No need to manage currentPage, PagedTable will handle
});

watch(() => props.searchQuery, () => {
  // No need to manage currentPage, PagedTable will handle
});
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