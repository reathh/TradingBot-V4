<template>
  <Form @submit="submitForm">
    <card footer-classes="text-left">
      <template #header>
        <h4 class="card-title">Login Form</h4>
      </template>

      <!-- Full Name -->
      <Field
        name="fullname"
        rules="required|min:3"
        v-slot="{ field, errorMessage, meta }"
      >
        <base-input
          v-bind="field"
          required
          label="Full Name"
          :error="errorMessage"
          :class="[
            { 'has-success': meta.valid },
            { 'has-danger': meta.invalid },
          ]"
        />
      </Field>

      <!-- Email -->
      <Field
        name="email"
        rules="required|email"
        v-slot="{ field, errorMessage, meta }"
      >
        <base-input
          v-bind="field"
          required
          type="email"
          label="Email address"
          :error="errorMessage"
          :class="[
            { 'has-success': meta.valid },
            { 'has-danger': meta.invalid },
          ]"
        />
      </Field>

      <!-- Password -->
      <Field
        name="password"
        rules="required|min:5"
        v-slot="{ field, errorMessage, meta }"
      >
        <base-input
          v-bind="field"
          required
          type="password"
          label="Password"
          :error="errorMessage"
          :class="[
            { 'has-success': meta.valid },
            { 'has-danger': meta.invalid },
          ]"
        />
      </Field>

      <div class="category form-category">* Required fields</div>

      <template #footer>
        <base-button type="submit" class="btn btn-primary">Login</base-button>
        <a href="javascript:void(0)" class="pull-right">Forgot password?</a>
      </template>
    </card>
  </Form>
</template>

<script setup>
import { Form, Field, defineRule } from "vee-validate";
import { required, email, min } from "@vee-validate/rules";
import Card from "@/components/Cards/Card.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";

// Register validation rules
defineRule("required", required);
defineRule("email", email);
defineRule("min", min);

// Submit handler
const submitForm = (values) => {
  alert("Form has been submitted!\n" + JSON.stringify(values, null, 2));
};
</script>
