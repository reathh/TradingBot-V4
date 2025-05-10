<template>
  <div class="notifications-container">
    <div
      v-for="position in positions"
      :key="position"
      :class="`notifications-position ${position}`"
    >
      <Notification
        v-for="notification in notificationsForPosition(position)"
        :key="notification.id"
        :notification="notification"
        @close="removeNotification"
      />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import Notification from './Notification.vue';
import { useNotifications } from './NotificationPlugin';

const { notifications, clear: removeNotification } = useNotifications();

const positions = computed(() => [
  'top-left',
  'top-center',
  'top-right',
  'bottom-left',
  'bottom-center',
  'bottom-right'
]);

const notificationsForPosition = (position) => {
  return notifications.value.filter(
    notification => `${notification.verticalAlign}-${notification.horizontalAlign}` === position
  );
};
</script>

<style lang="scss" scoped>
.notifications-container {
  position: fixed;
  z-index: 9999;
  pointer-events: none;
  width: 100%;
  height: 100%;
  top: 0;
  left: 0;
  
  .notifications-position {
    position: absolute;
    display: flex;
    flex-direction: column;
    padding: 15px;
    pointer-events: none;
    max-width: 420px;
    width: auto;
    overflow-wrap: break-word;
    word-break: break-word;
    
    & > * {
      pointer-events: auto;
    }
    
    &.top-left {
      top: 20px;
      left: 20px;
    }
    
    &.top-center {
      top: 20px;
      left: 50%;
      transform: translateX(-50%);
    }
    
    &.top-right {
      top: 20px;
      right: 20px;
      align-items: flex-end;
    }
    
    &.bottom-left {
      bottom: 20px;
      left: 20px;
    }
    
    &.bottom-center {
      bottom: 20px;
      left: 50%;
      transform: translateX(-50%);
    }
    
    &.bottom-right {
      bottom: 20px;
      right: 20px;
      align-items: flex-end;
    }
  }
}
</style>
