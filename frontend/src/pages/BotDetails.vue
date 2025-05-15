<template>
  <div class="content">
    <div class="md-layout">
      <div class="md-layout-item md-size-100">
        <card>
          <div slot="header" class="d-flex justify-content-between align-items-center">
            <div class="d-flex align-items-center mb-1">
              <el-tooltip content="Back to Bots" effect="dark" placement="top">
                <router-link to="/bots" class="icon-action">
                  <i class="tim-icons icon-minimal-left"></i>
                </router-link>
              </el-tooltip>
              <span class="ml-2 card-title bot-title-align">{{ bot.name || 'Loading...' }}</span>
            </div>
            <div class="action-btn-group">
              <el-tooltip v-if="bot.id" :content="bot.enabled ? 'Deactivate Bot' : 'Activate Bot'" effect="dark" placement="top">
                <button @click="toggleBotStatus" class="icon-action" :aria-label="bot.enabled ? 'Deactivate Bot' : 'Activate Bot'">
                  <i :class="bot.enabled ? 'tim-icons icon-button-pause' : 'tim-icons icon-button-power'"></i>
                </button>
              </el-tooltip>
              <el-tooltip v-if="bot.id" content="Delete Bot" effect="dark" placement="top">
                <button @click="deleteBot" class="icon-action" aria-label="Delete Bot">
                  <i class="tim-icons icon-trash-simple"></i>
                </button>
              </el-tooltip>
            </div>
          </div>

          <div v-if="loading" class="text-center py-5">
            <i class="tim-icons icon-refresh-01 fa-spin fa-3x"></i>
            <p class="mt-3">Loading bot details...</p>
          </div>

          <div v-else-if="!bot.id" class="text-center py-5">
            <p>Bot not found</p>
            <router-link to="/bots">
              <base-button type="primary">Back to Bots List</base-button>
            </router-link>
          </div>

          <div v-else>
            <el-form ref="botForm" :model="bot" label-position="top">
              <div class="row">
                <div class="col-md-6">
                  <el-form-item label="Bot Name" required>
                    <el-input v-model="bot.name" placeholder="Enter bot name"></el-input>
                  </el-form-item>
                </div>
                <div class="col-md-6">
                  <el-form-item label="Trading Symbol" required>
                    <el-input v-model="bot.symbol" placeholder="e.g. BTCUSDT"></el-input>
                  </el-form-item>
                </div>
              </div>

              <div class="row">
                <div class="col-md-6">
                  <el-form-item label="API Public Key" required>
                    <el-input v-model="bot.publicKey" placeholder="Enter API public key"></el-input>
                  </el-form-item>
                </div>
                <div class="col-md-6">
                  <el-form-item label="API Private Key" required>
                    <el-input v-model="bot.privateKey" type="password" placeholder="Enter API private key"></el-input>
                  </el-form-item>
                </div>
              </div>

              <div class="row">
                <div class="col-md-6">
                  <el-form-item label="Trade Direction">
                    <el-select v-model="bot.isLong" placeholder="Select direction">
                      <el-option :value="true" label="Long"></el-option>
                      <el-option :value="false" label="Short"></el-option>
                    </el-select>
                  </el-form-item>
                </div>
                <div class="col-md-6">
                  <el-form-item label="Trading Mode" required>
                    <el-select v-model="bot.tradingMode" placeholder="Select trading mode">
                      <el-option value="Spot" label="Spot"></el-option>
                      <el-option value="Margin" label="Margin"></el-option>
                    </el-select>
                  </el-form-item>
                </div>
              </div>

              <div class="row">
                <div class="col-md-6">
                  <el-form-item label="Entry Order Type" required>
                    <el-select v-model="bot.entryOrderType" placeholder="Select entry order type">
                      <el-option value="LimitMaker" label="Limit Maker"></el-option>
                      <el-option value="Limit" label="Limit"></el-option>
                      <el-option value="Market" label="Market"></el-option>
                    </el-select>
                  </el-form-item>
                </div>
                <div class="col-md-6">
                  <el-form-item label="Exit Order Type" required>
                    <el-select v-model="bot.exitOrderType" placeholder="Select exit order type">
                      <el-option value="LimitMaker" label="Limit Maker"></el-option>
                      <el-option value="Limit" label="Limit"></el-option>
                      <el-option value="Market" label="Market"></el-option>
                    </el-select>
                  </el-form-item>
                </div>
              </div>

              <div class="row">
                <div class="col-md-6">
                  <el-form-item label="Quantity Per Level" required>
                    <el-input
                      v-model.number="bot.entryQuantity"
                      type="number"
                      step="0.0001"
                      placeholder="Enter quantity"
                      @input="formatNumberInput($event, 'entryQuantity')"
                    ></el-input>
                  </el-form-item>
                </div>
                <div class="col-md-6">
                  <el-form-item label="Min Price">
                    <el-input
                      v-model.number="bot.minPrice"
                      type="number"
                      step="0.01"
                      placeholder="Min price"
                      @input="formatNumberInput($event, 'minPrice')"
                    ></el-input>
                  </el-form-item>
                </div>
              </div>

              <div class="row">
                <div class="col-md-6">
                  <el-form-item label="Max Price">
                    <el-input
                      v-model.number="bot.maxPrice"
                      type="number"
                      step="0.01"
                      placeholder="Max price"
                      @input="formatNumberInput($event, 'maxPrice')"
                    ></el-input>
                  </el-form-item>
                </div>
                <div class="col-md-6">
                  <el-form-item label="Entry Step" required>
                    <el-input
                      v-model.number="bot.entryStep"
                      type="number"
                      step="0.01"
                      placeholder="Entry step"
                      @input="formatNumberInput($event, 'entryStep')"
                    ></el-input>
                  </el-form-item>
                </div>
              </div>

              <div class="row">
                <div class="col-md-6">
                  <el-form-item label="Exit Step" required>
                    <el-input
                      v-model.number="bot.exitStep"
                      type="number"
                      step="0.01"
                      placeholder="Exit step"
                      @input="formatNumberInput($event, 'exitStep')"
                    ></el-input>
                  </el-form-item>
                </div>
                <div class="col-md-6">
                  <el-form-item label="Starting Base Amount" required>
                    <el-input
                      v-model.number="bot.startingBaseAmount"
                      type="number"
                      step="0.0001"
                      placeholder="Starting base amount"
                      @input="formatNumberInput($event, 'startingBaseAmount')"
                    ></el-input>
                  </el-form-item>
                </div>
              </div>

              <el-form-item>
                <el-checkbox v-model="bot.placeOrdersInAdvance">Place orders in advance</el-checkbox>
              </el-form-item>

              <el-form-item>
                <el-checkbox v-model="bot.stopLossEnabled">Enable stop-loss (liquidate positions on loss)</el-checkbox>
              </el-form-item>

              <div v-if="bot.stopLossEnabled" class="row ml-1 mb-3">
                <div class="col-md-4">
                  <el-form-item label="Stop Loss Percentage" required>
                    <el-input
                      v-model.number="bot.stopLossPercent"
                      type="number"
                      step="0.1"
                      min="0.1"
                      max="50"
                      placeholder="Stop loss (%)"
                      @input="formatNumberInput($event, 'stopLossPercent')"
                    >
                      <template #suffix>%</template>
                    </el-input>
                  </el-form-item>
                </div>
                <div class="col-md-8 d-flex align-items-center">
                  <div class="text-muted">
                    Exit trades when price moves this percentage against the position.
                  </div>
                </div>
              </div>

              <template v-if="bot.placeOrdersInAdvance">
                <div class="row">
                  <div class="col-md-6">
                    <el-form-item label="Entry Orders In Advance">
                      <el-input
                        v-model.number="bot.entryOrdersInAdvance"
                        type="number"
                        min="1"
                        max="1000"
                        placeholder="Entry orders in advance"
                        @input="formatNumberInput($event, 'entryOrdersInAdvance')"
                      ></el-input>
                    </el-form-item>
                  </div>
                  <div class="col-md-6">
                    <el-form-item label="Exit Orders In Advance">
                      <el-input
                        v-model.number="bot.exitOrdersInAdvance"
                        type="number"
                        min="1"
                        max="1000"
                        placeholder="Exit orders in advance"
                        @input="formatNumberInput($event, 'exitOrdersInAdvance')"
                      ></el-input>
                    </el-form-item>
                  </div>
                </div>
              </template>

              <div class="d-flex justify-content-between mt-4">
                <base-button @click="saveBot" type="primary">Save Changes</base-button>
              </div>
            </el-form>
          </div>
        </card>
        <card class="mt-4">
          <div slot="header">
            <span class="card-title">Trades</span>
          </div>
          <TradesTable :botId="bot.id" />
        </card>
        <card class="mt-4">
          <div slot="header">
            <span class="card-title">Orders</span>
          </div>
          <OrdersTable :botId="bot.id" />
        </card>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  ElForm,
  ElFormItem,
  ElInput,
  ElSelect,
  ElOption,
  ElCheckbox
} from "element-plus";
import Card from "@/components/Cards/Card.vue";
import BaseButton from "@/components/BaseButton.vue";
import { useNotifications } from "@/components/Notifications/NotificationPlugin";
import apiClient from "@/services/api";
import TradesTable from '@/components/TradesTable.vue';
import OrdersTable from '@/components/OrdersTable.vue';

