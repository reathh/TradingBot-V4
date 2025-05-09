<template>
  <div class="content">
    <div class="md-layout">
      <div class="md-layout-item md-size-100">
        <card card-body-classes="table-full-width">
          <div slot="header" class="d-flex justify-content-between align-items-center">
            <h4 class="card-title">Trading Bots</h4>
            <base-button @click="showCreateEditModal = true" type="primary" size="sm" icon>
              <i class="tim-icons icon-simple-add"></i> New Bot
            </base-button>
          </div>
          
          <div>
            <PagedTable
              :columns="botTableColumns"
              :data="bots"
              :searchable="true"
              :sortable="true"
              :page-size-options="pagination.perPageOptions"
              :default-page-size="pagination.perPage"
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
                  <base-button
                    @click.native="viewBotTrades(row)"
                    class="btn-link"
                    type="info"
                    size="sm"
                    icon
                  >
                    <i class="tim-icons icon-chart-bar-32"></i>
                  </base-button>
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
      :visible.sync="showCreateEditModal"
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
              <el-input v-model.number="currentBot.quantity" type="number" step="0.0001" placeholder="Enter quantity"></el-input>
            </el-form-item>
          </div>
        </div>
        
        <div class="row">
          <div class="col-md-6">
            <el-form-item label="Min Price">
              <el-input v-model.number="currentBot.minPrice" type="number" step="0.01" placeholder="Min price"></el-input>
            </el-form-item>
          </div>
          <div class="col-md-6">
            <el-form-item label="Max Price">
              <el-input v-model.number="currentBot.maxPrice" type="number" step="0.01" placeholder="Max price"></el-input>
            </el-form-item>
          </div>
        </div>
        
        <div class="row">
          <div class="col-md-6">
            <el-form-item label="Entry Step" required>
              <el-input v-model.number="currentBot.entryStep" type="number" step="0.01" placeholder="Entry step"></el-input>
            </el-form-item>
          </div>
          <div class="col-md-6">
            <el-form-item label="Exit Step" required>
              <el-input v-model.number="currentBot.exitStep" type="number" step="0.01" placeholder="Exit step"></el-input>
            </el-form-item>
          </div>
        </div>
        
        <el-form-item>
          <el-checkbox v-model="currentBot.placeOrdersInAdvance">Place orders in advance</el-checkbox>
        </el-form-item>
        
        <el-form-item v-if="currentBot.placeOrdersInAdvance" label="Orders in Advance">
          <el-input v-model.number="currentBot.ordersInAdvance" type="number" min="1" max="1000" placeholder="Number of orders"></el-input>
        </el-form-item>
      </el-form>
      
      <span slot="footer" class="dialog-footer">
        <base-button @click="showCreateEditModal = false" type="danger">Cancel</base-button>
        <base-button @click="saveBot" type="primary">{{ isEditing ? 'Update' : 'Create' }}</base-button>
      </span>
    </el-dialog>
    
    <!-- Bot Trades Modal -->
    <el-dialog
      title="Bot Trades"
      :visible.sync="showTradesModal"
      width="800px"
    >
      <el-table :data="botTrades" :empty-text="isLoadingTrades ? 'Loading trades...' : 'No trades found'">
        <el-table-column label="Symbol" prop="entryOrder.symbol" min-width="80"></el-table-column>
        <el-table-column label="Entry Price" min-width="120">
          <template v-slot="scope">
            {{ scope.row.entryOrder && scope.row.entryOrder.averageFillPrice ? scope.row.entryOrder.averageFillPrice.toFixed(2) : 'N/A' }}
          </template>
        </el-table-column>
        <el-table-column label="Exit Price" min-width="120">
          <template v-slot="scope">
            {{ scope.row.exitOrder && scope.row.exitOrder.averageFillPrice ? scope.row.exitOrder.averageFillPrice.toFixed(2) : 'N/A' }}
          </template>
        </el-table-column>
        <el-table-column label="Profit" min-width="100">
          <template v-slot="scope">
            <span :class="scope.row.profit > 0 ? 'text-success' : 'text-danger'">
              {{ scope.row.profit ? scope.row.profit.toFixed(2) : 'N/A' }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="Entry Date" min-width="160">
          <template v-slot="scope">
            {{ scope.row.entryOrder ? formatDate(scope.row.entryOrder.createdAt) : 'N/A' }}
          </template>
        </el-table-column>
        <el-table-column label="Exit Date" min-width="160">
          <template v-slot="scope">
            {{ scope.row.exitOrder ? formatDate(scope.row.exitOrder.createdAt) : 'N/A' }}
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
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
const isLoading = ref(false);
const isLoadingTrades = ref(false);
const bots = ref([]);
const botTrades = ref([]);
const showCreateEditModal = ref(false);
const showTradesModal = ref(false);
const isEditing = ref(false);
const searchQuery = ref('');

// Pagination
const pagination = reactive({
  perPage: 10,
  currentPage: 1,
  perPageOptions: [5, 10, 25, 50],
});

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

// Computed properties
const from = computed(() => pagination.perPage * (pagination.currentPage - 1));

const to = computed(() => {
  const high = from.value + pagination.perPage;
  return Math.min(high, total.value);
});

const total = computed(() => {
  return filteredBots.value.length;
});

const filteredBots = computed(() => {
  if (!searchQuery.value) return bots.value;
  
  const query = searchQuery.value.toLowerCase();
  return bots.value.filter(
    bot => 
      bot.name.toLowerCase().includes(query) || 
      bot.symbol.toLowerCase().includes(query)
  );
});

const paginatedBots = computed(() => {
  return filteredBots.value.slice(from.value, to.value);
});

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

// Methods
async function fetchBots() {
  isLoading.value = true;
  try {
    const response = await axios.get('/api/bots');
    bots.value = response.data;
  } catch (error) {
    console.error('Error fetching bots:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to load bots',
      icon: 'error'
    });
  } finally {
    isLoading.value = false;
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
      const index = bots.value.findIndex(b => b.id === currentBot.value.id);
      if (index !== -1) {
        bots.value[index] = {...currentBot.value};
      }
      Swal.fire({
        title: 'Success',
        text: 'Bot updated successfully',
        icon: 'success',
        timer: 2000,
        showConfirmButton: false
      });
    } else {
      const response = await axios.post('/api/bots', currentBot.value);
      bots.value.push(response.data);
      Swal.fire({
        title: 'Success',
        text: 'Bot created successfully',
        icon: 'success',
        timer: 2000,
        showConfirmButton: false
      });
    }
    showCreateEditModal.value = false;
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
        bots.value = bots.value.filter(b => b.id !== bot.id);
        Swal.fire(
          'Deleted!',
          'The bot has been deleted.',
          'success'
        );
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
    bot.enabled = !bot.enabled;
    Swal.fire({
      title: 'Success',
      text: `Bot ${bot.enabled ? 'activated' : 'deactivated'} successfully`,
      icon: 'success',
      timer: 2000,
      showConfirmButton: false
    });
  } catch (error) {
    console.error('Error toggling bot status:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to update bot status',
      icon: 'error'
    });
  }
}

async function viewBotTrades(bot) {
  isLoadingTrades.value = true;
  botTrades.value = [];
  showTradesModal.value = true;
  
  try {
    const response = await axios.get(`/api/bots/${bot.id}/trades`);
    botTrades.value = response.data;
  } catch (error) {
    console.error('Error fetching bot trades:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to load bot trades',
      icon: 'error'
    });
  } finally {
    isLoadingTrades.value = false;
  }
}

function formatDate(dateString) {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString();
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