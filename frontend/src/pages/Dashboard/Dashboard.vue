<template>
  <div class="row">
    <div class="col-12">
      <Card type="chart">
        <template #header>
          <div class="row">
            <div class="col-sm-6">
              <h5 class="card-category">Trading Analytics</h5>
              <h2 class="card-title">Bot Profit Performance</h2>
            </div>
            <div class="col-sm-6 d-flex d-sm-block">
              <div
                class="btn-group btn-group-toggle float-right"
                data-toggle="buttons"
              >
                <label
                  v-for="(period, index) in timePeriods"
                  :key="period.value"
                  class="btn btn-sm btn-primary btn-simple"
                  :class="{ active: selectedPeriodIndex === index }"
                  :id="index"
                >
                  <input
                    type="radio"
                    @click="changePeriod(index)"
                    name="periods"
                    autocomplete="off"
                    :checked="selectedPeriodIndex === index"
                  />
                  <span class="d-none d-sm-block">{{ period.label }}</span>
                  <span class="d-block d-sm-none">
                    <i :class="period.icon"></i>
                  </span>
                </label>
              </div>
            </div>
          </div>
        </template>

        <div class="chart-container">
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
          <h5 class="card-category">Total Shipments</h5>
          <h3 class="card-title">
            <i class="tim-icons icon-bell-55 text-primary"></i> 763,215
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
          <h5 class="card-category">Daily Sales</h5>
          <h3 class="card-title">
            <i class="tim-icons icon-delivery-fast text-info"></i> 3,500€
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
          <h5 class="card-category">Completed tasks</h5>
          <h3 class="card-title">
            <i class="tim-icons icon-send text-success"></i> 12,100K
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
            <a class="dropdown-item" href="#pablo"> Action </a>
            <a class="dropdown-item" href="#pablo"> Another action </a>
            <a class="dropdown-item" href="#pablo"> Something else </a>
          </base-dropdown>
        </template>
        <div class="table-full-width table-responsive">
          <task-list></task-list>
        </div>
      </Card>
    </div>
    <div class="col-lg-7">
      <Card class="card">
        <template #header>
          <h5 class="card-title mb-0">Bot Performance</h5>
        </template>
        <div class="table-responsive"><BotPerformanceTable></BotPerformanceTable></div>
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
} from "vue";
import LineChart from "@/components/Charts/LineChart.vue";
import BarChart from "@/components/Charts/BarChart.vue";
import * as chartConfigs from "@/components/Charts/config";
import TaskList from "@/pages/Dashboard/TaskList.vue";
import UserTable from "@/pages/Dashboard/UserTable.vue";
import BotPerformanceTable from "@/pages/Dashboard/BotPerformanceTable.vue";
import CountryMapCard from "@/pages/Dashboard/CountryMapCard.vue";
import StatsCard from "@/components/Cards/StatsCard.vue";
import BaseDropdown from "@/components/BaseDropdown.vue";
import Card from "@/components/Cards/Card.vue";
import config from "@/config";
import { useRoute } from "vue-router";
import botService from "@/services/botService";

const route = useRoute();

