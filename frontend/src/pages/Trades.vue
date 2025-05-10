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
            <TradeTable
              :botId="filterSettings.botId"
              :period="filterSettings.period"
              @view-details="viewTradeDetails"
            />
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
import { ref, reactive, onMounted, watch } from "vue";
import { useRoute } from "vue-router";
import {
  ElSelect,
  ElOption,
  ElDialog,
} from "element-plus";
import Card from "@/components/Cards/Card.vue";
import BaseButton from "@/components/BaseButton.vue";
import TradeTable from '@/components/TradeTable.vue';
import apiClient from "@/services/api";

// State variables
const selectedTrade = ref(null);
const showDetailsModal = ref(false);
const route = useRoute();
const availableBots = ref([]);

// Filter settings
const filterSettings = reactive({
  botId: null,
  period: "month"
});

// Initialize from route query params if available
if (route.query.botId) {
  filterSettings.botId = parseInt(route.query.botId);
}

// View trade details
function viewTradeDetails(trade) {
  selectedTrade.value = trade;
  showDetailsModal.value = true;
}

// Format date helper - used in the modal
function formatDate(dateString) {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString();
}

// Get bot name from bot ID - used in the modal
function getBotName(botId) {
  const bot = availableBots.value.find(b => b.id.toString() === botId.toString());
  return bot ? bot.name : 'Unknown Bot';
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

// When the component mounts
onMounted(() => {
  fetchBots();
});
</script>

<style>
.trade-detail-container {
  padding: 16px;
}
</style>
