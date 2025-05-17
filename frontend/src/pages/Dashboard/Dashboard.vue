<template>
  <div class="row">
    <div class="col-12">
      <Card type="chart">
        <template #header>
          <div class="row">
            <div class="col-md-6 col-sm-12">
              <h5 class="card-category">Trading Analytics</h5>
              <h2 class="card-title">Bot Profit Performance</h2>
            </div>
            <div class="col-md-6 col-sm-12 d-flex flex-md-row flex-column justify-content-md-end justify-content-sm-start align-items-start align-items-md-center">
              <!-- Date range picker with quick shortcuts -->

              <el-date-picker
                v-model="dateRange"
                type="datetimerange"
                :shortcuts="rangeShortcuts"
                size="large"
                class="select-primary me-md-2 mb-2 mb-md-0 w-100"
                start-placeholder="Start"
                end-placeholder="End"
                @change="fetchStatsData"
              />

              <el-select
                v-model="selectedInterval"
                class="select-primary w-100"
                size="large"
                placeholder="Select Interval"
                @change="fetchStatsData"
              >
                <el-option
                  v-for="interval in intervalChoices"
                  :key="interval.value"
                  :label="interval.label"
                  :value="interval.value"
                  class="select-primary"
                />
              </el-select>
            </div>
          </div>
        </template>

        <div class="chart-area">
          <LineChart
            :chart-data="performanceChartData"
            :options="lineChartOptions"
          />
        </div>
      </Card>
    </div>
    <div class="col-lg-3 col-md-6" v-for="card in statsCards" :key="card.title">
      <stats-card
        :title="card.title"
        :sub-title="card.subTitle"
        :type="card.type"
        :icon="card.icon"
        :footer-icon="card.footerIcon"
        :footer="card.footer"
      >
      </stats-card>
    </div>

    <div class="col-lg-4">
      <Card type="chart">
        <template #header>
          <h5 class="card-category">ROI (24h)</h5>
          <h3 class="card-title">
            <i class="tim-icons icon-bell-55 text-primary"></i> {{ roi24h.toFixed(2) }}%
          </h3>
        </template>
        <div class="chart-area">
          <LineChart
            :chart-data="purpleLineChartData"
            :options="lineChartOptions"
            style="height: 100%"
          />
        </div>
      </Card>
    </div>
    <div class="col-lg-4">
      <Card type="chart">
        <template #header>
          <h5 class="card-category">Base Volume (24h)</h5>
          <h3 class="card-title">
            <i class="tim-icons icon-delivery-fast text-info"></i> {{ baseVolume24h.toLocaleString() }}
          </h3>
        </template>
        <div class="chart-area">
          <bar-chart
            style="height: 100%"
            :chart-data="blueBarChartData"
            :gradient-stops="blueBarChartGradientStops"
            :extra-options="blueBarChartExtraOptions"
          >
          </bar-chart>
        </div>
      </Card>
    </div>
    <div class="col-lg-4">
      <Card type="chart">
        <template #header>
          <h5 class="card-category">Quote Volume (24h)</h5>
          <h3 class="card-title">
            <i class="tim-icons icon-send text-success"></i> {{ quoteVolume24h.toLocaleString() }}
          </h3>
        </template>
        <div class="chart-area">
          <LineChart
            :chart-data="greenLineChartData"
            style="height: 100%"
            :options="lineChartOptions"
          />
        </div>
      </Card>
    </div>
    <div class="col-lg-5">
      <Card type="tasks">
        <template #header>
          <h6 class="title d-inline">Tasks (5)</h6>
          <p class="card-category d-inline">Today</p>
          <base-dropdown
            menu-on-right=""
            tag="div"
            title-classes="btn btn-link btn-icon"
          >
            <i slot="title" class="tim-icons icon-settings-gear-63"></i>
            <a class="dropdown-item" href="#"> Action </a>
            <a class="dropdown-item" href="#"> Another action </a>
            <a class="dropdown-item" href="#"> Something else </a>
          </base-dropdown>
        </template>
        <div class="table-full-width table-responsive">
          <task-list></task-list>
        </div>
      </Card>
    </div>
    <div class="col-lg-7">
      <card class="card">
        <h5 slot="header" class="card-title">Trades</h5>
        <div class="table-responsive"><TradesTable :period="selectedInterval" /></div>
      </card>
    </div>
    <div class="col-lg-12">
      <Card class="card">
        <template #header>
          <h5 class="card-title mb-0">Active Orders</h5>
        </template>
        <div class="table-responsive">
          <OrdersTable
            :period="selectedInterval"
          />
        </div>
      </Card>
    </div>
    <div class="col-lg-12"><CountryMapCard></CountryMapCard></div>
  </div>
