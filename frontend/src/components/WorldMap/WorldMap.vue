<template>
  <div ref="mapContainer" id="worldMap" style="height: 300px"></div>
</template>

<script setup>
import { onMounted, ref, nextTick } from "vue";
import { throttle } from "@/util/throttle";
import * as topojson from "topojson-client";

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

onMounted(async () => {
  try {
    // Wait for the DOM to be ready
    await nextTick();

    // Make sure we're in browser environment
    if (typeof window === 'undefined') {
      console.warn('WorldMap requires browser environment');
      return;
    }

    // Dynamically import d3 modules
    const d3 = await import("d3");
    const d3Selection = await import("d3-selection");

    // Generate color scale
    function generateColors(length) {
      return d3.scaleLinear().domain([0, length]).range(["#AAAAAA", "#444444"]);
    }

    function generateMapColors(mapData) {
      const values = Object.values(mapData);
      const maxVal = values.length ? Math.max(...values) : 1;
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

    // Use provided data or default
    const mapData = Object.keys(props.data).length > 0 ? props.data : defaultMapData;

    // Get datamaps
    const DatamapsModule = await import("datamaps");
    const DataMap = DatamapsModule.default;

    const { fills, mapData: formattedData } = generateMapColors(mapData);

    // Ensure the map container exists
    if (!mapContainer.value) {
      console.error("Map container ref not available");
      return;
    }

    // Create the map
    const worldMap = new DataMap({
      scope: "world",
      element: mapContainer.value,
      fills,
      data: formattedData,
      responsive: true,
      geographyConfig: {
        borderColor: "#3c3c3c",
        borderWidth: 0.5,
        borderOpacity: 0.8,
        highlightFillColor: "#66615B",
        highlightBorderColor: "#3c3c3c",
        highlightBorderWidth: 0.5,
        highlightBorderOpacity: 0.8,
      },
    });

    // Handle resize
    const handleResize = throttle(() => {
      if (worldMap && typeof worldMap.resize === 'function') {
        worldMap.resize();
      }
    }, 100);

    window.addEventListener("resize", handleResize);

    // Clean up function
    return () => {
      window.removeEventListener("resize", handleResize);
    };
  } catch (error) {
    console.error("Error initializing world map:", error);
  }
});
</script>
