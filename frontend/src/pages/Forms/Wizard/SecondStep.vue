<template>
  <div>
    <h5 class="info-text">Who are you? (Fileinput)</h5>
    <div class="row justify-content-center">
      <div class="col-lg-10 text-center">
        <ImageUpload @change="onFileChange" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, defineExpose } from "vue";
import ImageUpload from "@/components/ImageUpload.vue";

const emit = defineEmits(["on-validated"]);
const file = ref(null);

function onFileChange(selectedFile) {
  file.value = selectedFile;
}

// This method can be called by the parent using a ref
function validate() {
  emit("on-validated", true, { file: file.value });
  return Promise.resolve(true);
}

defineExpose({ validate });
</script>
