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
            <PagedTable
              :columns="botTableColumns"
              :data="pagedResult.items"
              :page="currentPage"
              :total-pages="pagedResult.totalPages"
              :total-count="pagedResult.totalCount"
              :searchable="true"
              :sortable="true"
              :server-side="true"
              :fetch-data="fetchBots"
              thead-classes="text-primary"
            >
              <template #row="{ row }">
                <td>{{ row.id }}</td>
                <td>{{ row.name }}</td>
                <td>{{ row.symbol }}</td>
                <td>{{ row.quantity ? row.quantity.toFixed(4) : '0.0000' }}</td>
                <td>
                  <el-tag :type="row.enabled ? 'success' : 'danger'">
                    {{ row.enabled ? 'Active' : 'Inactive' }}
                  </el-tag>
                </td>
                <td>
                  {{ row.minPrice ? row.minPrice.toFixed(2) : 'N/A' }} - {{ row.maxPrice ? row.maxPrice.toFixed(2) : 'N/A' }}
                </td>
                <td>
                  <span :class="row.isLong ? 'text-success' : 'text-danger'">
                    {{ row.isLong ? 'Long' : 'Short' }}
                  </span>
                </td>
                <td class="text-right">
                  <base-button
                    @click.native="toggleBotStatus(row)"
                    class="btn-link"
                    :type="row.enabled ? 'warning' : 'success'"
                    size="sm"
                    icon
                  >
                    <i :class="row.enabled ? 'tim-icons icon-button-pause' : 'tim-icons icon-button-power'"></i>
                  </base-button>
                  <router-link :to="{ name: 'Trades', query: { botId: row.id } }">
                    <base-button
                      class="btn-link"
                      type="info"
                      size="sm"
                      icon
                    >
                      <i class="tim-icons icon-chart-bar-32"></i>
                    </base-button>
                  </router-link>
                  <base-button
                    @click.native="editBot(row)"
                    class="edit btn-link"
                    type="warning"
                    size="sm"
                    icon
                  >
                    <i class="tim-icons icon-pencil"></i>
                  </base-button>
                  <base-button
                    @click.native="deleteBot(row)"
                    class="remove btn-link"
                    type="danger"
                    size="sm"
                    icon
                  >
                    <i class="tim-icons icon-simple-remove"></i>
                  </base-button>
                </td>
              </template>
            </PagedTable>
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
import Swal from "sweetalert2";
import axios from "axios";
import PagedTable from '@/components/PagedTable.vue';

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

// Fetch bots with pagination and search
async function fetchBots(params) {
  try {
    const response = await axios.get('/api/bots', {
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

function formatDate(dateString) {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString();
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

// Fetch bots on mount
onMounted(() => {
  fetchBots();
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