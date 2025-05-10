<template>
  <el-dialog
    title="Trade Details"
    :modelValue="modelValue"
    @update:modelValue="$emit('update:modelValue', $event)"
    width="600px"
  >
    <div v-if="trade" class="trade-detail-container">
      <div class="row mb-3">
        <div class="col-md-6">
          <strong>Trade ID:</strong> {{ trade.id }}
        </div>
        <div class="col-md-6">
          <strong>Symbol:</strong> {{ trade.symbol }}
        </div>
      </div>

      <div class="row mb-3">
        <div class="col-md-6">
          <strong>Direction:</strong>
          <span :class="trade.isLong ? 'text-success' : 'text-danger'">
            {{ trade.isLong ? 'Buy' : 'Sell' }}
          </span>
        </div>
        <div class="col-md-6">
          <strong>Profit:</strong>
          <span :class="trade.profit > 0 ? 'text-success' : 'text-danger'">
            {{ formatCurrency(trade.profit) }}
          </span>
        </div>
      </div>

      <h5 class="mt-4">Entry Details</h5>
      <div class="row mb-3">
        <div class="col-md-6">
          <strong>Price:</strong> {{ formatCurrency(trade.entryPrice) }}
        </div>
        <div class="col-md-6">
          <strong>Average Fill:</strong> {{ formatCurrency(trade.entryAvgFill || trade.entryPrice) }}
        </div>
      </div>

      <div class="row mb-3">
        <div class="col-md-6">
          <strong>Quantity:</strong> {{ formatNumber(trade.quantity) }}
        </div>
        <div class="col-md-6">
          <strong>Fee:</strong> {{ formatNumber(trade.entryFee) }}
        </div>
      </div>

      <div class="row mb-3">
        <div class="col-md-12">
          <strong>Entry Time:</strong> {{ formatDate(trade.entryTime) }}
        </div>
      </div>

      <h5 class="mt-4">Exit Details</h5>
      <div class="row mb-3">
        <div class="col-md-6">
          <strong>Price:</strong> {{ formatCurrency(trade.exitPrice) }}
        </div>
        <div class="col-md-6">
          <strong>Average Fill:</strong> {{ formatCurrency(trade.exitAvgFill || trade.exitPrice) }}
        </div>
      </div>

      <div class="row mb-3">
        <div class="col-md-6">
          <strong>Fee:</strong> {{ formatNumber(trade.exitFee) }}
        </div>
        <div class="col-md-6">
          <strong>Status:</strong> {{ trade.isCompleted ? 'Completed' : 'Open' }}
        </div>
      </div>

      <div class="row mb-3">
        <div class="col-md-12">
          <strong>Exit Time:</strong> {{ formatDate(trade.exitTime) }}
        </div>
      </div>
    </div>
  </el-dialog>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";
import { ElDialog } from "element-plus";
import { formatCurrency, formatDate, formatNumber } from "@/util/formatters";

const props = defineProps({
  modelValue: {
    type: Boolean,
    required: true
  },
  trade: {
    type: Object,
    default: null
  }
});

const emit = defineEmits(['update:modelValue']);
</script>

<style scoped>
.text-success {
  color: #00f2c3 !important;
}

.text-danger {
  color: #fd5d93 !important;
}

.trade-detail-container {
  padding: 16px;
}
</style> 