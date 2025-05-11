<template>
  <div class="card-body">
    <PagedTable
      :columns="tableColumns"
      :fetch-data="fetchOrderData"
      thead-classes="text-primary"
    >
      <template #row="{ row }">
        <td>{{ row.id.substring(0, 8) }}</td>
        <td>{{ row.symbol }}</td>
        <td>{{ formatCurrency(row.price) }}</td>
        <td>{{ formatCurrency(row.averageFillPrice || 0) }}</td>
        <td>{{ formatNumber(row.quantity) }}</td>
        <td>{{ formatNumber(row.quantityFilled) }}</td>
        <td>
          <el-tag :type="row.isBuy ? 'success' : 'danger'">
            {{ row.isBuy ? 'Buy' : 'Sell' }}
          </el-tag>
        </td>
        <td>
          <el-tag :type="getStatusType(row)">
            {{ getStatusLabel(row) }}
          </el-tag>
        </td>
        <td class="text-center">
          <BaseButton 
            type="info" 
            icon 
            size="sm" 
            class="btn-link"
            @click="viewOrderDetails(row)"
          >
            <i class="tim-icons icon-zoom-split"></i>
          </BaseButton>
        </td>
      </template>
    </PagedTable>

    <!-- Order Details Modal -->
    <el-dialog
      title="Order Details"
      :modelValue="showDetailsModal"
      @update:modelValue="showDetailsModal = $event"
      width="600px"
    >
      <div v-if="selectedOrder" class="trade-detail-container">
        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Order ID:</strong> {{ selectedOrder.id }}
          </div>
          <div class="col-md-6">
            <strong>Symbol:</strong> {{ selectedOrder.symbol }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Direction:</strong>
            <span :class="selectedOrder.isBuy ? 'text-success' : 'text-danger'">
              {{ selectedOrder.isBuy ? 'Buy' : 'Sell' }}
            </span>
          </div>
          <div class="col-md-6">
            <strong>Status:</strong>
            <el-tag :type="getStatusType(selectedOrder)" size="small">
              {{ getStatusLabel(selectedOrder) }}
            </el-tag>
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Price:</strong> {{ formatCurrency(selectedOrder.price) }}
          </div>
          <div class="col-md-6">
            <strong>Average Fill:</strong> {{ formatCurrency(selectedOrder.averageFillPrice || 0) }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Quantity:</strong> {{ formatNumber(selectedOrder.quantity) }}
          </div>
          <div class="col-md-6">
            <strong>Filled:</strong> {{ formatNumber(selectedOrder.quantityFilled) }}
            <span class="text-muted ml-1">
              ({{ Math.round((selectedOrder.quantityFilled / selectedOrder.quantity) * 100) }}%)
            </span>
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-6">
            <strong>Fee:</strong> {{ formatCurrency(selectedOrder.fees) }}
          </div>
          <div class="col-md-6">
            <strong>Created:</strong> {{ formatDate(selectedOrder.createdAt) }}
          </div>
        </div>

        <div class="row mb-3">
          <div class="col-md-12">
            <strong>Last Updated:</strong> {{ formatDate(selectedOrder.lastUpdated) }}
          </div>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted, defineProps, defineEmits, watch } from "vue";
import { ElTag, ElDialog } from "element-plus";
import BaseButton from "@/components/BaseButton.vue";
import PagedTable from "@/components/PagedTable.vue";
import apiClient from "@/services/api";
import { formatCurrency, formatNumber, formatDate } from "@/util/formatters";

const props = defineProps({
  botId: {
    type: Number,
    default: null
  },
  period: {
    type: String,
    default: "month"
  },
  searchQuery: {
    type: String,
    default: ""
  }
});

const emit = defineEmits(['update:loading']);

const isLoading = ref(false);
const error = ref(null);
const selectedOrder = ref(null);
const showDetailsModal = ref(false);

const tableColumns = [
  { key: 'id', label: 'ID' },
  { key: 'symbol', label: 'SYMBOL' },
  { key: 'price', label: 'PRICE' },
  { key: 'averageFillPrice', label: 'AVG FILL' },
  { key: 'quantity', label: 'QUANTITY' },
  { key: 'quantityFilled', label: 'FILLED' },
  { key: 'side', label: 'SIDE' },
  { key: 'status', label: 'STATUS' },
  { key: 'actions', label: 'ACTIONS', align: 'center' }
];

function viewOrderDetails(order) {
  selectedOrder.value = order;
  showDetailsModal.value = true;
}

function getStatusLabel(order) {
  if (order.canceled) return 'Canceled';
  if (order.closed) return 'Closed';
  if (order.quantityFilled > 0 && order.quantityFilled < order.quantity) return 'Partially Filled';
  if (order.quantityFilled === order.quantity) return 'Filled';
  return 'Open';
}

function getStatusType(order) {
  if (order.canceled) return 'danger';
  if (order.closed) return 'info';
  if (order.quantityFilled > 0 && order.quantityFilled < order.quantity) return 'warning';
  if (order.quantityFilled === order.quantity) return 'success';
  return 'primary';
}

const fetchOrderData = async (params) => {
  isLoading.value = true;
  emit('update:loading', true);
  try {
    const response = await apiClient.get('orders', {
      params: {
        page: params?.page || 1,
        pageSize: params?.pageSize || 10,
        botId: props.botId || undefined,
        period: props.period,
        searchQuery: params?.searchQuery || props.searchQuery || undefined,
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
    console.error('Error fetching order data:', e);
    return { items: [], totalCount: 0 };
  } finally {
    isLoading.value = false;
    emit('update:loading', false);
  }
};

watch(() => props.botId, () => {
  // No need to manage currentPage, PagedTable will handle
});

watch(() => props.period, () => {
  // No need to manage currentPage, PagedTable will handle
});

watch(() => props.searchQuery, () => {
  // No need to manage currentPage, PagedTable will handle
});
</script>

<style>
.text-success {
  color: #00f2c3 !important;
}

.text-danger {
  color: #fd5d93 !important;
}

.trade-detail-container {
  padding: 16px;
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