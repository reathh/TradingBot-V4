<template>
  <div class="fileinput text-center">
    <div
      class="fileinput-new thumbnail"
      :class="{ 'img-circle': type === 'avatar' }"
    >
      <img :src="computedImage" alt="preview" />
    </div>
    <div>
      <span class="btn btn-primary btn-simple btn-file">
        <span class="fileinput-new">{{
          fileExists ? changeText : selectText
        }}</span>
        <input type="hidden" value="" name="" />
        <input
          accept="image/*"
          type="file"
          class="valid"
          :multiple="false"
          aria-invalid="false"
          @change="handlePreview"
        />
      </span>
      <base-button v-if="fileExists" @click="removeFile" round type="danger">
        <i class="fas fa-times"></i> {{ removeText }}
      </base-button>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import BaseButton from "@/components/BaseButton.vue";

// Props
const props = defineProps({
  type: {
    type: String,
    default: "",
    description: 'Image upload type (""|avatar)',
  },
  src: { type: String, default: "", description: "Initial image to display" },
  selectText: { type: String, default: "Select image" },
  changeText: { type: String, default: "Change" },
  removeText: { type: String, default: "Remove" },
});

// Emit
const emit = defineEmits(["change"]);

// Placeholders
const avatarPlaceholder = "/img/placeholder.jpg";
const imgPlaceholder = "/img/image_placeholder.jpg";

// State
const imagePreview = ref(null);
const placeholder = computed(() =>
  props.type === "avatar" ? avatarPlaceholder : imgPlaceholder
);

const fileExists = computed(() => imagePreview.value !== null);

const computedImage = computed(
  () => imagePreview.value || props.src || placeholder.value
);

// Methods
function handlePreview(event) {
  const file = event.target.files[0];
  if (file) {
    imagePreview.value = URL.createObjectURL(file);
    emit("change", file);
  }
}

function removeFile() {
  imagePreview.value = null;
  emit("change", null);
}
</script>
