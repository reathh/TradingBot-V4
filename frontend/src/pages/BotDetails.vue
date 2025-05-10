<template>
  <div class="content">
    <div class="md-layout">
      <div class="md-layout-item md-size-100">
        <card>
          <div slot="header" class="d-flex justify-content-between align-items-center">
            <h4 class="card-title">Bot Details: {{ bot.name || 'Loading...' }}</h4>
            <div>
              <base-button 
                v-if="bot.id" 
                @click="toggleBotStatus" 
                :type="bot.enabled ? 'warning' : 'success'" 
                class="mr-2"
              >
                {{ bot.enabled ? 'Deactivate' : 'Activate' }}
              </base-button>
              <base-button 
                v-if="bot.id" 
                @click="deleteBot" 
                type="danger"
                class="ml-2"
              >
                Delete Bot
              </base-button>
              <router-link to="/bots">
                <base-button type="default">
                  Back to Bots
                </base-button>
              </router-link>
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
                  <el-form-item label="Trade Quantity" required>
                    <el-input
                      v-model.number="bot.quantity"
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
                      v-model.number="bot.minPrice"
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
                      v-model.number="bot.maxPrice"
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
                      v-model.number="bot.entryStep"
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
                      v-model.number="bot.exitStep"
                      type="number"
                      step="0.01"
                      placeholder="Exit step"
                      @input="formatNumberInput($event, 'exitStep')"
                    ></el-input>
                  </el-form-item>
                </div>
              </div>

              <el-form-item>
                <el-checkbox v-model="bot.placeOrdersInAdvance">Place orders in advance</el-checkbox>
              </el-form-item>

              <template v-if="bot.placeOrdersInAdvance">
                <div class="row">
                  <div class="col-md-6">
                    <el-form-item label="Entry Orders In Advance">
                      <el-input
                        v-model.number="bot.EntryOrdersInAdvance"
                        type="number"
                        min="1"
                        max="1000"
                        placeholder="Entry orders in advance"
                        @input="formatNumberInput($event, 'EntryOrdersInAdvance')"
                      ></el-input>
                    </el-form-item>
                  </div>
                  <div class="col-md-6">
                    <el-form-item label="Exit Orders In Advance">
                      <el-input
                        v-model.number="bot.ExitOrdersInAdvance"
                        type="number"
                        min="1"
                        max="1000"
                        placeholder="Exit orders in advance"
                        @input="formatNumberInput($event, 'ExitOrdersInAdvance')"
                      ></el-input>
                    </el-form-item>
                  </div>
                </div>
              </template>

              <div class="d-flex justify-content-between mt-4">
                <base-button @click="saveBot" type="primary">Save Changes</base-button>
              </div>
            </el-form>
            <div class="mt-5">
              <TradesTable :botId="bot.id" />
            </div>
          </div>
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
import Swal from "sweetalert2";
import apiClient from "@/services/api";
import TradesTable from '@/components/TradesTable.vue';

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
  EntryOrdersInAdvance: 100,
  ExitOrdersInAdvance: 100
});

// Fetch the bot data
async function fetchBot() {
  loading.value = true;
  try {
    const response = await apiClient.get(`bots/${route.params.id}`);
    bot.value = response.data;
  } catch (error) {
    console.error('Error fetching bot:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to load bot details',
      icon: 'error'
    });
  } finally {
    loading.value = false;
  }
}

// Save the bot data
async function saveBot() {
  try {
    await apiClient.put(`bots/${bot.value.id}`, bot.value);
    Swal.fire({
      title: 'Success',
      text: 'Bot updated successfully',
      icon: 'success',
      timer: 2000,
      showConfirmButton: false
    });
  } catch (error) {
    console.error('Error saving bot:', error);
    Swal.fire({
      title: 'Error',
      text: error.response?.data?.message || 'Failed to save bot',
      icon: 'error'
    });
  }
}

// Toggle bot enabled status
async function toggleBotStatus() {
  try {
    await apiClient.post(`bots/${bot.value.id}/toggle`);
    Swal.fire({
      title: 'Success',
      text: `Bot ${!bot.value.enabled ? 'activated' : 'deactivated'} successfully`,
      icon: 'success',
      timer: 2000,
      showConfirmButton: false
    });
    // Refresh bot data to get updated status
    fetchBot();
  } catch (error) {
    console.error('Error toggling bot status:', error);
    Swal.fire({
      title: 'Error',
      text: 'Failed to update bot status',
      icon: 'error'
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
  const result = await Swal.fire({
    title: 'Are you sure?',
    text: `You are about to delete bot "${bot.value.name}". This cannot be undone!`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Yes, delete it!',
    cancelButtonText: 'Cancel',
    confirmButtonColor: '#d33',
    cancelButtonColor: '#3085d6',
  });
  if (result.isConfirmed) {
    try {
      await apiClient.delete(`bots/${bot.value.id}`);
      Swal.fire('Deleted!', 'The bot has been deleted.', 'success');
      router.push({ name: 'Bots' });
    } catch (error) {
      console.error('Error deleting bot:', error);
      Swal.fire({
        title: 'Error',
        text: 'Failed to delete bot',
        icon: 'error'
      });
    }
  }
}

// Fetch bot on mount
onMounted(() => {
  fetchBot();
});
</script>

<style>
.mr-2 {
  margin-right: 0.5rem;
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
</style> 