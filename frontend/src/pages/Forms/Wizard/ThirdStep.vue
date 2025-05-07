<template>
  <Form @submit="onSubmit" v-slot="{ errors }">
    <div class="row justify-content-center">
      <div class="col-sm-12">
        <h5 class="info-text">Are you living in a nice area?</h5>
      </div>

      <div class="col-sm-7">
        <Field
          name="street"
          rules="required|min:5"
          v-slot="{ field, errorMessage, meta }"
        >
          <base-input
            v-bind="field"
            label="Street Name"
            :error="errorMessage"
            :class="[
              { 'has-success': meta.valid },
              { 'has-danger': meta.touched && meta.invalid },
            ]"
          />
        </Field>
      </div>

      <div class="col-sm-3">
        <Field
          name="streetNo"
          rules="required"
          v-slot="{ field, errorMessage, meta }"
        >
          <base-input
            v-bind="field"
            label="Street No"
            :error="errorMessage"
            :class="[
              { 'has-success': meta.valid },
              { 'has-danger': meta.touched && meta.invalid },
            ]"
          />
        </Field>
      </div>

      <div class="col-sm-5">
        <Field
          name="city"
          rules="required"
          v-slot="{ field, errorMessage, meta }"
        >
          <base-input
            v-bind="field"
            label="City"
            :error="errorMessage"
            :class="[
              { 'has-success': meta.valid },
              { 'has-danger': meta.touched && meta.invalid },
            ]"
          />
        </Field>
      </div>

      <div class="col-sm-5">
        <label>Country</label>
        <Field
          name="country"
          rules="required|min:5"
          v-slot="{ field, errorMessage }"
        >
          <base-input :error="errorMessage">
            <el-select
              v-model="field.value"
              class="select-primary"
              placeholder="Select Country"
            >
              <el-option
                v-for="country in countryOptions"
                :key="country"
                :label="country"
                :value="country"
              />
            </el-select>
          </base-input>
        </Field>
      </div>
    </div>
  </Form>
</template>

<script setup>
import { ref, defineExpose, defineEmits } from "vue";
import { Field, Form } from "vee-validate";
import * as yup from "yup";
import { ElSelect, ElOption } from "element-plus";

const emit = defineEmits(["on-validated"]);

const countryOptions = [
  "Australia",
  "Germany",
  "Netherlands",
  "USA",
  "UK",
  "New Zealand",
];

function onSubmit(values) {
  emit("on-validated", true, values);
}

// Expose validate method for parent component
defineExpose({
  validate() {
    return new Promise((resolve) => {
      // Using Form's @submit handles validation automatically
      // We call onSubmit manually to resolve emit
      const formEl = document.querySelector("form");
      formEl?.dispatchEvent(
        new Event("submit", { cancelable: true, bubbles: true })
      );
      // Wait a tick for validation to complete
      setTimeout(() => resolve(true), 100);
    });
  },
});
</script>
