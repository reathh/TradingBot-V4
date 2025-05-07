<template>
  <div>
    <h2 class="text-center">Charts.js</h2>
    <p class="category text-center">
      Simple yet flexible JavaScript charting for designers &amp; developers.
      Made by our friends from
      <a target="_blank" rel="noopener" href="https://vue-chartjs.org/"
        >Vue Charts.js</a
      >. Please check
      <a
        target="_blank"
        rel="noopener"
        href="https://vue-chartjs.org/guide/#vue-single-file-components"
      >
        the full documentation </a
      >.
    </p>

    <div class="row mt-5">
      <div class="col-md-5 ml-auto">
        <Card class="card-chart" no-footer-line>
          <template #header>
            <h5 class="card-category">Simple With Gradient</h5>
            <h3 class="card-title">
              <i class="tim-icons icon-chart-bar-32 text-primary"></i> 10,000
            </h3>
          </template>
          <div class="chart-area">
            <LineChart
              :chart-data="lineChart1.chartData"
              :gradient-colors="lineChart1.gradientColors"
              :gradient-stops="lineChart1.gradientStops"
              :extra-options="lineChart1.extraOptions"
              :height="200"
            />
          </div>
        </Card>
      </div>

      <div class="col-md-5 mr-auto">
        <Card class="card-chart">
          <template #header>
            <h5 class="card-category">With Numbers and Grid</h5>
            <h3 class="card-title">
              <i class="tim-icons icon-send text-info"></i> 750,000€
            </h3>
          </template>
          <div class="chart-area">
            <LineChart
              :labels="lineChart2.labels"
              :chart-data="lineChart2.chartData"
              :extra-options="lineChart2.extraOptions"
              :height="200"
            />
          </div>
        </Card>
      </div>
    </div>

    <div class="row">
      <div class="col-md-5 ml-auto">
        <Card class="card-chart">
          <template #header>
            <h5 class="card-category">Gradient Bar Chart</h5>
            <h3 class="card-title">
              <i class="tim-icons icon-shape-star text-danger"></i> 1,000,000£
            </h3>
          </template>
          <div class="chart-area">
            <BarChart
              :chart-data="barChart1.chartData"
              :extra-options="barChart1.extraOptions"
              :gradient-colors="barChart1.gradientColors"
              :gradient-stops="barChart1.gradientStops"
              :height="200"
            />
          </div>
        </Card>
      </div>

      <div class="col-md-5 mr-auto">
        <Card class="card-chart" no-footer-line>
          <template #header>
            <h5 class="card-category">Multiple Bars Chart With Grid</h5>
            <h3 class="card-title">
              <i class="tim-icons icon-time-alarm text-warning"></i> 130,000$
            </h3>
          </template>
          <div class="chart-area">
            <BarChart
              :chart-data="barChart2.chartData"
              :extra-options="barChart2.extraOptions"
              :height="200"
            />
          </div>
        </Card>
      </div>
    </div>

    <div class="row">
      <div class="col-md-5 ml-auto">
        <Card class="card-chart card-chart-pie">
          <template #header>
            <h5 class="card-category">Simple Pie Chart</h5>
          </template>
          <div class="row">
            <div class="col-6">
              <div class="chart-area">
                <PieChart
                  :chart-data="pieChart1.chartData"
                  :extra-options="pieChart1.extraOptions"
                  :height="120"
                />
              </div>
            </div>
            <div class="col-6">
              <h4 class="card-title">
                <i class="tim-icons icon-trophy text-success"></i> 10.000$
              </h4>
              <p class="category">A total of $54000</p>
            </div>
          </div>
        </Card>
      </div>

      <div class="col-md-5 mr-auto">
        <Card class="card-chart card-chart-pie">
          <template #header>
            <h5 class="card-category">Multiple Pie Chart</h5>
          </template>
          <div class="row">
            <div class="col-6">
              <div class="chart-area">
                <PieChart
                  :chart-data="pieChart2.chartData"
                  :extra-options="pieChart2.extraOptions"
                  :height="120"
                />
              </div>
            </div>
            <div class="col-6">
              <h4 class="card-title">
                <i class="tim-icons icon-tag text-warning"></i> 130,000
              </h4>
              <p class="category">Feedback from 20.000 users</p>
            </div>
          </div>
        </Card>
      </div>
    </div>
  </div>
