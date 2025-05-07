<template>
  <base-table :data="tableData" thead-classes="text-primary">
    <template #columns>
      <th>#</th>
      <th>Name</th>
      <th>Job Position</th>
      <th>Salary</th>
      <th class="text-right">Milestone</th>
      <th class="text-right">Actions</th>
    </template>

    <template #default="{ row, index }">
      <td class="text-center" v-if="row">
        <div class="photo"><img :src="row.img" alt="photo" /></div>
      </td>
      <td v-if="row">{{ row.name }}</td>
      <td v-if="row">{{ row.job }}</td>
      <td class="text-center" v-if="row">
        <base-progress :value="row.progress" />
      </td>
      <td class="text-right" v-if="row">€ 99,225</td>
      <td class="text-right" v-if="row">
        <el-tooltip
          content="Refresh"
          effect="light"
          :open-delay="300"
          placement="top"
        >
          <base-button
            :type="index > 2 ? 'success' : 'neutral'"
            icon
            size="sm"
            class="btn-link"
          >
            <i class="tim-icons icon-refresh-01"></i>
          </base-button>
        </el-tooltip>
        <el-tooltip
          content="Delete"
          effect="light"
          :open-delay="300"
          placement="top"
        >
          <base-button
            :type="index > 2 ? 'danger' : 'neutral'"
            icon
            size="sm"
            class="btn-link"
          >
            <i class="tim-icons icon-simple-remove"></i>
          </base-button>
        </el-tooltip>
      </td>
    </template>
  </base-table>
</template>

<script setup>
import { ref } from "vue";
import BaseTable from "@/components/BaseTable.vue";
import BaseProgress from "@/components/BaseProgress.vue";

const tableData = ref([
  {
    id: 1,
    img: "img/tania.jpg",
    name: "Tania Mike",
    job: "Develop",
    progress: 25,
    salary: "€ 99,225",
  },
  {
    id: 2,
    img: "img/robi.jpg",
    name: "John Doe",
    job: "CEO",
    progress: 77,
    salary: "€ 89,241",
  },
  {
    id: 3,
    img: "img/lora.jpg",
    name: "Alexa Mike",
    job: "Design",
    progress: 41,
    salary: "€ 92,144",
  },
  {
    id: 4,
    img: "img/jana.jpg",
    name: "Jana Monday",
    job: "Marketing",
    progress: 50,
    salary: "€ 49,990",
  },
  {
    id: 5,
    img: "img/mike.jpg",
    name: "Paul Dickens",
    job: "Develop",
    progress: 100,
    salary: "€ 69,201",
  },
  {
    id: 6,
    img: "img/emilyz.jpg",
    name: "Manuela Rico",
    job: "Manager",
    progress: 15,
    salary: "€ 99,201",
  },
]);
</script>

<style></style>