</template>
<script setup>
import {
  ref,
  reactive,
  computed,
  onMounted,
  onBeforeUnmount,
  inject,
  watch,
} from "vue";
import { ElSelect, ElOption, ElDatePicker } from "element-plus";
import "element-plus/es/components/select/style/css";
import "element-plus/es/components/option/style/css";
import "element-plus/es/components/date-picker/style/css";
import LineChart from "@/components/Charts/LineChart.vue";
import BarChart from "@/components/Charts/BarChart.vue";
import * as chartConfigs from "@/components/Charts/config";
import TaskList from "@/pages/Dashboard/TaskList.vue";
import UserTable from "@/pages/Dashboard/UserTable.vue";
import BotTable from "@/components/BotTable.vue";
import TradesTable from '@/components/TradesTable.vue';
import CountryMapCard from "@/pages/Dashboard/CountryMapCard.vue";
import StatsCard from "@/components/Cards/StatsCard.vue";
import BaseDropdown from "@/components/BaseDropdown.vue";
import Card from "@/components/Cards/Card.vue";
import config from "@/config";
import { useRoute } from "vue-router";
import botService from "@/services/botService";
import OrdersTable from '@/components/OrdersTable.vue';
import signalrService from "@/services/signalrService";

const months = [
  "JAN",
  "FEB",
  "MAR",
  "APR",
  "MAY",
  "JUN",
  "JUL",
  "AUG",
  "SEP",
  "OCT",
  "NOV",
  "DEC",
];
const performanceChartData = ref({ labels: [], datasets: [] });
const purpleLineChartData = ref({ labels: [], datasets: [] });
const lineChartOptions = ref({
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    y: {
      beginAtZero: true,
      grid: {
        color: "rgba(255, 255, 255, 0.1)",
      },
      ticks: {
        color: "rgba(255, 255, 255, 0.7)",
        callback: function(value) {
          return '$' + value;
        }
      },
    },
    x: {
      grid: {
        color: "rgba(255, 255, 255, 0.1)",
      },
      ticks: {
        color: "rgba(255, 255, 255, 0.7)",
      },
    },
  },
  plugins: {
    legend: {
      display: false,
    },
    tooltip: {
      callbacks: {
        label: function(context) {
          return `Profit: $${context.raw}`;
        }
      }
    }
  },
});

const statsCards = ref([
  {
    title: "150GB",
    subTitle: "Number",
    type: "warning",
    icon: "tim-icons icon-chat-33",
    footerIcon: "tim-icons icon-refresh-01",
    footer: "Update Now",
  },
  {
    title: "+45K",
    subTitle: "Followers",
    type: "primary",
    icon: "tim-icons icon-shape-star",
    footerIcon: "tim-icons icon-sound-wave",
    footer: "Last Research",
  },
  {
    title: "150,000",
    subTitle: "Users",
    type: "info",
    icon: "tim-icons icon-single-02",
    footerIcon: "tim-icons icon-trophy",
    footer: "Customer feedback",
  },
  {
    title: "23",
    subTitle: "Errors",
    type: "danger",
    icon: "tim-icons icon-molecule-40",
    footerIcon: "tim-icons icon-watch-time",
    footer: "In the last hours",
  },
]);

