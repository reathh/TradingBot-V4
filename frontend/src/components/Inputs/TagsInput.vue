<template>
  <div>
    <el-tag
      v-for="(tag, index) in dynamicTags"
      :key="tag + index"
      size="small"
      :type="tagType"
      closable
      :disable-transitions="true"
      @close="handleClose(tag)"
    >
      {{ tag }}
    </el-tag>

    <input
      type="text"
      placeholder="Add new tag"
      class="form-control input-new-tag"
      v-model="inputValue"
      ref="saveTagInput"
      size="mini"
      @keyup.enter="handleInputConfirm"
      @blur="handleInputConfirm"
    />
  </div>
</template>

<script setup>
import { ref, watch, onMounted, nextTick } from "vue";

const props = defineProps({
  modelValue: {
    type: Array,
    default: () => [],
  },
  tagType: {
    type: String,
    default: "primary",
  },
});

const emit = defineEmits(["update:modelValue"]);

// State
const dynamicTags = ref([...props.modelValue]);
const inputValue = ref("");
const saveTagInput = ref(null);

// Watch for external changes to modelValue
watch(
  () => props.modelValue,
  (newVal) => {
    dynamicTags.value = [...newVal];
  },
  { immediate: true }
);

// Methods
function handleClose(tag) {
  dynamicTags.value = dynamicTags.value.filter((t) => t !== tag);
  emit("update:modelValue", dynamicTags.value);
}

function handleInputConfirm() {
  const value = inputValue.value.trim();
  if (value) {
    dynamicTags.value.push(value);
    emit("update:modelValue", dynamicTags.value);
  }
  inputValue.value = "";
}

onMounted(() => {
  nextTick(() => {
    saveTagInput.value?.focus();
  });
});
</script>

<style scoped>
.input-new-tag {
  margin-top: 8px;
}
</style>
