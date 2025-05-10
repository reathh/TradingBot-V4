<template>
  <div>
    <PagedTable
      :columns="tableColumns"
      :data="pagedResult.items"
      :page="currentPage"
      :total-pages="pagedResult.totalPages"
      :total-count="pagedResult.totalCount"
      :searchable="searchable"
      :sortable="true"
      :server-side="true"
      :fetch-data="fetchData"
      :loading="isLoading"
      thead-classes="text-primary"
    >
      <template #row="{ row }">
        <td>{{ row.tradeId }}</td>
        <td>{{ row.symbol }}</td>
        <td>
          <span :class="row.direction === 'Buy' ? 'text-success' : 'text-danger'">
            {{ row.direction }}
          </span>
        </td>
        <td>{{ formatNumber(row.entryPrice) }}</td>
        <td>{{ formatNumber(row.exitPrice) }}</td>
        <td>{{ formatNumber(row.entryAvgPrice) }}</td>
        <td>{{ formatNumber(row.exitAvgPrice) }}</td>
        <td>{{ formatQuantity(row.quantity) }}</td>
        <td>{{ formatQuantity(row.quantityFilled) }}</td>
        <td :class="getProfitClass(row.profit)">
          {{ formatCurrency(row.profit) }}
        </td>
        <td>{{ formatDate(row.entryTime) }}</td>
        <td>{{ formatDate(row.exitTime) }}</td>
        <td>
          <router-link :to="{ name: 'Bot', params: { id: row.botId?.toString() } }" class="text-info">
            {{ getBotName(row.botId) }}
          </router-link>
        </td>
        <td class="text-right">
          <BaseButton 
            v-if="showViewButton"
            @click="viewDetails(row)"
            type="info" 
            icon 
            size="sm" 
            class="btn-link"
          >
            <i class="tim-icons" :class="detailsIcon"></i>
          </BaseButton>
          
          <BaseButton 
            v-if="showRefreshButton"
            @click="refreshRow(row)"
            type="success" 
            icon 
            size="sm" 
            class="btn-link"
          >
            <i class="tim-icons icon-refresh-01"></i>
          </BaseButton>
        </td>
      </template>
    </PagedTable>
  </div>
</template>

<script setup>
import { ref, onMounted, defineProps, defineEmits } from "vue";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import apiClient from "@/services/api";

const props = defineProps({
  // Display options
  searchable: { type: Boolean, default: true },
  
  // Action buttons
  showViewButton: { type: Boolean, default: true },
  showRefreshButton: { type: Boolean, default: false },
  
  // Filter options
  botId: { type: [Number, String], default: null },
  period: { type: String, default: 'month' },
  
  // Icon configuration
  detailsIcon: { type: String, default: 'icon-chart-pie-36' }
});

const emit = defineEmits(['view-details', 'refresh']);

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
const availableBots = ref([]);

// Table columns definition
const tableColumns = [
  { key: 'tradeId', label: 'ID', minWidth: 70 },
  { key: 'symbol', label: 'SYMBOL', minWidth: 100 },
  { key: 'direction', label: 'DIRECTION', minWidth: 100 },
  { key: 'entryPrice', label: 'ENTRY PRICE', minWidth: 120 },
  { key: 'exitPrice', label: 'EXIT PRICE', minWidth: 120 },
  { key: 'entryAvgPrice', label: 'ENTRY AVG PRICE', minWidth: 120 },
  { key: 'exitAvgPrice', label: 'EXIT AVG PRICE', minWidth: 120 },
  { key: 'quantity', label: 'QUANTITY', minWidth: 120 },
  { key: 'quantityFilled', label: 'FILLED', minWidth: 120 },
  { key: 'profit', label: 'PROFIT', minWidth: 120, align: 'right' },
  { key: 'entryTime', label: 'ENTRY DATE', minWidth: 160 },
  { key: 'exitTime', label: 'EXIT DATE', minWidth: 160 },
  { key: 'botId', label: 'BOT', minWidth: 120 },
  { key: 'actions', label: '', minWidth: 80, align: 'right' }
];

// Fetch bot names for display
async function fetchBots() {
  try {
    const response = await apiClient.get('bots', {
      params: { pageSize: 100 }
    });
    availableBots.value = response.data.items;
  } catch (error) {
    console.error('Error fetching bots:', error);
  }
}

// Fetch data with pagination, sorting and filtering
async function fetchData(params) {
  isLoading.value = true;
  
  try {
    const response = await apiClient.get('trades', {
      params: {
        page: params?.page || currentPage.value,
        pageSize: params?.pageSize || 10,
        botId: props.botId || undefined,
        period: props.period,
        searchQuery: params?.searchQuery || undefined,
        sortKey: params?.sortKey,
        sortDirection: params?.sortDirection
      }
    });
    
    pagedResult.value = response.data;
    
    // Sync the current page
    if (params && params.page) {
      currentPage.value = params.page;
    }
  } catch (error) {
    console.error('Error fetching trades data:', error);
  } finally {
    isLoading.value = false;
  }
}

// View details of a trade
function viewDetails(row) {
  emit('view-details', row);
}

// Refresh a particular row's data
function refreshRow(row) {
  emit('refresh', row);
}

// Format helpers
function formatCurrency(value) {
  if (value === undefined || value === null) return 'N/A';
  return `$${parseFloat(value).toFixed(2)}`;
}

function formatNumber(value) {
  if (value === undefined || value === null) return 'N/A';
  return parseFloat(value).toFixed(2);
}

function formatQuantity(value) {
  if (value === undefined || value === null) return 'N/A';
  return parseFloat(value).toFixed(4);
}

function formatDate(dateString) {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString();
}

// Get CSS class based on profit value
function getProfitClass(profit) {
  if (!profit) return '';
  return profit > 0 ? 'text-success' : 'text-danger';
}

// Get bot name from bot ID
function getBotName(botId) {
  if (!botId) return 'N/A';
  const bot = availableBots.value.find(b => b.id.toString() === botId.toString());
  return bot ? bot.name : 'Unknown Bot';
}

// On component mount
onMounted(() => {
  fetchBots();
  fetchData();
});
</script>

<style>
.text-success {
  color: #00f2c3 !important;
}

.text-danger {
  color: #fd5d93 !important;
}
</style> 