const route = useRoute();
const router = useRouter();
const loading = ref(true);
const bot = ref({
  id: 0,
  name: '',
  publicKey: '',
  privateKey: '',
  symbol: '',
  enabled: false,
  maxPrice: null,
  minPrice: null,
  quantity: 0,
  placeOrdersInAdvance: false,
  isLong: true,
  exitStep: 0.01,
  entryStep: 0.01,
  entryQuantity: 0,
  startingBaseAmount: 0,
  startFromMaxPrice: false,
  stopLossEnabled: false,
  stopLossPercent: 1.0,
  entryOrdersInAdvance: 100,
  exitOrdersInAdvance: 100,
  tradingMode: 'Spot',
  entryOrderType: 'LimitMaker',
  exitOrderType: 'LimitMaker'
});

const { notify } = useNotifications();

// Fetch the bot data
async function fetchBot() {
  loading.value = true;
  try {
    const response = await apiClient.get(`bots/${route.params.id}`);
    bot.value = response.data;
  } catch (error) {
    console.error('Error fetching bot:', error);
    notify({
      type: 'danger',
      title: 'Error',
      message: 'Failed to load bot details',
      icon: 'fas fa-times',
    });
  } finally {
    loading.value = false;
  }
}

// Save the bot data
async function saveBot() {
  try {
    await apiClient.put(`bots/${bot.value.id}`, bot.value);
    notify({
      type: 'success',
      title: 'Success',
      message: 'Bot updated successfully',
      icon: 'fas fa-check',
    });
  } catch (error) {
    console.error('Error saving bot:', error);
    notify({
      type: 'danger',
      title: 'Error',
      message: error.response?.data?.message || 'Failed to save bot',
      icon: 'fas fa-times',
    });
  }
}