const purpleLineChartExtraOptions = ref(chartConfigs.purpleChartOptions);
const purpleLineChartGradientColors = ref(config.colors.primaryGradient);
const purpleLineChartGradientStops = ref([1, 0.2, 0]);
// greenLineChartData;
const greenLineChartData = ref({ labels: [], datasets: [] });
const greenLineChartExtraOptions = ref(chartConfigs.greenChartOptions);
const greenLineChartGradientColors = ref([
  "rgba(66,134,121,0.15)",
  "rgba(66,134,121,0.0)",
  "rgba(66,134,121,0)",
]);
const greenLineChartGradientStops = ref([1, 0.4, 0]);

const blueBarChartData = ref({ labels: [], datasets: [] });
const blueBarChartExtraOptions = ref(chartConfigs.barChartOptions);
const blueBarChartGradientStops = ref([1, 0.4, 0]);

// Reactive date range (default last 7 days)
const dateRange = ref([new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), new Date()]);

// Quick-select shortcuts similar to Seq
const rangeShortcuts = [
  { text: 'Last 5m', value: () => [new Date(Date.now() - 5 * 60 * 1000), new Date()] },
  { text: 'Last 15m', value: () => [new Date(Date.now() - 15 * 60 * 1000), new Date()] },
  { text: 'Last 1h', value: () => [new Date(Date.now() - 60 * 60 * 1000), new Date()] },
  { text: 'Last 1d', value: () => [new Date(Date.now() - 24 * 60 * 60 * 1000), new Date()] },
  { text: 'Last 7d', value: () => [new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), new Date()] },
  { text: 'Last 30d', value: () => [new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), new Date()] },
  { text: 'Last 90d', value: () => [new Date(Date.now() - 90 * 24 * 60 * 60 * 1000), new Date()] },
  { text: 'Last 365d', value: () => [new Date(Date.now() - 365 * 24 * 60 * 60 * 1000), new Date()] }
];

// Compute interval choices based on range length
const intervalChoices = computed(() => {
  const [start, end] = dateRange.value;
  if (!start || !end) return [];

  const diffMs = Math.abs(end - start);
  const diffMinutes = diffMs / (1000 * 60);
  const diffHours = diffMs / (1000 * 60 * 60);
  const diffDays = diffMs / (1000 * 60 * 60 * 24);
  const diffMonths = diffDays / 30;
  const diffYears = diffDays / 365;

  if (diffMinutes <= 5) {
    return [
      { label: 'Seconds', value: 'Second' },
      { label: 'Minutes', value: 'Minute' }
    ];
  } else if (diffHours <= 1) {
    return [
      { label: 'Minutes', value: 'Minute' }
    ];
  } else if (diffHours < 24) {
    return [
      { label: 'Hourly', value: 'Hour' }
    ];
  } else if (diffDays <= 7) {
    return [
      { label: 'Hourly', value: 'Hour' },
      { label: 'Daily', value: 'Day' }
    ];
  } else if (diffDays <= 30) {
    return [
      { label: 'Daily', value: 'Day' },
      { label: 'Weekly', value: 'Week' }
    ];
  } else if (diffDays <= 90) {
    return [
      { label: 'Daily', value: 'Day' },
      { label: 'Weekly', value: 'Week' },
      { label: 'Monthly', value: 'Month' }
    ];
  } else if (diffDays <= 365) {
    return [
      { label: 'Weekly', value: 'Week' },
      { label: 'Monthly', value: 'Month' }
    ];
  }
  return [
    { label: 'Weekly', value: 'Week' },
    { label: 'Monthly', value: 'Month' },
    { label: 'Yearly', value: 'Year' }
  ];
});

const selectedInterval = ref('Day');

// Watch for changes in date range and adjust selected interval if needed
watch(dateRange, () => {
  // Get available interval options
  const availableIntervals = intervalChoices.value.map(option => option.value);

  // If currently selected interval is not available, select the first available option
  if (!availableIntervals.includes(selectedInterval.value) && availableIntervals.length > 0) {
    selectedInterval.value = availableIntervals[0];
  }
}, { deep: true });

// SignalR subscriptions
let unsubscribeOrderUpdated;

const roi24h = ref(0);
const quoteVolume24h = ref(0);
const baseVolume24h = ref(0);

