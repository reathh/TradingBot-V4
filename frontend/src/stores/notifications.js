import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useNotificationsStore = defineStore('notifications', () => {
  const notifications = ref([]);
  let notificationId = 1;

  function addNotification(notification) {
    const id = notificationId++;
    const newNotification = {
      id,
      ...notification,
      timestamp: new Date()
    };
    
    notifications.value.push(newNotification);
    
    if (notification.timeout !== 0) {
      setTimeout(() => {
        removeNotification(id);
      }, notification.timeout || 3000);
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

  return {
    notifications,
    addNotification,
    removeNotification,
    clearAllNotifications
  };
});
