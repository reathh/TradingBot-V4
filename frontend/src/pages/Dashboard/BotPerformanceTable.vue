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
      :fetch-data="fetchBotProfits"
      thead-classes="text-primary"
    >
      <template #row="{ row }">
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
          <BaseButton type="info" icon size="sm" class="btn-link">
            <i class="tim-icons icon-zoom-split"></i>
          </BaseButton>
          <BaseButton type="success" icon size="sm" class="btn-link">
            <i class="tim-icons icon-refresh-01"></i>
          </BaseButton>
        </td>
      </template>
    </PagedTable>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import botService from "@/services/botService";

// Data states
const pagedResult = ref({
  page: 1,
  pageSize: 10,
  totalPages: 0,
  totalCount: 0,
  items: []
});

const currentPage = ref(1);
const error = ref(null);

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

// Format currency with $ symbol and 2 decimal places
const formatCurrency = (value) => {
  return `$${parseFloat(value).toFixed(2)}`;
};

// Get CSS class based on profit value
const getProfitClass = (profit) => {
  if (!profit) return '';
  return profit > 0 ? 'text-success' : 'text-danger';
};

// Fetch bot profits with pagination
const fetchBotProfits = async (params) => {
  try {
    const response = await botService.getBotProfits({ 
      page: params?.page || currentPage.value, 
      pageSize: params?.pageSize || 10,
      sortKey: params?.sortKey,
      sortDirection: params?.sortDirection
    });
    
    // Update local state with response data
    pagedResult.value = response.data;
    
    // Sync the current page and page size
    if (params) {
      currentPage.value = params.page || currentPage.value;
    }
  } catch (e) {
    error.value = e;
    console.error('Error fetching bot profits:', e);
  }
};

// Fetch data on mount
onMounted(() => fetchBotProfits());
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