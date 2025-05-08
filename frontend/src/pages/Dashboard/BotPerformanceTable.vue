<template>
  <base-table :data="botPerformanceData" thead-classes="text-primary">
    <template #columns>
      <th>Bot ID</th>
      <th>Ticker</th>
      <th>Entry Avg. Price</th>
      <th>Exit Avg. Price</th>
      <th>Quantity</th>
      <th>Fees</th>
      <th class="text-right">Profit</th>
      <th class="text-right">Actions</th>
    </template>

    <template #default="{ row }">
      <td v-if="row">{{ row.botId }}</td>
      <td v-if="row">{{ row.ticker }}</td>
      <td v-if="row">{{ formatCurrency(row.entryAvgPrice) }}</td>
      <td v-if="row">{{ formatCurrency(row.exitAvgPrice) }}</td>
      <td v-if="row">{{ row.quantity }}</td>
      <td v-if="row">{{ formatCurrency(row.entryFee + row.exitFee) }}</td>
      <td class="text-right" v-if="row" :class="getProfitClass(row.profit)">
        {{ formatCurrency(row.profit) }}
      </td>
      <td class="text-right" v-if="row">
        <el-tooltip content="Details" effect="light" :open-delay="300" placement="top">
          <base-button type="info" icon size="sm" class="btn-link">
            <i class="tim-icons icon-zoom-split"></i>
          </base-button>
        </el-tooltip>
        <el-tooltip content="Refresh" effect="light" :open-delay="300" placement="top">
          <base-button type="success" icon size="sm" class="btn-link">
            <i class="tim-icons icon-refresh-01"></i>
          </base-button>
        </el-tooltip>
      </td>
    </template>
  </base-table>
</template>

<script setup>
import { ref, onMounted, defineExpose, onUnmounted } from "vue";
import BaseTable from "@/components/BaseTable.vue";
import BaseButton from "@/components/BaseButton.vue";
import botService from "@/services/botService";

const botPerformanceData = ref([]);
const isLoading = ref(false);
const error = ref(null);

// Format currency with $ symbol and 2 decimal places
const formatCurrency = (value) => {
  return `$${parseFloat(value).toFixed(2)}`;
};

// Get CSS class based on profit value
const getProfitClass = (profit) => {
  if (!profit) return '';
  return profit > 0 ? 'text-success' : 'text-danger';
};

// Fetch bot performance data
const fetchBotPerformance = async () => {
  isLoading.value = true;
  error.value = null;

  try {
    const response = await botService.getBotProfits();
    botPerformanceData.value = response.data;
  } catch (err) {
    console.error('Error fetching bot performance:', err);
    error.value = 'Failed to load bot performance data';
    botPerformanceData.value = [];
  } finally {
    isLoading.value = false;
  }
};

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
</style>