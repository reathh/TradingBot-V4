<template>
  <div class="card-body">
    <PagedTable
      :columns="tableColumns"
      :data="botPerformanceData"
      :searchable="true"
      :sortable="true"
      :page-size-options="pageSizeOptions"
      :default-page-size="pageSize"
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
          <base-button type="info" icon size="sm" class="btn-link">
            <i class="tim-icons icon-zoom-split"></i>
          </base-button>
          <base-button type="success" icon size="sm" class="btn-link">
            <i class="tim-icons icon-refresh-01"></i>
          </base-button>
        </td>
      </template>
    </PagedTable>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from "vue";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import botService from "@/services/botService";

// Data states
const botPerformanceData = ref([]);
const isLoading = ref(false);
const error = ref(null);
const pageSize = ref(5);
const pageSizeOptions = [5, 10, 25, 50];

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

// Fetch bot performance data (dummy for now, replace with real fetch)
onMounted(async () => {
  isLoading.value = true;
  try {
    // Replace with real API call
    botPerformanceData.value = await botService.getPerformance();
  } catch (e) {
    error.value = e;
  } finally {
    isLoading.value = false;
  }
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