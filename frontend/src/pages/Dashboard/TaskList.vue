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
import { ref } from "vue";
import { ElTooltip } from "element-plus";
import BaseTable from "@/components/BaseTable.vue";
import BaseCheckbox from "@/components/Inputs/BaseCheckbox.vue";
import BaseButton from "@/components/BaseButton.vue";

const tableData = ref([
  {
    title: "Update the Documentation",
    description: "Dwuamish Head, Seattle, WA 8:47 AM",
    done: false,
  },
  {
    title: "GDPR Compliance",
    description:
      "The GDPR is a regulation that requires businesses to protect the personal data and privacy of Europe citizens for transactions that occur within EU member states.",
    done: true,
  },
  {
    title: "Solve the issues",
    description:
      "Fifty percent of all respondents said they would be more likely to shop at a company",
    done: false,
  },
  {
    title: "Release v2.0.0",
    description: "Ra Ave SW, Seattle, WA 98116, SUA 11:19 AM",
    done: false,
  },
  {
    title: "Export the processed files",
    description:
      "The report also shows that consumers will not easily forgive a company once a breach exposing their personal data occurs.",
    done: false,
  },
  {
    title: "Arival at export process",
    description: "Capitol Hill, Seattle, WA 12:34 AM",
    done: false,
  },
]);
</script>

<style></style>