const bigChartRef = ref(null);
const bigLineChartActiveIndex = ref(0);
const bigChartDataValues = [
  [100, 70, 90, 70, 85, 60, 75, 60, 90, 80, 110, 100],
  [80, 120, 105, 110, 95, 105, 90, 100, 80, 95, 70, 120],
  [60, 80, 65, 130, 80, 105, 90, 130, 70, 115, 60, 130],
];
const bigChartLabels = [
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
const performanceChartData = ref({
  labels: months,
  datasets: [
    {
      label: "Performance",
      data: [100, 70, 90, 70, 85, 60, 75, 60, 90, 80, 110, 100],
      borderColor: "#41B883",
      backgroundColor: "rgba(65, 184, 131, 0.1)",
      borderWidth: 3,
      pointRadius: 4,
      pointBackgroundColor: "#41B883",
      tension: 0.4,
      fill: false,
    },
  ],
});
const purpleLineChartData = ref({
  labels: ["JUL", "AUG", "SEP", "OCT", "NOV", "DEC"],
  datasets: [
    {
      label: "Performance",
      data: [80, 100, 70, 80, 120, 80],
      borderColor: "#41B883",
      backgroundColor: "rgba(65, 184, 131, 0.1)",
      borderWidth: 3,
      pointRadius: 4,
      pointBackgroundColor: "#41B883",
      tension: 0.4,
      fill: false,
    },
  ],
  // datasets: [
  //   {
  //     label: "Data",
  //     fill: true,
  //     borderColor: config.colors.primary,
  //     borderWidth: 2,
  //     borderDash: [],
  //     borderDashOffset: 0.0,
  //     backgroundColor: config.colors.primary,
  //     BorderColor: "rgba(255,255,255,0)",
  //     hoverBackgroundColor: config.colors.primary,
  //     borderWidth: 20,
  //     hoverRadius: 4,
  //     hoverBorderWidth: 15,
  //     radius: 4,
  //     data: [80, 100, 70, 80, 120, 80],
  //   },
  // ],
});
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
const bigChartDatasetOptions = {
  fill: true,
  borderColor: config.colors.primary,
  borderWidth: 2,
  borderDash: [],
  borderDashOffset: 0.0,
  pointBackgroundColor: config.colors.primary,
  pointBorderColor: "rgba(255,255,255,0)",
  pointHoverBackgroundColor: config.colors.primary,
  pointBorderWidth: 20,
  pointHoverRadius: 4,
  pointHoverBorderWidth: 15,
  pointRadius: 4,
};

const bigLineChartData = ref({
  datasets: [
    {
      ...bigChartDatasetOptions,
      data: bigChartDataValues[0],
    },
  ],
  labels: bigChartLabels,
});
const bigLineChartExtraOptions = ref(chartConfigs.purpleChartOptions);
const bigLineChartGradientColors = ref(config.colors.primaryGradient);
const bigLineChartGradientStops = ref([1, 0.4, 0]);

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
const greenLineChartData = ref({
  labels: ["JUL", "AUG", "SEP", "OCT", "NOV"],
  datasets: [
    {
      label: "My First dataset",
      data: [90, 27, 60, 12, 80],
      borderColor: config.colors.danger,
      borderWidth: 3,
      pointRadius: 4,
      pointBackgroundColor: config.colors.danger,
      pointBorderColor: "rgba(255,255,255,0)",
      pointHoverBackgroundColor: config.colors.danger,
      tension: 0.4,
      fill: true,
    },
  ],
});
const greenLineChartExtraOptions = ref(chartConfigs.greenChartOptions);
const greenLineChartGradientColors = ref([
  "rgba(66,134,121,0.15)",
  "rgba(66,134,121,0.0)",
  "rgba(66,134,121,0)",
]);
const greenLineChartGradientStops = ref([1, 0.4, 0]);

const blueBarChartData = ref({
  labels: ["USA", "GER", "AUS", "UK", "RO", "BR"],
  datasets: [
    {
      label: "Countries",
      fill: true,
      borderColor: config.colors.info,
      borderWidth: 2,
      borderDash: [],
      borderDashOffset: 0.0,
      data: [53, 20, 10, 80, 100, 45],
    },
  ],
});
const blueBarChartExtraOptions = ref(chartConfigs.barChartOptions);
const blueBarChartGradientStops = ref([1, 0.4, 0]);

const bigLineChartCategories = ref([
  { name: "Accounts", icon: "tim-icons icon-single-02" },
  { name: "Purchases", icon: "tim-icons icon-gift-2" },
  { name: "Sessions", icon: "tim-icons icon-tap-02" },
]);

const initBigChart = (index) => {
  const chartData = {
    datasets: [
      {
        ...bigChartDatasetOptions,
        data: bigChartDataValues[index],
      },
    ],
    labels: bigChartLabels,
  };
  if (bigChartRef.value) {
    bigChartRef.value.updateGradients(chartData);
  }
  bigLineChartData.value = chartData;
  bigLineChartActiveIndex.value = index;
};

// Time period selectors for bot profit chart
const timePeriods = [
  { label: 'Day', value: 'day', icon: 'tim-icons icon-calendar-60' },
  { label: 'Week', value: 'week', icon: 'tim-icons icon-chart-pie-36' },
  { label: 'Month', value: 'month', icon: 'tim-icons icon-chart-bar-32' },
  { label: 'Year', value: 'year', icon: 'tim-icons icon-money-coins' },
];
const selectedPeriodIndex = ref(2); // Month selected by default

// Change time period for profit chart
const changePeriod = (index) => {
  selectedPeriodIndex.value = index;
  fetchBotProfitData(timePeriods[index].value);
};

// Fetch bot profit data based on time period
const fetchBotProfitData = async (period) => {
  try {
    const response = await botService.getBotPerformance(period);

    if (response && response.data) {
      // Use the data directly from the backend
      performanceChartData.value = {
        labels: response.data.labels,
        datasets: [
          {
            label: "Bot Profit",
            data: response.data.data,
            borderColor: "#41B883",
            backgroundColor: "rgba(65, 184, 131, 0.1)",
            borderWidth: 3,
            pointRadius: 4,
            pointBackgroundColor: "#41B883",
            tension: 0.4,
            fill: false,
          },
        ],
      };
    }
  } catch (err) {
    console.error('Error fetching bot profit data:', err);
  }
};

onMounted(() => {
  // Initialize bot profit data
  fetchBotProfitData(timePeriods[selectedPeriodIndex.value].value);
});
</script>
<style lang="scss" scoped>
.content {
  padding: 20px;
}

.header {
  margin-bottom: 20px;
  position: relative;

  .title {
    color: rgba(255, 255, 255, 0.7);
    margin-bottom: 0;
  }

  h2 {
    margin-top: 5px;
    margin-bottom: 20px;
  }

  .tabs {
    display: flex;
    position: absolute;
    right: 60px;
    top: 20px;

    .tab {
      padding: 8px 15px;
      margin-left: 5px;
      border-radius: 5px;
      background-color: rgba(255, 255, 255, 0.1);
      color: rgba(255, 255, 255, 0.7);
      cursor: pointer;
      transition: all 0.3s;

      &.active {
        background-color: #a03bff;
        color: white;
      }

      &:hover:not(.active) {
        background-color: rgba(255, 255, 255, 0.15);
      }
    }
  }
}

.chart-container {
  height: 300px;
  margin-bottom: 30px;
}

.stats-cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
  margin-bottom: 30px;

  @media (max-width: 1200px) {
    grid-template-columns: repeat(2, 1fr);
  }

  @media (max-width: 768px) {
    grid-template-columns: 1fr;
  }
}

.charts-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;

  @media (max-width: 1200px) {
    grid-template-columns: repeat(2, 1fr);
  }

  @media (max-width: 768px) {
    grid-template-columns: 1fr;
  }

  .chart-card {
    background-color: #27293d;
    border-radius: 10px;
    padding: 20px;
    box-shadow: 0 1px 20px 0 rgba(0, 0, 0, 0.1);

    .title {
      color: rgba(255, 255, 255, 0.7);
      margin-top: 0;
      margin-bottom: 5px;
    }

    h3 {
      margin-top: 0;
      margin-bottom: 15px;
    }

    .chart-wrapper {
      height: 100%;
    }
  }
}
</style>
