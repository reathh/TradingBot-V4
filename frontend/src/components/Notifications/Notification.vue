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
  min-height: 64px;
  max-width: 420px;
  width: 100%;
  box-sizing: border-box;
  padding: 16px 40px 16px 20px;
  border-radius: 12px;
  background: #27293d;
  margin-bottom: 16px;
  position: relative;
  font-family: inherit;
  box-shadow: 0 5px 15px -5px rgba(0,0,0,0.3);
  overflow: visible;

  // Left color bar
  &.notification-success { border-left: 5px solid #41B883; }
  &.notification-warning { border-left: 5px solid #FFC107; }
  &.notification-danger  { border-left: 5px solid #FF5252; }
  &.notification-info    { border-left: 5px solid #1E88E5; }

  .notification-icon {
    margin-right: 16px;
    font-size: 24px;
    display: flex;
    align-items: center;
    flex-shrink: 0;
  }

  .notification-content {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    justify-content: center;
    overflow-wrap: break-word;
    word-break: break-word;
    white-space: normal;
  }

  .notification-title {
    font-weight: 700;
    font-size: 16px;
    color: #fff;
    margin-bottom: 2px;
    line-height: 1.2;
    overflow-wrap: break-word;
    word-break: break-word;
    white-space: normal;
  }

  .notification-message {
    font-weight: 400;
    font-size: 15px;
    color: #fff;
    line-height: 1.3;
    overflow-wrap: break-word;
    word-break: break-word;
    white-space: normal;
  }

  .notification-close-button {
    position: absolute;
    top: 16px;
    right: 16px;
    color: rgba(255,255,255,0.6);
    background: none;
    border: none;
    font-size: 18px;
    cursor: pointer;
    padding: 0;
    display: flex;
    align-items: center;
    z-index: 1;
    transition: color 0.2s;
    &:hover { color: #fff; }
  }
}

@media (max-width: 600px) {
  .notification {
    max-width: 95vw;
    padding-left: 12px;
    padding-right: 12px;
  }
}
</style>
