<template>
  <div class="row" v-loading="loading" id="loading"></div>
</template>

<script setup>
import { ref } from "vue";
import { ElLoading } from "element-plus";

const loading = ref(true);
</script>

<style scoped>
#loading {
  min-height: 200px;
  display: flex;
  align-items: center;
}

.el-loading-spinner .path {
  stroke: #66615b !important;
}

.el-loading-mask {
  background: transparent !important;
}
</style>