</template>

<script setup>
import LineChart from "@/components/Charts/LineChart.vue";
import BarChart from "@/components/Charts/BarChart.vue";
import PieChart from "@/components/Charts/PieChart.vue";
import Card from "@/components/Cards/Card.vue";
import config from "@/config.js";
import * as chartConfigs from "@/components/Charts/config.js";

const lineChart1 = {
  chartData: {
    labels: ["JUL", "AUG", "SEP", "OCT", "NOV", "DEC"],
    datasets: [
      {
        label: "Data",
        fill: true,
        borderColor: config.colors.primary,
        borderWidth: 2,
        pointBackgroundColor: config.colors.primary,
        pointHoverBackgroundColor: "#be55ed",
        pointBorderColor: "rgba(255,255,255,0)",
        pointBorderWidth: 20,
        pointHoverRadius: 4,
        pointHoverBorderWidth: 15,
        pointRadius: 4,
        data: [80, 100, 70, 80, 120, 80],
      },
    ],
  },
  extraOptions: chartConfigs.purpleChartOptions,
  gradientColors: config.colors.primaryGradient,
  gradientStops: [1, 0.4, 0],
};

const lineChart2 = {
  chartData: {
    labels: ["JUL", "AUG", "SEP", "OCT", "NOV", "DEC"],
    datasets: [
      {
        label: "Data",
        fill: true,
        borderColor: config.colors.info,
        borderWidth: 2,
        pointBackgroundColor: config.colors.info,
        pointHoverBackgroundColor: "#2380f7",
        pointBorderColor: "rgba(255,255,255,0)",
        pointBorderWidth: 20,
        pointHoverRadius: 4,
        pointHoverBorderWidth: 15,
        pointRadius: 4,
        data: [80, 100, 70, 80, 120, 80],
      },
    ],
  },
  extraOptions: chartConfigs.lineChartOptionsBlue,
};

const barChart1 = {
  chartData: {
    labels: ["JUL", "AUG", "SEP", "OCT", "NOV", "DEC"],
    datasets: [
      {
        label: "Data",
        fill: true,
        borderColor: config.colors.danger,
        borderWidth: 2,
        data: [80, 100, 70, 80, 120, 80],
      },
    ],
  },
  extraOptions: chartConfigs.barChartOptionsGradient,
  gradientColors: config.colors.purpleGradient,
  gradientStops: [1, 0],
};

const barChart2 = {
  chartData: {
    labels: ["JUL", "AUG", "SEP", "OCT", "NOV", "DEC"],
    datasets: [
      {
        label: "Data",
        fill: true,
        backgroundColor: config.colors.orange,
        hoverBackgroundColor: config.colors.orange,
        borderColor: config.colors.orange,
        borderWidth: 2,
        data: [80, 100, 70, 80, 120, 80],
      },
      {
        label: "Data",
        fill: true,
        backgroundColor: config.colors.info,
        hoverBackgroundColor: config.colors.info,
        borderColor: config.colors.info,
        borderWidth: 2,
        data: [60, 110, 90, 70, 90, 100],
      },
    ],
  },
  extraOptions: chartConfigs.barChartOptionsGradient,
};

const pieChart1 = {
  chartData: {
    labels: [1, 2],
    datasets: [
      {
        label: "Emails",
        pointRadius: 0,
        pointHoverRadius: 0,
        backgroundColor: ["#00c09d", "#e2e2e2"],
        borderWidth: 0,
        data: [60, 40],
      },
    ],
  },
  extraOptions: chartConfigs.pieChartOptions,
};

const pieChart2 = {
  chartData: {
    labels: [1, 2, 3],
    datasets: [
      {
        label: "Emails",
        pointRadius: 0,
        pointHoverRadius: 0,
        backgroundColor: ["#ff8779", "#2a84e9", "#e2e2e2"],
        borderWidth: 0,
        data: [60, 40, 20],
      },
    ],
  },
  extraOptions: chartConfigs.pieChartOptions,
};
</script>
