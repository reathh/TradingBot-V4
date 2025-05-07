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
    
    & > * {
      pointer-events: auto;
    }
    
    &.top-left {
      top: 0;
      left: 0;
    }
    
    &.top-center {
      top: 0;
      left: 50%;
      transform: translateX(-50%);
    }
    
    &.top-right {
      top: 0;
      right: 0;
    }
    
    &.bottom-left {
      bottom: 0;
      left: 0;
    }
    
    &.bottom-center {
      bottom: 0;
      left: 50%;
      transform: translateX(-50%);
    }
    
    &.bottom-right {
      bottom: 0;
      right: 0;
    }
  }
}
</style>
