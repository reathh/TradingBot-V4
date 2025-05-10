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
              :showViewButton="false"
              :showRefreshButton="false"
              :showToggleStatusButton="true"
              :showTradesButton="true"
              :showEditButton="true"
              :showDeleteButton="true"
              @toggle-status="toggleBotStatus"
              @edit="editBot"
              @delete="deleteBot"
            ></BotTable>
          </div>
        </card>
      </div>
    </div>

    <!-- Create/Edit Bot Modal -->
    <el-dialog
      :title="isEditing ? 'Edit Bot' : 'Create New Bot'"
      v-model="showCreateEditModal"
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
            <el-form-item label="Trade Quantity" required>
              <el-input
                v-model.number="currentBot.quantity"
                type="number"
                step="0.0001"
                placeholder="Enter quantity"
                @input="formatNumberInput($event, 'quantity')"
              ></el-input>
            </el-form-item>
          </div>
        </div>

        <div class="row">
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
        </div>

        <div class="row">
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
        </div>

        <el-form-item>
          <el-checkbox v-model="currentBot.placeOrdersInAdvance">Place orders in advance</el-checkbox>
        </el-form-item>

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
        <base-button @click="showCreateEditModal = false" type="danger">Cancel</base-button>
        <base-button @click="saveBot" type="primary">{{ isEditing ? 'Update' : 'Create' }}</base-button>
      </span>
    </el-dialog>

    <!-- Trade modal removed, now using dedicated Trades page -->
  </div>
</template>

<script setup>
import { ref, reactive, computed, watch, onMounted } from "vue";
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
import Swal from "sweetalert2";
import axios from "axios";
import { useRoute, useRouter } from "vue-router";

// Route related
const route = useRoute();
const router = useRouter();
const isSingleBotView = computed(() => route.name === 'Bot' && route.params.id);

// State variables
const pagedResult = ref({
  page: 1,
  pageSize: 10,
  totalPages: 0,
  totalCount: 0,
  items: []
});
const currentPage = ref(1);
const searchTerm = ref('');
const showCreateEditModal = ref(false);
const isEditing = ref(false);

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
  startFromMaxPrice: false
};

const currentBot = ref({...defaultBot});

const botTableColumns = [
  { key: 'id', label: 'ID', minWidth: 70 },
  { key: 'name', label: 'Name', minWidth: 200 },
  { key: 'symbol', label: 'Symbol', minWidth: 120 },
  { key: 'quantity', label: 'Quantity', minWidth: 100 },
  { key: 'status', label: 'Status', minWidth: 100 },
  { key: 'minMaxPrice', label: 'Min/Max Price', minWidth: 150 },
  { key: 'direction', label: 'Direction', minWidth: 100 },
  { key: 'actions', label: 'Actions', minWidth: 200, align: 'right' },
];

// Fetch specific bot by ID
async function fetchBotById(id) {
  try {
    const response = await axios.get(`/bots/${id}`);
    // If we're in single bot view, display only this bot
    if (isSingleBotView.value) {
      pagedResult.value = {
        page: 1,
        pageSize: 1,
        totalPages: 1,
        totalCount: 1,
        items: [response.data]
      };
    }
    return response.data;
  } catch (error) {
    console.error('Error fetching bot by ID:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to load bot details',
      icon: 'error'
    });
    // Redirect back to bots list if there's an error
    if (isSingleBotView.value) {
      router.push({ name: 'Bots' });
    }
  }
}

// Fetch bots with pagination and search
async function fetchBots(params) {
  // If we're in single bot view, get that specific bot
  if (isSingleBotView.value) {
    await fetchBotById(route.params.id);
    return;
  }

  try {
    const response = await axios.get('/bots', {
      params: {
        page: params?.page || currentPage.value,
        pageSize: params?.pageSize || 10,
        search: params?.searchQuery || searchTerm.value || undefined,
        sortKey: params?.sortKey,
        sortDirection: params?.sortDirection
      }
    });

    pagedResult.value = response.data;

    // Sync the current page and page size
    if (params) {
      currentPage.value = params.page || currentPage.value;
      if (params.searchQuery !== undefined) {
        searchTerm.value = params.searchQuery;
      }
    }
  } catch (error) {
    console.error('Error fetching bots:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to load bots',
      icon: 'error'
    });
  }
}

function editBot(bot) {
  isEditing.value = true;
  currentBot.value = {...bot};
  showCreateEditModal.value = true;
}

function createNewBot() {
  isEditing.value = false;
  currentBot.value = {...defaultBot};
  showCreateEditModal.value = true;
}

async function saveBot() {
  try {
    if (isEditing.value) {
      await axios.put(`/api/bots/${currentBot.value.id}`, currentBot.value);
      Swal.fire({
        title: 'Success',
        text: 'Bot updated successfully',
        icon: 'success',
        timer: 2000,
        showConfirmButton: false
      });
    } else {
      await axios.post('/api/bots', currentBot.value);
      Swal.fire({
        title: 'Success',
        text: 'Bot created successfully',
        icon: 'success',
        timer: 2000,
        showConfirmButton: false
      });
    }
    showCreateEditModal.value = false;
    // Refresh the bots list
    fetchBots();
  } catch (error) {
    console.error('Error saving bot:', error);
    Swal.fire({
      title: 'Error',
      text: error.response?.data?.message || 'Failed to save bot',
      icon: 'error'
    });
  }
}

async function deleteBot(bot) {
  Swal.fire({
    title: 'Are you sure?',
    text: `You are about to delete bot "${bot.name}". This cannot be undone!`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Yes, delete it!',
    cancelButtonText: 'Cancel',
    confirmButtonColor: '#d33',
    cancelButtonColor: '#3085d6',
  }).then(async (result) => {
    if (result.isConfirmed) {
      try {
        await axios.delete(`/api/bots/${bot.id}`);
        Swal.fire(
          'Deleted!',
          'The bot has been deleted.',
          'success'
        );
        // Refresh the bots list
        fetchBots();
      } catch (error) {
        console.error('Error deleting bot:', error);
        Swal.fire({
          title: 'Error',
          text: 'Failed to delete bot',
          icon: 'error'
        });
      }
    }
  });
}

async function toggleBotStatus(bot) {
  try {
    await axios.post(`/api/bots/${bot.id}/toggle`);
    Swal.fire({
      title: 'Success',
      text: `Bot ${!bot.enabled ? 'activated' : 'deactivated'} successfully`,
      icon: 'success',
      timer: 2000,
      showConfirmButton: false
    });
    // Refresh the bots list to get updated status
    fetchBots();
  } catch (error) {
    console.error('Error toggling bot status:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to update bot status',
      icon: 'error'
    });
  }
}

// Trade related functions moved to Trades.vue page

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

// Fetch bots on mount
onMounted(() => {
  if (isSingleBotView.value) {
    fetchBotById(route.params.id);
  } else {
    fetchBots();
  }
});
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