<template>
  <div class="extended-forms">
    <div class="row">
      <div class="col-md-4">
        <card>
          <template #header>
            <h4 class="card-title">Datetimepicker</h4>
          </template>
          <base-input>
            <el-date-picker
              v-model="dateTimePicker"
              type="datetime"
              placeholder="Date Time Picker"
            />
          </base-input>
        </card>
      </div>

      <div class="col-md-4">
        <card>
          <template #header>
            <h4 class="card-title">Date Picker</h4>
          </template>
          <base-input>
            <el-date-picker
              v-model="datePicker"
              type="date"
              placeholder="Date Picker"
            />
          </base-input>
        </card>
      </div>

      <div class="col-md-4">
        <card>
          <template #header>
            <h4 class="card-title">Time Picker</h4>
          </template>
          <base-input>
            <el-time-select v-model="timePicker" placeholder="Time Picker" />
          </base-input>
        </card>
      </div>
    </div>

    <card>
      <div class="col-12">
        <div class="row">
          <div class="col-md-6">
            <h4 class="card-title">Toggle Buttons</h4>
            <div class="row">
              <div class="col-md-4">
                <p class="category">Default</p>
                <base-switch
                  v-model="switches.defaultOn"
                  on-text="ON"
                  off-text="OFF"
                  type="primary"
                />
                &nbsp;
                <base-switch
                  v-model="switches.defaultOff"
                  on-text="ON"
                  off-text="OFF"
                  type="primary"
                />
              </div>
              <div class="col-md-4">
                <p class="category">Plain</p>
                <base-switch v-model="switches.plainOn" />
                &nbsp;
                <base-switch v-model="switches.plainOff" />
              </div>
              <div class="col-md-4">
                <p class="category">With Icons</p>
                <base-switch v-model="switches.withIconsOn">
                  <template #on><i class="tim-icons icon-check-2" /></template>
                  <template #off
                    ><i class="tim-icons icon-simple-remove"
                  /></template>
                </base-switch>
                &nbsp;
                <base-switch v-model="switches.withIconsOff">
                  <template #on><i class="tim-icons icon-check-2" /></template>
                  <template #off
                    ><i class="tim-icons icon-simple-remove"
                  /></template>
                </base-switch>
              </div>
            </div>
          </div>

          <div class="col-md-6">
            <h4 class="card-title">Customisable Select</h4>
            <div class="row">
              <div class="col-md-6">
                <el-select
                  v-model="selects.simple"
                  class="select-primary"
                  size="large"
                  placeholder="Single Select"
                >
                  <el-option
                    v-for="option in selects.countries"
                    :key="option.label"
                    :label="option.label"
                    :value="option.value"
                    class="select-primary"
                  />
                </el-select>
              </div>

              <div class="col-md-6">
                <el-select
                  v-model="selects.multiple"
                  class="select-info"
                  size="large"
                  multiple
                  collapse-tags
                  placeholder="Multiple Select"
                >
                  <el-option
                    v-for="option in selects.countries"
                    :key="option.label"
                    :label="option.label"
                    :value="option.value"
                    class="select-info"
                  />
                </el-select>
              </div>
            </div>
          </div>
        </div>

        <div class="row">
          <div class="col-md-6">
            <h4 class="card-title">Tags</h4>
            <tags-input v-model="tags.dynamicTags" />
          </div>
          <div class="col-md-6">
            <h4 class="card-title">Dropdown & Dropup</h4>
            <div class="row">
              <div class="col-xl-4 col-md-6">
                <base-dropdown
                  title="Dropdown"
                  title-classes="dropdown-toggle btn btn-primary btn-block"
                >
                  <h6 class="dropdown-header">Dropdown header</h6>
                  <a class="dropdown-item" href="#">Action</a>
                  <a class="dropdown-item" href="#">Another action</a>
                  <a class="dropdown-item" href="#">Something else here</a>
                </base-dropdown>
              </div>

              <div class="col-xl-4 col-md-6">
                <base-dropdown
                  direction="up"
                  title="Dropup"
                  title-classes="dropdown-toggle btn btn-primary btn-block"
                >
                  <h6 class="dropdown-header">Dropdown header</h6>
                  <a class="dropdown-item" href="#">Action</a>
                  <a class="dropdown-item" href="#">Another action</a>
                  <a class="dropdown-item" href="#">Something else here</a>
                </base-dropdown>
              </div>
            </div>
          </div>
        </div>

        <div class="row">
          <div class="col-md-6">
            <h4 class="card-title">Progress Bars</h4>
            <base-progress label="Default" value-position="right" :value="25" />
            <base-progress
              label="Primary"
              :value="60"
              value-position="right"
              type="primary"
            />
          </div>

          <div class="col-md-6">
            <h4 class="card-title">Sliders</h4>
            <slider v-model="sliders.simple" /> <br />
            <slider
              v-model="sliders.rangeSlider"
              :connect="true"
              type="primary"
            />
          </div>
        </div>

        <div class="row">
          <div class="col-md-4 col-sm-4">
            <h4 class="card-title">Regular Image</h4>
            <ImageUpload @change="onImageChange" select-text="Select Image" />
          </div>
          <div class="col-md-4 col-sm-4">
            <h4 class="card-title">Avatar</h4>
            <ImageUpload
              type="avatar"
              @change="onAvatarChange"
              select-text="Add photo"
            />
          </div>
        </div>
      </div>
    </card>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { ElDatePicker, ElTimeSelect, ElSelect, ElOption } from "element-plus";
