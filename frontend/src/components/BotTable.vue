<template>
  <div class="card-body">
    <PagedTable
      :columns="tableColumns"
      :fetch-data="fetchBotData"
      thead-classes="text-primary"
    >
      <template #row="{ row }">
        <td>{{ row.id }}</td>
        <td>{{ row.name }}</td>
        <td>{{ row.symbol }}</td>
        <td>{{ formatNumber(row.quantity) }}</td>
        <td>
          <el-tag :type="row.enabled ? 'success' : 'info'">
            {{ row.enabled ? 'Active' : 'Inactive' }}
          </el-tag>
        </td>
        <td>{{ formatCurrency(row.minPrice) }} - {{ formatCurrency(row.maxPrice) }}</td>
        <td>
          <el-tag :type="row.isLong ? 'success' : 'danger'">
            {{ row.isLong ? 'Long' : 'Short' }}
          </el-tag>
        </td>
        <td class="text-right">
          <BaseButton
            v-if="showViewButton"
            type="info"
            icon
            size="sm"
            class="btn-link"
            @click="$emit('view', row)"
          >
            <i class="tim-icons icon-zoom-split"></i>
          </BaseButton>
          <BaseButton
            v-if="showToggleStatusButton"
            class="btn-link"
            :type="row.enabled ? 'warning' : 'success'"
            size="sm"
            icon
            @click="$emit('toggle-status', row)"
          >
            <i :class="row.enabled ? 'tim-icons icon-button-pause' : 'tim-icons icon-button-power'"></i>
          </BaseButton>
        </td>
      </template>
    </PagedTable>
  </div>
</template>

<script setup>
import { ref, onMounted, defineProps, defineEmits } from "vue";
import { ElTag } from "element-plus";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import botService from "@/services/botService";
import apiClient from "@/services/api";
import { formatCurrency, formatNumber, getProfitClass } from "@/util/formatters";

const props = defineProps({
  showViewButton: { type: Boolean, default: true },
  showRefreshButton: { type: Boolean, default: true },
  showToggleStatusButton: { type: Boolean, default: false },
  showTradesButton: { type: Boolean, default: false },
  showEditButton: { type: Boolean, default: false },
  showDeleteButton: { type: Boolean, default: false }
});

const emit = defineEmits(['toggle-status', 'edit', 'delete']);

const error = ref(null);

const tableColumns = [
  { key: 'id', label: 'ID' },
  { key: 'name', label: 'NAME' },
  { key: 'symbol', label: 'SYMBOL' },
  { key: 'quantity', label: 'QUANTITY' },
  { key: 'status', label: 'STATUS' },
  { key: 'minMaxPrice', label: 'MIN/MAX PRICE' },
  { key: 'direction', label: 'DIRECTION' },
  { key: 'actions', label: 'ACTIONS', align: 'right' }
];

const fetchBotData = async (params) => {
  try {
    const response = await apiClient.get('/bots', {
      params: {
        page: params?.page || 1,
        pageSize: params?.pageSize || 10,
        searchQuery: params?.searchQuery || undefined,
        sortKey: params?.sortKey,
        sortDirection: params?.sortDirection
      }
    });
    return {
      items: response.data.items,
      totalCount: response.data.totalCount
    };
  } catch (e) {
    error.value = e;
    console.error('Error fetching bots data:', e);
    return { items: [], totalCount: 0 };
  }
};
</script>

<style>
.text-success {
  color: #00f2c3 !important;
}

.text-danger {
  color: #fd5d93 !important;
}

.opacity-5 {
  opacity: 0.5;
}

.pagination-select {
  width: 80px !important;
}

/* Direct style override for the select background */
.el-select .el-select__wrapper {
  background-color: #27293d !important;
}

.white-content .el-select .el-select__wrapper {
  background-color: #ffffff !important;
}

/* Force override for the fill-color-blank variable */
:deep(.el-select) {
  --el-fill-color-blank: #27293d !important;
}

.white-content :deep(.el-select) {
  --el-fill-color-blank: #ffffff !important;
}
</style>