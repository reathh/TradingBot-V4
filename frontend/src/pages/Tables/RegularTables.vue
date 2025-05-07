<template>
  <div class="row">
    <!-- Striped Table -->
    <div class="col-md-12">
      <Card body-classes="table-full-width">
        <template #header>
          <h4 class="card-title">Striped table</h4>
        </template>
        <ElTable :data="tableData">
          <ElTableColumn prop="name" min-width="150" sortable label="Name" />
          <ElTableColumn
            prop="country"
            min-width="150"
            sortable
            label="Country"
          />
          <ElTableColumn prop="city" min-width="150" sortable label="City" />
          <ElTableColumn
            prop="salary"
            min-width="150"
            sortable
            align="right"
            header-align="right"
            label="Salary"
          />
        </ElTable>
      </Card>
    </div>

    <!-- Plain Background Table -->
    <div class="col-md-12">
      <Card class="card-plain" body-classes="table-full-width">
        <template #header>
          <h4 class="card-title">Table on Plain Background</h4>
          <p class="category">Here is a subtitle for this table</p>
        </template>
        <ElTable
          header-cell-class-name="table-transparent"
          header-row-class-name="table-transparent"
          row-class-name="table-transparent"
          :data="tableData"
        >
          <ElTableColumn prop="name" min-width="150" sortable label="Name" />
          <ElTableColumn
            prop="country"
            min-width="150"
            sortable
            label="Country"
          />
          <ElTableColumn prop="city" min-width="150" sortable label="City" />
          <ElTableColumn
            prop="salary"
            min-width="150"
            sortable
            align="right"
            header-align="right"
            label="Salary"
          />
        </ElTable>
      </Card>
    </div>

    <!-- Colored Rows Table -->
    <div class="col-md-12">
      <Card body-classes="table-full-width">
        <template #header>
          <h4 class="card-title">Regular Table with Colors</h4>
        </template>
        <ElTable :row-class-name="tableRowClassName" :data="tableData">
          <ElTableColumn prop="name" min-width="150" sortable label="Name" />
          <ElTableColumn
            prop="country"
            min-width="150"
            sortable
            label="Country"
          />
          <ElTableColumn prop="city" min-width="150" sortable label="City" />
          <ElTableColumn
            prop="salary"
            min-width="150"
            sortable
            align="right"
            header-align="right"
            label="Salary"
          />
        </ElTable>
      </Card>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { ElTable, ElTableColumn } from "element-plus";
import "element-plus/es/components/table/style/css";
import "element-plus/es/components/table-column/style/css";

import Card from "@/components/Cards/Card.vue";

const tableData = ref([
  {
    id: 1,
    name: "Dakota Rice",
    salary: "$36.738",
    country: "Niger",
    city: "Oud-Turnhout",
  },
  {
    id: 2,
    name: "Minerva Hooper",
    salary: "$23,789",
    country: "Curaçao",
    city: "Sinaai-Waas",
  },
  {
    id: 3,
    name: "Sage Rodriguez",
    salary: "$56,142",
    country: "Netherlands",
    city: "Baileux",
  },
  {
    id: 4,
    name: "Philip Chaney",
    salary: "$38,735",
    country: "Korea, South",
    city: "Overland Park",
  },
  {
    id: 5,
    name: "Doris Greene",
    salary: "$63,542",
    country: "Malawi",
    city: "Feldkirchen in Kärnten",
  },
]);

const tableRowClassName = ({ rowIndex }) => {
  switch (rowIndex) {
    case 0:
      return "table-success";
    case 2:
      return "table-info";
    case 4:
      return "table-danger";
    case 6:
      return "table-warning";
    default:
      return "";
  }
};
</script>

<style scoped>
.table-transparent {
  background-color: transparent !important;
}
</style>
