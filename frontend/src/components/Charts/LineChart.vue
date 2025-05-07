<template>
  <div class="chart-container">
    <Line :data="chartData" :options="options" :height="height" />
  </div>
</template>

<script setup>
import { computed, ref, onMounted, watch } from "vue";
import { Line } from "vue-chartjs";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

const props = defineProps({
  chartData: {
    type: Object,
    required: true,
  },
  options: {
    type: Object,
    default: () => ({}),
  },
  height: {
    type: Number,
    default: null,
  },
});

const defaultOptions = {
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    y: {
      grid: {
        color: "rgba(255, 255, 255, 0.1)",
      },
      ticks: {
        color: "rgba(255, 255, 255, 0.7)",
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
  },
};

const options = computed(() => {
  return {
    ...defaultOptions,
    ...props.options,
  };
});
</script>

<style lang="scss" scoped>
.chart-container {
  position: relative;
  width: 100%;
  height: 100%;
}
</style>
