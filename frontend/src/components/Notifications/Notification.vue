<template>
  <div
    class="notification"
    :class="[
      `notification-${notification.type}`,
      notification.horizontalAlign === 'center' ? 'notification-center' : '',
      notification.verticalAlign === 'middle' ? 'notification-middle' : ''
    ]"
    v-show="visible"
    @mouseenter="stopTimer"
    @mouseleave="startTimer"
  >
    <div class="notification-icon">
      <i :class="notification.icon"></i>
    </div>
    <div class="notification-content">
      <span v-if="notification.title" class="notification-title">{{ notification.title }}</span>
      <div class="notification-message" v-html="notification.message"></div>
    </div>
    <a v-if="notification.showClose" class="notification-close-button" @click="close">
      <i class="fas fa-times"></i>
    </a>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue';

const props = defineProps({
  notification: {
    type: Object,
    required: true
  }
});

const emit = defineEmits(['close']);

const visible = ref(false);
let timeout = null;

onMounted(() => {
  visible.value = true;
  startTimer();
});

onBeforeUnmount(() => {
  clearTimeout(timeout);
});

function close() {
  visible.value = false;
  setTimeout(() => {
    emit('close', props.notification.id);
  }, 300);
}

function startTimer() {
  if (props.notification.timeout > 0) {
    clearTimeout(timeout);
    timeout = setTimeout(() => {
      close();
    }, props.notification.timeout);
  }
}

function stopTimer() {
  clearTimeout(timeout);
}
</script>

<style lang="scss" scoped>
.notification {
  display: flex;
  align-items: center;
  max-width: 400px;
  min-width: 300px;
  padding: 15px;
  border-radius: 10px;
  box-shadow: 0 5px 15px -5px rgba(0, 0, 0, 0.3);
  margin-bottom: 15px;
  transition: all 0.3s ease;
  position: relative;
  overflow: visible;
  background-color: #27293d;
  opacity: 0;
  transform: translateY(-20px);
  animation: notification-in 0.3s ease forwards;
  border: none;
  
  .notification-close-button {
    position: absolute;
    top: 10px;
    right: 10px;
    color: rgba(255, 255, 255, 0.5);
    cursor: pointer;
    font-size: 16px;
    z-index: 2;
    background: none;
    border: none;
    padding: 0;
    display: flex;
    align-items: center;
    
    &:hover {
      color: white;
    }
  }
  
  .notification-icon {
    margin-right: 15px;
    font-size: 22px;
    display: flex;
    align-items: center;
    flex-shrink: 0;
    
    i {
      color: white;
    }
  }
  
  .notification-content {
    flex: 1;
    padding-right: 30px;
    min-width: 0;
    display: flex;
    flex-direction: column;
    justify-content: center;
    
    .notification-title {
      font-weight: 600;
      margin-bottom: 2px;
      color: white;
      word-break: break-word;
      font-size: 15px;
      line-height: 1.2;
    }
    
    .notification-message {
      color: rgba(255, 255, 255, 0.85);
      font-size: 14px;
      word-break: break-word;
      line-height: 1.3;
      white-space: pre-line;
      overflow-wrap: break-word;
      max-width: 100%;
    }
  }
  
  &.notification-center {
    margin-left: auto;
    margin-right: auto;
  }
  
  &.notification-middle {
    align-self: center;
  }
  
  &.notification-success {
    .notification-icon i {
      color: #41B883;
    }
    &::after {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      width: 5px;
      height: 100%;
      background-color: #41B883;
    }
  }
  
  &.notification-warning {
    .notification-icon i {
      color: #FFC107;
    }
    &::after {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      width: 5px;
      height: 100%;
      background-color: #FFC107;
    }
  }
  
  &.notification-danger {
    .notification-icon i {
      color: #FF5252;
    }
    &::after {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      width: 5px;
      height: 100%;
      background-color: #FF5252;
    }
  }
  
  &.notification-info {
    .notification-icon i {
      color: #1E88E5;
    }
    &::after {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      width: 5px;
      height: 100%;
      background-color: #1E88E5;
    }
  }
}

@keyframes notification-in {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