import "element-plus/es/components/date-picker/style/css";
import "element-plus/es/components/time-select/style/css";
import "element-plus/es/components/select/style/css";
import "element-plus/es/components/option/style/css";

import BaseInput from "@/components/Inputs/BaseInput.vue";
import BaseSwitch from "@/components/BaseSwitch.vue";
import BaseProgress from "@/components/BaseProgress.vue";
import Slider from "@/components/Slider.vue";
import TagsInput from "@/components/Inputs/TagsInput.vue";
import ImageUpload from "@/components/ImageUpload.vue";
import Card from "@/components/Cards/Card.vue";
import BaseDropdown from "@/components/BaseDropdown.vue";

const dateTimePicker = ref("");
const datePicker = ref("");
const timePicker = ref("");

const switches = ref({
  defaultOn: true,
  defaultOff: false,
  plainOn: true,
  plainOff: false,
  withIconsOn: true,
  withIconsOff: false,
});

const sliders = ref({
  simple: 30,
  rangeSlider: [20, 60],
});

const selects = ref({
  simple: "",
  multiple: "ARS",
  countries: [
    { value: "Bahasa Indonesia", label: "Bahasa Indonesia" },
    { value: "Bahasa Melayu", label: "Bahasa Melayu" },
    { value: "Català", label: "Català" },
    { value: "Dansk", label: "Dansk" },
    { value: "Deutsch", label: "Deutsch" },
    { value: "English", label: "English" },
    { value: "Español", label: "Español" },
    { value: "Eλληνικά", label: "Eλληνικά" },
    { value: "Français", label: "Français" },
    { value: "Italiano", label: "Italiano" },
    { value: "Magyar", label: "Magyar" },
    { value: "Nederlands", label: "Nederlands" },
    { value: "Norsk", label: "Norsk" },
    { value: "Polski", label: "Polski" },
    { value: "Português", label: "Português" },
    { value: "Suomi", label: "Suomi" },
    { value: "Svenska", label: "Svenska" },
    { value: "Türkçe", label: "Türkçe" },
    { value: "Íslenska", label: "Íslenska" },
    { value: "Čeština", label: "Čeština" },
    { value: "Русский", label: "Русский" },
    { value: "ภาษาไทย", label: "ภาษาไทย" },
    { value: "中文 (简体)", label: "中文 (简体)" },
    { value: "中文 (繁體)", label: "中文 (繁體)" },
    { value: "日本語", label: "日本語" },
    { value: "한국어", label: "한국어" },
  ],
});

const tags = ref({
  dynamicTags: ["Tag 1", "Tag 2", "Tag 3"],
});

const images = ref({
  regular: null,
  avatar: null,
});

function onImageChange(file) {
  images.value.regular = file;
}

function onAvatarChange(file) {
  images.value.avatar = file;
}
</script>

<style scoped>
.extended-forms .el-select {
  width: 100%;
  margin-bottom: 30px;
}

.extended-forms .progress {
  margin-bottom: 30px;
}
</style>
