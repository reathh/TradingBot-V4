<template>
  <div class="content">
    <div class="md-layout">
      <div class="md-layout-item md-size-100">
        <card card-body-classes="table-full-width">
          <div slot="header" class="d-flex justify-content-between align-items-center">
            <h4 class="card-title">Trading Bots</h4>
            <base-button @click="createNewBot" type="primary" size="sm" icon>
              <i class="tim-icons icon-simple-add"></i>
            </base-button>
          </div>

          <div>
            <BotTable
              ref="botTableRef"
              :showViewButton="true"
              :showRefreshButton="false"
              :showToggleStatusButton="true"
              :showTradesButton="false"
              :showEditButton="false"
              :showDeleteButton="false"
              @toggle-status="toggleBotStatus"
              @view="navigateToBotDetails"
            ></BotTable>
          </div>
        </card>
      </div>
    </div>

    <!-- Create New Bot Modal -->
    <el-dialog
      title="Create New Bot"
      v-model="showCreateModal"
      width="600px"
    >
      <el-form ref="botForm" :model="currentBot" label-position="top">
        <el-form-item label="Bot Name" required>
          <el-input v-model="currentBot.name" placeholder="Enter bot name"></el-input>
        </el-form-item>

        <el-form-item label="Trading Symbol" required>
          <el-input v-model="currentBot.symbol" placeholder="e.g. BTCUSDT"></el-input>
        </el-form-item>

        <div class="row">
          <div class="col-md-6">
            <el-form-item label="API Public Key" required>
              <el-input v-model="currentBot.publicKey" placeholder="Enter API public key"></el-input>
            </el-form-item>
          </div>
          <div class="col-md-6">
            <el-form-item label="API Private Key" required>
              <el-input v-model="currentBot.privateKey" type="password" placeholder="Enter API private key"></el-input>
            </el-form-item>
          </div>
        </div>

        <div class="row">
          <div class="col-md-6">
            <el-form-item label="Trade Direction">
              <el-select v-model="currentBot.isLong" placeholder="Select direction">
                <el-option :value="true" label="Long"></el-option>
                <el-option :value="false" label="Short"></el-option>
              </el-select>
            </el-form-item>
          </div>
          <div class="col-md-6">
            <el-form-item label="Trading Mode" required>
              <el-select v-model="currentBot.tradingMode" placeholder="Select trading mode">
                <el-option value="Spot" label="Spot"></el-option>
                <el-option value="Margin" label="Margin"></el-option>
              </el-select>
            </el-form-item>
          </div>
        </div>

        <div class="row">
          <div class="col-md-6">
            <el-form-item label="Trade Quantity" required>
              <el-input
                v-model.number="currentBot.entryQuantity"
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
                v-model.number="currentBot.minPrice"
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
                v-model.number="currentBot.maxPrice"
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
                v-model.number="currentBot.entryStep"
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
                v-model.number="currentBot.exitStep"
                type="number"
                step="0.01"
                placeholder="Exit step"
                @input="formatNumberInput($event, 'exitStep')"
              ></el-input>
            </el-form-item>
          </div>
          <div class="col-md-6">
            <el-form-item label="Place Orders in Advance">
              <el-checkbox v-model="currentBot.placeOrdersInAdvance"></el-checkbox>
            </el-form-item>
          </div>
        </div>

        <el-form-item v-if="currentBot.placeOrdersInAdvance" label="Orders in Advance">
          <el-input
            v-model.number="currentBot.ordersInAdvance"
            type="number"
            min="1"
            max="1000"
            placeholder="Number of orders"
            @input="formatNumberInput($event, 'ordersInAdvance')"
          ></el-input>
        </el-form-item>
      </el-form>

      <span slot="footer" class="dialog-footer">
        <base-button @click="showCreateModal = false" type="danger">Cancel</base-button>
        <base-button @click="createBot" type="primary">Create</base-button>
      </span>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import {
  ElTable,
  ElTableColumn,
  ElSelect,
  ElOption,
  ElInput,
  ElDialog,
  ElForm,
  ElFormItem,
  ElCheckbox,
  ElTag,
} from "element-plus";
import Card from "@/components/Cards/Card.vue";
import BasePagination from "@/components/BasePagination.vue";
import BaseButton from "@/components/BaseButton.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import BotTable from "@/components/BotTable.vue";
import { useNotifications } from "@/components/Notifications/NotificationPlugin";
import apiClient from "@/services/api";
import { useRouter } from "vue-router";

// Router instance
const router = useRouter();

// State variables
const showCreateModal = ref(false);
const currentPage = ref(1);
const searchTerm = ref('');

// Default bot model
const defaultBot = {
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
  ordersInAdvance: 100,
  exitStep: 0.01,
  entryStep: 0.01,
  entryQuantity: 0,
  startingBaseAmount: 0,
  startFromMaxPrice: false,
  tradingMode: 'Spot',
};

const currentBot = ref({...defaultBot});

// Notification composable
const { notify } = useNotifications();

// Reference to the BotTable instance
const botTableRef = ref(null);

// Fetch bots with pagination and search
async function fetchBots(params) {
  try {
    return await apiClient.get('bots', {
      params: {
        page: params?.page || currentPage.value,
        pageSize: params?.pageSize || 10,
        search: params?.searchQuery || searchTerm.value || undefined,
        sortKey: params?.sortKey,
        sortDirection: params?.sortDirection
      }
    });
  } catch (error) {
    console.error('Error fetching bots:', error);
    notify({
      type: 'danger',
      title: 'Error',
      message: 'Failed to load bots',
      icon: 'fas fa-times',
    });
  }
}

function navigateToBotDetails(bot) {
  router.push({ name: 'BotDetails', params: { id: bot.id }});
}

function createNewBot() {
  currentBot.value = {...defaultBot};
  showCreateModal.value = true;
}

async function createBot() {
  try {
    await apiClient.post('bots', currentBot.value);
    notify({
      type: 'success',
      title: 'Success',
      message: 'Bot created successfully',
      icon: 'fas fa-check',
    });
    showCreateModal.value = false;
    // Refresh the bots list through the BotTable component
    if (botTableRef.value && botTableRef.value.refresh) {
      botTableRef.value.refresh();
    }
  } catch (error) {
    console.error('Error creating bot:', error);
    notify({
      type: 'danger',
      title: 'Error',
      message: error.response?.data?.message || 'Failed to create bot',
      icon: 'fas fa-times',
    });
  }
}

async function deleteBot(bot) {
  // Use a confirm dialog for destructive actions, but show notifications for result
  if (!confirm(`Are you sure you want to delete bot "${bot.name}"? This cannot be undone!`)) return;
  try {
    await apiClient.delete(`bots/${bot.id}`);
    notify({
      type: 'success',
      title: 'Deleted',
      message: 'The bot has been deleted.',
      icon: 'fas fa-check',
    });
    // Refresh will happen automatically through the BotTable component
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

async function toggleBotStatus(bot) {
  try {
    await apiClient.post(`bots/${bot.id}/toggle`);
    notify({
      type: 'success',
      title: 'Success',
      message: `Bot ${!bot.enabled ? 'activated' : 'deactivated'} successfully`,
      icon: 'fas fa-check',
    });
    // Refresh will happen automatically through the BotTable component
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
    currentBot.value[field] = parseFloat(correctedValue);
  }
}
</script>

<style>
.search-input {
  width: 250px;
}

.pagination-select {
  width: 100px;
}

.card-title {
  margin-bottom: 0;
}
</style>