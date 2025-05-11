<template>
  <div class="content">
    <div class="md-layout">
      <div class="md-layout-item md-size-100">
        <card card-body-classes="table-full-width">
          <div slot="header" class="d-flex justify-content-between align-items-center">
            <h4 class="card-title">Order History</h4>

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

              <base-button @click="refreshOrders" type="primary" size="sm" icon>
                <i class="tim-icons icon-refresh-02"></i>
              </base-button>
            </div>
          </div>

          <OrdersTable
            :botId="filterSettings.botId"
            :period="filterSettings.period"
            @update:loading="isLoading = $event"
            ref="ordersTableRef"
          />
        </card>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from "vue";
import { useRoute } from "vue-router";
import { ElSelect, ElOption } from "element-plus";
import Card from "@/components/Cards/Card.vue";
import BaseButton from "@/components/BaseButton.vue";
import OrdersTable from '@/components/OrdersTable.vue';
import apiClient from "@/services/api";

// State variables
const isLoading = ref(false);
const availableBots = ref([]);
const route = useRoute();
const ordersTableRef = ref(null);

// Filter settings
const filterSettings = reactive({
  botId: null,
  period: "month"
});

// Initialize from route query params if available
if (route.query.botId) {
  filterSettings.botId = parseInt(route.query.botId);
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

// Refresh orders data
function refreshOrders() {
  if (ordersTableRef.value) {
    ordersTableRef.value.fetchOrderData();
  }
}

// Watch for filter changes
watch(filterSettings, () => {
  if (ordersTableRef.value) {
    ordersTableRef.value.fetchOrderData();
  }
});

// Fetch data on mount
onMounted(() => {
  fetchBots();
});
</script> 