// Toggle bot enabled status
async function toggleBotStatus() {
  try {
    await apiClient.post(`bots/${bot.value.id}/toggle`);
    notify({
      type: 'success',
      title: 'Success',
      message: `Bot ${!bot.value.enabled ? 'activated' : 'deactivated'} successfully`,
      icon: 'fas fa-check',
    });
    // Refresh bot data to get updated status
    fetchBot();
  } catch (error) {
    console.error('Error toggling bot status:', error);
    notify({
      type: 'danger',
      title: 'Error',
      message: 'Failed to update bot status',
      icon: 'fas fa-times',
    });
  }
}

// Helper function to ensure decimal separator is a dot
function formatNumberInput(event, field) {
  // Get the input value
  const value = event.target.value;

  // Replace comma with dot if present
  if (value && value.includes(',')) {
    const correctedValue = value.replace(',', '.');
    // Update the input field directly
    event.target.value = correctedValue;
    // Update the model
    bot.value[field] = parseFloat(correctedValue);
  }
}

// Add deleteBot function
async function deleteBot() {
  if (!bot.value.id) return;
  if (!confirm(`Are you sure you want to delete bot "${bot.value.name}"? This cannot be undone!`)) return;
  try {
    await apiClient.delete(`bots/${bot.value.id}`);
    notify({
      type: 'success',
      title: 'Deleted',
      message: 'The bot has been deleted.',
      icon: 'fas fa-check',
    });
    router.push({ name: 'Bots' });
  } catch (error) {
    console.error('Error deleting bot:', error);
    notify({
      type: 'danger',
      title: 'Error',
      message: 'Failed to delete bot',
      icon: 'fas fa-times',
    });
  }
}

// Fetch bot on mount
onMounted(() => {
  fetchBot();
});
</script>

<style>
.action-btn-group {
  display: flex;
  gap: 0.75rem;
}

.fa-spin {
  animation: fa-spin 2s infinite linear;
}

@keyframes fa-spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(359deg);
  }
}

.icon-action {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  color: #bfc9da;
  background: none;
  border: none;
  outline: none;
  border-radius: 50%;
  transition: background 0.2s, color 0.2s;
  cursor: pointer;
  padding: 0;
}
.icon-action:hover, .icon-action:focus {
  background: rgba(191, 201, 218, 0.1);
  color: #fff;
  text-decoration: none;
}
.icon-action .tim-icons {
  font-size: 1.35rem;
}

.bot-title-align {
  display: inline-block;
  vertical-align: middle;
  line-height: 1;
  position: relative;
  top: 4px;
}
</style> 