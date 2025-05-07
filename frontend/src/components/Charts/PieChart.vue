<script setup>
import { Pie } from "vue-chartjs";
import { Chart as ChartJS, Title, Tooltip, Legend, ArcElement } from "chart.js";
import { ref, watch, onMounted, nextTick } from "vue";

ChartJS.register(Title, Tooltip, Legend, ArcElement);

const props = defineProps({
  chartData: {
    type: Object,
    required: true,
  },
  extraOptions: {
    type: Object,
    default: () => ({}),
  },
  gradientColors: {
    type: Array,
    default: () => [
      "rgba(72,72,176,0.2)",
      "rgba(72,72,176,0.0)",
      "rgba(119,52,169,0)",
    ],
  },
  gradientStops: {
    type: Array,
    default: () => [1, 0.4, 0],
  },
});

const chartRef = ref(null);

const updateGradients = () => {
  const chartInstance = chartRef.value?.chart;

  if (!chartInstance) return;

  const ctx = chartInstance.ctx;
  const gradient = ctx.createLinearGradient(0, 230, 0, 50);

  props.gradientStops.forEach((stop, index) => {
    gradient.addColorStop(stop, props.gradientColors[index]);
  });

  props.chartData.datasets.forEach((dataset) => {
    if (!dataset.backgroundColor || Array.isArray(dataset.backgroundColor)) {
      dataset.backgroundColor = gradient;
    }
  });
};

watch(
  () => props.chartData,
  async () => {
    await nextTick();
    updateGradients();
  },
  { immediate: true }
);

onMounted(() => {
  updateGradients();
});
</script>

<template>
  <Pie ref="chartRef" :data="chartData" :options="extraOptions" />
</template>
