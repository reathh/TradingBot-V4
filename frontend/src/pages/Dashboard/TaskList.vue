<template>
  <BaseTable :data="tableData" thead-classes="text-primary">
    <template #default="{ row }">
      <td v-if="row"><BaseCheckbox v-model="row.done"></BaseCheckbox></td>
      <td v-if="row">
        <p class="title">{{ row.title }}</p>
        <p class="text-muted">{{ row.description }}</p>
      </td>
      <td class="td-actions text-right" v-if="row">
        <el-tooltip
          content="Edit task"
          effect="light"
          :open-delay="300"
          placement="top"
        >
          <BaseButton type="link">
            <i class="tim-icons icon-pencil"></i>
          </BaseButton>
        </el-tooltip>
      </td>
    </template>
  </BaseTable>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { ElTooltip } from "element-plus";
import BaseTable from "@/components/BaseTable.vue";
import BaseCheckbox from "@/components/Inputs/BaseCheckbox.vue";
import BaseButton from "@/components/BaseButton.vue";
import dashboardService from "@/services/dashboard";

const tableData = ref([
  {
    title: "Loading tasks...",
    description: "Please wait while tasks are being loaded",
    done: false,
  }
]);

// Fetch tasks from API
const fetchTasks = async () => {
  try {
    const response = await dashboardService.getTasks();
    tableData.value = response.data;
  } catch (error) {
    console.error("Error fetching tasks:", error);
    tableData.value = [
      {
        title: "Error loading tasks",
        description: "Could not load tasks from the server",
        done: false,
      }
    ];
  }
};

onMounted(() => {
  fetchTasks();
});
</script>

<style></style>
