<template>
  <form class="form-horizontal" @submit.prevent="handleSubmit(onSubmit)">
    <card>
      <template #header>
        <h4 class="card-title">Range Validation</h4>
      </template>

      <div class="row" v-for="(field, index) in fields" :key="index">
        <label class="col-sm-2 col-form-label">{{ field.label }}</label>
        <div class="col-sm-7">
          <base-input
            v-model="field.model"
            :error="field.meta && field.errorMessage"
            :class="[
              { 'has-success': field.meta && field.meta },
              { 'has-danger': field.meta && field.meta },
            ]"
          />
        </div>
        <label class="col-sm-3 label-on-right">
          <code>{{ field.code }}</code>
        </label>
      </div>

      <div class="text-center">
        <base-button native-type="submit" type="primary">
          Validate inputs
        </base-button>
      </div>
    </card>
  </form>
</template>

<script setup>
import { useForm, useField } from "vee-validate";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import BaseButton from "@/components/BaseButton.vue";
import Card from "@/components/Cards/Card.vue";
import * as yup from "yup";

const schema = yup.object({
  minLength: yup.string().required().min(5),
  maxLength: yup.string().required().max(5),
  range: yup.string().required().min(6).max(10),
  minValue: yup.number().required().min(6),
  maxValue: yup.number().required().max(10),
});

const { handleSubmit } = useForm({ validationSchema: schema });

const minLength = useField("minLength");
const maxLength = useField("maxLength");
const range = useField("range");
const minValue = useField("minValue");
const maxValue = useField("maxValue");

const fields = [
  {
    label: "Min Length",
    model: minLength[0],
    meta: minLength[1],
    errorMessage: minLength[2],
    code: 'min="5"',
  },
  {
    label: "Max Length",
    model: maxLength[0],
    meta: maxLength[1],
    errorMessage: maxLength[2],
    code: 'max="5"',
  },
  {
    label: "Range",
    model: range[0],
    meta: range[1],
    errorMessage: range[2],
    code: 'min="6", max="10"',
  },
  {
    label: "Min Value",
    model: minValue[0],
    meta: minValue[1],
    errorMessage: minValue[2],
    code: 'min_value="6"',
  },
  {
    label: "Max Value",
    model: maxValue[0],
    meta: maxValue[1],
    errorMessage: maxValue[2],
    code: 'max_value="10"',
  },
];

const onSubmit = (values) => {
  alert("Form has been submitted!\n" + JSON.stringify(values, null, 2));
};
</script>

<style scoped>
.has-success input {
  border-color: #28a745;
}
.has-danger input {
  border-color: #dc3545;
}
</style>
