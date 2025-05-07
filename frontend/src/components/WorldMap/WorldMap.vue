<template>
  <div id="worldMap" style="height: 300px"></div>
</template>

<script setup>
import * as d3 from "d3";
import "topojson";
import { onMounted } from "vue";
import { throttle } from "@/util/throttle";

const color1 = "#AAAAAA";
const color2 = "#444444";
const highlightFillColor = "#66615B";
const borderColor = "#3c3c3c";
const highlightBorderColor = "#3c3c3c";

const mapData = {
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

function generateColors(length) {
  return d3.scaleLinear().domain([0, length]).range([color1, color2]);
}

function generateMapColors() {
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
  const { default: DataMap } = await import("datamaps"); // 👈 Dynamic import inside browser-only context

  const { fills, mapData } = generateMapColors();

  const worldMap = new DataMap({
    scope: "world",
    element: document.getElementById("worldMap"),
    fills,
    data: mapData,
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

  const resizeFunc = worldMap.resize.bind(worldMap);
  window.addEventListener("resize", () => throttle(resizeFunc, 40), false);
});
</script>
