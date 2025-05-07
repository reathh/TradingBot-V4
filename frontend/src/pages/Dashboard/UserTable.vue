<template>
  <div>
    <base-table
      :data="recentTrades"
      :columns="columns"
      thead-classes="text-primary"
    >
      <template #columns>
        <th>Symbol</th>
        <th>Direction</th>
        <th>Entry Price</th>
        <th>Exit Price</th>
        <th>P/L</th>
        <th>Date</th>
        <th>Status</th>
      </template>
      <template #default="{ row }">
        <td>{{ row.symbol }}</td>
        <td>
          <span :class="row.direction === 'Buy' ? 'text-success' : 'text-danger'">
            {{ row.direction }}
          </span>
        </td>
        <td>{{ formatPrice(row.entryPrice) }}</td>
        <td>{{ formatPrice(row.exitPrice) }}</td>
        <td :class="row.profitLoss >= 0 ? 'text-success' : 'text-danger'">
          {{ formatPrice(row.profitLoss) }}
        </td>
        <td>{{ formatDate(row.timestamp) }}</td>
        <td>
          <span
            :class="{
              'badge': true,
              'badge-success': row.status === 'Completed',
              'badge-warning': row.status !== 'Completed'
            }"
          >
            {{ row.status }}
          </span>
        </td>
      </template>
    </base-table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import BaseTable from '@/components/BaseTable.vue';
import dashboardService from '@/services/dashboard';

// Table columns
const columns = ['Symbol', 'Direction', 'Entry Price', 'Exit Price', 'P/L', 'Date', 'Status'];

// Recent trades data
const recentTrades = ref([]);

// Format price with 2 decimal places and $ sign
const formatPrice = (price) => {
  return `$${parseFloat(price).toFixed(2)}`;
};

// Format date
const formatDate = (dateString) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

// Fetch recent trades
const fetchRecentTrades = async () => {
  try {
    const response = await dashboardService.getRecentTrades();
    recentTrades.value = response.data;
  } catch (error) {
    console.error('Error fetching recent trades:', error);
  }
};

onMounted(() => {
  fetchRecentTrades();
});
</script>

<style>
.badge {
  padding: 0.25rem 0.5rem;
  border-radius: 0.25rem;
  font-size: 0.75rem;
  font-weight: 700;
}
.badge-success {
  background-color: rgba(66, 134, 121, 0.3);
  color: #42b883;
}
.badge-warning {
  background-color: rgba(251, 175, 0, 0.3);
  color: #fbaf00;
}
</style>
