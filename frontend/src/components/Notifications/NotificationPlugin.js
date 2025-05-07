import { ref, reactive } from 'vue';
import Notifications from './Notifications.vue';

const notifications = ref([]);
let notificationId = 1;

const defaultOptions = {
  type: 'success',
  timeout: 3000,
  message: '',
  showClose: true,
  position: 'top-right',
  horizontalAlign: 'right',
  verticalAlign: 'top',
  icon: 'fas fa-bell'
};

function addNotification(notification) {
  const id = notificationId++;
  const newNotification = {
    id,
    ...defaultOptions,
    ...notification,
    timestamp: new Date()
  };
  
  notifications.value.push(newNotification);
  
  if (newNotification.timeout !== 0) {
    setTimeout(() => {
      removeNotification(id);
    }, newNotification.timeout);
  }
  
  return id;
}

function removeNotification(id) {
  const index = notifications.value.findIndex(n => n.id === id);
  if (index !== -1) {
    notifications.value.splice(index, 1);
  }
}

function clearAllNotifications() {
  notifications.value = [];
}

const NotificationStore = {
  notifications,
  addNotification,
  removeNotification,
  clearAllNotifications
};

export function useNotifications() {
  return {
    notify: addNotification,
    clear: removeNotification,
    clearAll: clearAllNotifications,
    notifications
  };
}

export default {
  install(app) {
    app.config.globalProperties.$notifications = {
      notify: addNotification,
      clear: removeNotification,
      clearAll: clearAllNotifications
    };
    
    app.provide('notifications', NotificationStore);
  }
};
