<template>
  <div id="worldMap" style="height: 300px"></div>
</template>

<script setup>
import * as d3 from "d3";
import { onMounted, ref } from "vue";
import { throttle } from "@/util/throttle";

const props = defineProps({
  data: {
    type: Object,
    default: () => ({})
  }
});

const mapContainer = ref(null);

// Default data if none is provided
const defaultMapData = {
  AUS: 760,
  BRA: 550,
  CAN: 120,
  DEU: 1300,
  FRA: 540,
  GBR: 690,
  GEO: 200,
  IND: 200,
  ROU: 600,
  RUS: 300,
  USA: 2920,
};

const color1 = "#AAAAAA";
const color2 = "#444444";
const highlightFillColor = "#66615B";
const borderColor = "#3c3c3c";
const highlightBorderColor = "#3c3c3c";

function generateColors(length) {
  return d3.scaleLinear().domain([0, length]).range([color1, color2]);
}

function generateMapColors(mapData) {
  const values = Object.values(mapData);
  const maxVal = Math.max(...values);
  const colorScale = generateColors(maxVal);

  const fills = { defaultFill: "#e4e4e4" };
  const formattedData = {};

  for (const key in mapData) {
    const val = mapData[key];
    fills[key] = colorScale(val);
    formattedData[key] = {
      fillKey: key,
      value: val,
    };
  }

  return { fills, mapData: formattedData };
}

onMounted(async () => {
  try {
    // Check if we're in a browser environment
    if (typeof window === 'undefined' || !document) {
      console.warn('WorldMap component requires browser environment');
      return;
    }

    // Use dynamic import for Datamaps
    const DatamapsModule = await import("datamaps");
    const DataMap = DatamapsModule.default;

    // Use provided data or default
    const mapData = Object.keys(props.data).length > 0 ? props.data : defaultMapData;
    const { fills, mapData: formattedData } = generateMapColors(mapData);

    const mapElement = document.getElementById("worldMap");
    if (!mapElement) {
      console.error("Could not find worldMap element");
      return;
    }

    const worldMap = new DataMap({
      scope: "world",
      element: mapElement,
      fills,
      data: formattedData,
      responsive: true,
      geographyConfig: {
        borderColor,
        borderWidth: 0.5,
        borderOpacity: 0.8,
        highlightFillColor,
        highlightBorderColor,
        highlightBorderWidth: 0.5,
        highlightBorderOpacity: 0.8,
      },
    });

    // Ensure proper cleanup
    const handleResize = throttle(() => {
      if (worldMap && typeof worldMap.resize === 'function') {
        worldMap.resize();
      }
    }, 100);

    window.addEventListener("resize", handleResize);

    // Return cleanup function
    return () => {
      window.removeEventListener("resize", handleResize);
    };
  } catch (error) {
    console.error("Error initializing world map:", error);
  }
});
</script>
