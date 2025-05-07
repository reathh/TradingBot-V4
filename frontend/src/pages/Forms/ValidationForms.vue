<template>
  <div class="row">
    <div class="col-md-6">
      <RegisterForm @on-submit="onRegisterSubmit" />
    </div>
    <div class="col-md-6">
      <LoginForm @on-submit="onLoginSubmit" />
    </div>
    <div class="col-md-12">
      <TypeValidationForm @on-submit="onTypeValidationSubmit" />
    </div>
    <div class="col-md-12">
      <RangeValidationForm @on-submit="onRangeValidationSubmit" />
    </div>
  </div>
</template>

<script setup>
import { reactive } from "vue";

import RegisterForm from "./ValidationForms/RegisterForm.vue";
import LoginForm from "./ValidationForms/LoginForm.vue";
import TypeValidationForm from "./ValidationForms/TypeValidationForm.vue";
import RangeValidationForm from "./ValidationForms/RangeValidationForm.vue";

const registerModel = reactive({});
const loginModel = reactive({});
const typeValidationModel = reactive({});
const rangeValidationModel = reactive({});

const onRegisterSubmit = (model) => {
  Object.assign(registerModel, model);
};

const onLoginSubmit = (model) => {
  Object.assign(loginModel, model);
};

const onTypeValidationSubmit = (model) => {
  Object.assign(typeValidationModel, model);
};

const onRangeValidationSubmit = (model) => {
  Object.assign(rangeValidationModel, model);
};
</script>

<style scoped>
/* You can add scoped styles here if needed */
</style>