const fetchStatsData = async () => {
  try {
    const [start, end] = dateRange.value;
    const response = await botService.getStats(
      selectedInterval.value,
      null,
      start ? start.toISOString() : undefined,
      end ? end.toISOString() : undefined
    );

    if (response && response.data) {
      const { stats: profitData, roi24h: roi, quoteVolume24h: quoteVol, baseVolume24h: baseVol } = response.data;

      roi24h.value = roi;
      quoteVolume24h.value = quoteVol;
      baseVolume24h.value = baseVol;

      // Transform aggregated data for chart format
      profitData.sort((a, b) => new Date(a.periodStart || a.PeriodStart) - new Date(b.periodStart || b.PeriodStart));

      // Extract labels and data points
      const labels = profitData.map(item => {
        const start = item.periodStart || item.PeriodStart;
        // Format the label based on interval
        switch (selectedInterval.value) {
          case 'Second': return new Date(start).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
          case 'Minute': return new Date(start).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
          case 'Hour': return new Date(start).toLocaleTimeString([], { hour: '2-digit' }) + ':00';
          case 'Day': return new Date(start).toLocaleDateString([], { day: 'numeric' });
          case 'Week': return 'W' + Math.ceil(new Date(start).getDate() / 7);
          case 'Month': return new Date(start).toLocaleDateString([], { month: 'short' });
          case 'Year': return new Date(start).getFullYear().toString();
          default: return item.timePeriod || item.TimePeriod;
        }
      });

      const totalProfitSeries = profitData.map(item => item.totalProfit ?? item.TotalProfit);

      const roiSeries = profitData.map(item => item.profitPct ?? item.ProfitPct);
      const baseVolSeries = profitData.map(item => item.baseVolume ?? item.BaseVolume);
      const quoteVolSeries = profitData.map(item => item.quoteVolume ?? item.QuoteVolume);

      // Update KPI charts
      purpleLineChartData.value = {
        labels,
        datasets: [{
          label: 'ROI %',
          data: roiSeries,
          borderColor: '#41B883',
          backgroundColor: 'rgba(65, 184, 131, 0.1)',
          borderWidth: 3,
          pointRadius: 4,
          pointBackgroundColor: '#41B883',
          tension: 0.4,
          fill: false,
        }]
      };

      blueBarChartData.value = {
        labels,
        datasets: [{
          label: 'Base Volume',
          backgroundColor: config.colors.info,
          data: baseVolSeries,
          borderWidth: 2,
        }]
      };

      greenLineChartData.value = {
        labels,
        datasets: [{
          label: 'Quote Volume',
          data: quoteVolSeries,
          borderColor: config.colors.danger,
          backgroundColor: 'rgba(255,99,132,0.15)',
          borderWidth: 3,
          pointRadius: 4,
          pointBackgroundColor: config.colors.danger,
          tension: 0.4,
          fill: false,
        }]
      };

      performanceChartData.value = {
        labels,
        datasets: [
          {
            label: 'Bot Profit',
            data: totalProfitSeries,
            borderColor: '#41B883',
            backgroundColor: 'rgba(65, 184, 131, 0.1)',
            borderWidth: 3,
            pointRadius: 4,
            pointBackgroundColor: '#41B883',
            tension: 0.4,
            fill: false,
          },
        ],
      };
    }
  } catch (err) {
    console.error('Error fetching stats data:', err);
  }
};

const isEndNearNow = () => {
  const end = dateRange.value[1];
  if (!end) return false;
  return Math.abs(Date.now() - end.getTime()) < 2 * 60 * 1000; // within 2 minutes
};

onMounted(() => {
  fetchStatsData();

  // Subscribe to order updates from SignalR - when an order changes, refresh profit data
  unsubscribeOrderUpdated = signalrService.onOrderUpdated(() => {
    if (isEndNearNow()) {
      dateRange.value[1] = new Date(); // advance end to now
    }
    fetchStatsData();
  });
});

onBeforeUnmount(() => {
  // Clean up subscriptions
  if (unsubscribeOrderUpdated) unsubscribeOrderUpdated();
});
</script>
