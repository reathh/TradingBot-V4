<template>
  <div class="content">
    <div class="md-layout">
      <div class="md-layout-item md-size-100">
        <card card-body-classes="table-full-width">
          <div slot="header" class="d-flex justify-content-between align-items-center">
            <h4 class="card-title">Trading History</h4>

            <div class="d-flex align-items-center">
              <el-select
                v-model="filterSettings.botId"
                placeholder="Filter by Bot"
                clearable
                class="mr-3"
              >
                <el-option
                  v-for="bot in availableBots"
                  :key="bot.id"
                  :label="bot.name"
                  :value="bot.id"
                />
              </el-select>

              <el-select
                v-model="filterSettings.period"
                placeholder="Period"
                class="mr-3"
              >
                <el-option label="Day" value="day" />
                <el-option label="Week" value="week" />
                <el-option label="Month" value="month" />
                <el-option label="Year" value="year" />
                <el-option label="All Time" value="all" />
              </el-select>

              <base-button @click="fetchTrades" type="primary" size="sm" icon>
                <i class="tim-icons icon-refresh-02"></i>
              </base-button>
            </div>
          </div>

          <div>
            <PagedTable
              :columns="tradeColumns"
              :data="pagedResult.items"
              :page="currentPage"
              :total-pages="pagedResult.totalPages"
              :total-count="pagedResult.totalCount"
              :searchable="true"
              :sortable="true"
              :server-side="true"
              :fetch-data="fetchTrades"
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
                <td>{{ row.entryPrice?.toFixed(2) ?? 'N/A' }}</td>
                <td>{{ row.exitPrice?.toFixed(2) ?? 'N/A' }}</td>
                <td>{{ row.entryAvgPrice?.toFixed(2) ?? 'N/A' }}</td>
                <td>{{ row.exitAvgPrice?.toFixed(2) ?? 'N/A' }}</td>
                <td>{{ row.quantity?.toFixed(4) ?? 'N/A' }}</td>
                <td>{{ row.quantityFilled?.toFixed(4) ?? 'N/A' }}</td>
                <td>
                  <span :class="row.profit > 0 ? 'text-success' : 'text-danger'">
                    {{ row.profit != null ? row.profit.toFixed(2) : 'N/A' }}
                  </span>
                </td>
                <td>{{ formatDate(row.entryTime) }}</td>
                <td>{{ formatDate(row.exitTime) }}</td>
                <td>
                  <router-link :to="{ name: 'Bot', params: { id: row.botId.toString() } }" class="text-info">
                    {{ getBotName(row.botId) }}
                  </router-link>
                </td>
                <td class="text-right">
                  <base-button
                    @click.native="viewTradeDetails(row)"
                    class="btn-link"
                    type="info"
                    size="sm"
                    icon
                  >
                    <i class="tim-icons icon-chart-pie-36"></i>
                  </base-button>
                </td>
              </template>
            </PagedTable>
          </div>
        </card>
      </div>
    </div>

    <!-- Trade Details Modal -->
    <el-dialog
      title="Trade Details"
      v-model="showDetailsModal"
      width="600px"
    >
      <div v-if="selectedTrade" class="trade-detail-container">
        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Trade ID:</strong> {{ selectedTrade.tradeId }}
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
              {{ selectedTrade.profit ? selectedTrade.profit.toFixed(2) : 'N/A' }}
            </span>
          </div>
        </div>

        <h5 class="mt-4">Entry Details</h5>
        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Price:</strong> {{ selectedTrade.entryPrice?.toFixed(2) ?? 'N/A' }}
          </div>
          <div class="col-md-6">
            <strong>Average Fill:</strong> {{ selectedTrade.entryAvgPrice?.toFixed(2) ?? 'N/A' }}
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
            <strong>Price:</strong> {{ selectedTrade.exitPrice?.toFixed(2) ?? 'N/A' }}
          </div>
          <div class="col-md-6">
            <strong>Average Fill:</strong> {{ selectedTrade.exitAvgPrice?.toFixed(2) ?? 'N/A' }}
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
import { ref, reactive, onMounted, computed, watch } from "vue";
import { useRoute } from "vue-router";
import {
  ElTable,
  ElTableColumn,
  ElSelect,
  ElOption,
  ElDialog,
} from "element-plus";
import Card from "@/components/Cards/Card.vue";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from '@/components/PagedTable.vue';
import apiClient from "@/services/api";

// State variables
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
const selectedTrade = ref(null);
const showDetailsModal = ref(false);
const route = useRoute();

// Filter settings
const filterSettings = reactive({
  botId: null,
  period: "month"
});

// Initialize from route query params if available
if (route.query.botId) {
  filterSettings.botId = parseInt(route.query.botId);
}

// Table columns definition
const tradeColumns = [
  { key: 'tradeId', label: 'ID', minWidth: 70 },
  { key: 'symbol', label: 'Symbol', minWidth: 100 },
  { key: 'direction', label: 'Direction', minWidth: 100 },
  { key: 'entryPrice', label: 'Entry Price', minWidth: 120 },
  { key: 'exitPrice', label: 'Exit Price', minWidth: 120 },
  { key: 'entryAvgPrice', label: 'Entry Avg Price', minWidth: 120 },
  { key: 'exitAvgPrice', label: 'Exit Avg Price', minWidth: 120 },
  { key: 'quantity', label: 'Quantity', minWidth: 120 },
  { key: 'quantityFilled', label: 'Filled', minWidth: 120 },
  { key: 'profit', label: 'Profit', minWidth: 120 },
  { key: 'entryTime', label: 'Entry Date', minWidth: 160 },
  { key: 'exitTime', label: 'Exit Date', minWidth: 160 },
  { key: 'botId', label: 'Bot', minWidth: 120 },
  { key: 'actions', label: '', minWidth: 80, align: 'right' },
];

// Fetch trades with pagination and filters
async function fetchTrades(params) {
  isLoading.value = true;
  try {
    const response = await apiClient.get('trades', {
      params: {
        page: params?.page || currentPage.value,
        pageSize: params?.pageSize || 10,
        botId: filterSettings.botId || undefined,
        period: filterSettings.period,
        searchQuery: params?.searchQuery || undefined,
        sortKey: params?.sortKey,
        sortDirection: params?.sortDirection
      }
    });

    pagedResult.value = response.data;

    // Sync the current page and page size
    if (params && params.page) {
      currentPage.value = params.page;
    }
  } catch (error) {
    console.error('Error fetching trades:', error);
  } finally {
    isLoading.value = false;
  }
}

// Fetch available bots for the filter dropdown
async function fetchBots() {
  try {
    const response = await apiClient.get('bots', {
      params: { pageSize: 100 } // Get a larger number of bots for the filter
    });
    availableBots.value = response.data.items;
  } catch (error) {
    console.error('Error fetching bots:', error);
  }
}

// View trade details
function viewTradeDetails(trade) {
  selectedTrade.value = trade;
  showDetailsModal.value = true;
}

// Format date helper
function formatDate(dateString) {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString();
}

// Get bot name from bot ID
function getBotName(botId) {
  const bot = availableBots.value.find(b => b.id.toString() === botId.toString());
  return bot ? bot.name : 'Unknown Bot';
}

// Watch for filter changes
watch(filterSettings, () => {
  currentPage.value = 1; // Reset to first page on filter change
  fetchTrades();
});

// Fetch data on mount
onMounted(() => {
  fetchBots();
  fetchTrades();
});
</script>

<style>
.trade-detail-container {
  padding: 16px;
}
</style>
