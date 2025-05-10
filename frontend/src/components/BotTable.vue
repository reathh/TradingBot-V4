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
      :fetch-data="fetchBotData"
      thead-classes="text-primary"
    >
      <template #row="{ row }">
        <td>{{ row.botId || row.id }}</td>
        <td>{{ row.ticker || row.symbol }}</td>
        <td>{{ formatCurrency(row.entryPrice) }}</td>
        <td>{{ formatCurrency(row.exitPrice) }}</td>
        <td>{{ row.quantity ? row.quantity.toFixed(4) : '0.0000' }}</td>
        <td>{{ formatCurrency(row.entryFee + row.exitFee) }}</td>
        <td class="text-right" :class="getProfitClass(row.profit)">
          {{ formatCurrency(row.profit) }}
        </td>
        <td class="text-right">
          <BaseButton 
            v-if="showViewButton"
            type="info" 
            icon 
            size="sm" 
            class="btn-link"
          >
            <i class="tim-icons icon-zoom-split"></i>
          </BaseButton>
          <BaseButton 
            v-if="showRefreshButton"
            type="success" 
            icon 
            size="sm" 
            class="btn-link"
          >
            <i class="tim-icons icon-refresh-01"></i>
          </BaseButton>
          <BaseButton
            v-if="showToggleStatusButton"
            class="btn-link"
            :type="row.enabled ? 'warning' : 'success'"
            size="sm"
            icon
            @click="$emit('toggle-status', row)"
          >
            <i :class="row.enabled ? 'tim-icons icon-button-pause' : 'tim-icons icon-button-power'"></i>
          </BaseButton>
          <router-link 
            v-if="showTradesButton" 
            :to="{ name: 'Trades', query: { botId: row.id || row.botId } }"
          >
            <BaseButton
              class="btn-link"
              type="info"
              size="sm"
              icon
            >
              <i class="tim-icons icon-chart-bar-32"></i>
            </BaseButton>
          </router-link>
          <BaseButton
            v-if="showEditButton"
            class="edit btn-link"
            type="warning"
            size="sm"
            icon
            @click="$emit('edit', row)"
          >
            <i class="tim-icons icon-pencil"></i>
          </BaseButton>
          <BaseButton
            v-if="showDeleteButton"
            class="remove btn-link"
            type="danger"
            size="sm"
            icon
            @click="$emit('delete', row)"
          >
            <i class="tim-icons icon-simple-remove"></i>
          </BaseButton>
        </td>
      </template>
    </PagedTable>
  </div>
</template>

<script setup>
import { ref, onMounted, defineProps, defineEmits } from "vue";
import { ElTag } from "element-plus";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import botService from "@/services/botService";

const props = defineProps({
  showViewButton: { type: Boolean, default: true },
  showRefreshButton: { type: Boolean, default: true },
  showToggleStatusButton: { type: Boolean, default: false },
  showTradesButton: { type: Boolean, default: false },
  showEditButton: { type: Boolean, default: false },
  showDeleteButton: { type: Boolean, default: false }
});

const emit = defineEmits(['toggle-status', 'edit', 'delete']);

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

// Table columns
const tableColumns = [
  { key: 'botId', label: 'BOT ID' },
  { key: 'ticker', label: 'TICKER' },
  { key: 'entryPrice', label: 'ENTRY PRICE' },
  { key: 'exitPrice', label: 'EXIT PRICE' },
  { key: 'quantity', label: 'QUANTITY' },
  { key: 'fees', label: 'FEES' },
  { key: 'profit', label: 'PROFIT', align: 'right' },
  { key: 'actions', label: 'ACTIONS', align: 'right' }
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

// Fetch bot data with pagination
const fetchBotData = async (params) => {
  try {
    const response = await botService.getTrades({ 
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
    console.error('Error fetching data:', e);
  }
};

// Fetch data on mount
onMounted(() => fetchBotData());
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