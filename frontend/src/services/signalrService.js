import * as signalR from '@microsoft/signalr';
import config from '@/config';
import { ref } from 'vue';

// State to track if the connection is established
const isConnected = ref(false);

// Event emitter objects for order updates
const orderUpdatedCallbacks = new Set();

// Create the connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${config.apiBaseUrl}/hubs/trading`)
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: retryContext => {
      // Implement exponential backoff with a maximum delay
      const maxDelay = 30000; // 30 seconds
      const baseDelay = 1000; // 1 second
      return Math.min(maxDelay, baseDelay * Math.pow(2, retryContext.previousRetryCount));
    }
  })
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Connection event handlers
connection.onclose(() => {
  isConnected.value = false;
  console.log('SignalR connection closed');
});

connection.onreconnecting(() => {
  isConnected.value = false;
  console.log('SignalR connection reconnecting...');
});

connection.onreconnected(() => {
  isConnected.value = true;
  console.log('SignalR connection reestablished');
});

// Register SignalR event handler for order updates
connection.on('OnOrderUpdated', (orderId) => {
  console.debug(`Order updated: ${orderId}`);
  orderUpdatedCallbacks.forEach(callback => callback(orderId));
});

// Start the connection
async function start() {
  try {
    await connection.start();
    isConnected.value = true;
    console.log('SignalR connected');
  } catch (err) {
    console.error('SignalR connection error:', err);
    isConnected.value = false;
    // Connection start will automatically retry
  }
}

// Export the service API
export default {
  isConnected,
  
  // Start the connection
  start,
  
  // Stop the connection
  stop: async () => {
    try {
      await connection.stop();
      isConnected.value = false;
      console.log('SignalR connection stopped');
    } catch (err) {
      console.error('Error stopping SignalR connection:', err);
    }
  },
  
  // Subscribe to order update events
  onOrderUpdated: (callback) => {
    orderUpdatedCallbacks.add(callback);
    return () => orderUpdatedCallbacks.delete(callback); // Return unsubscribe function
  }
}; 