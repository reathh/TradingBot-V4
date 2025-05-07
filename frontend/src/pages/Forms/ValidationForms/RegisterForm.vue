<template>
  <Form @submit="submitForm">
    <card footer-classes="text-right">
      <template #header>
        <h4 class="card-title">Register Form</h4>
      </template>

      <!-- Email Field -->
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

      <!-- Password Field -->
      <Field
        name="password"
        rules="required|confirmed:@confirmation"
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

      <!-- Confirm Password Field -->
      <Field
        name="confirmation"
        rules="required"
        v-slot="{ field, errorMessage, meta }"
      >
        <base-input
          v-bind="field"
          required
          type="password"
          label="Confirm Password"
          :error="errorMessage"
          :class="[
            { 'has-success': meta.valid },
            { 'has-danger': meta.invalid },
          ]"
        />
      </Field>

      <div class="category form-category">* Required fields</div>

      <template #footer>
        <base-checkbox v-model="subscribe" class="pull-left" name="subscribe">
          Accept terms & conditions
        </base-checkbox>
        <base-button type="submit" class="btn btn-primary"
          >Register</base-button
        >
      </template>
    </card>
  </Form>
</template>

<script setup>
import { ref } from "vue";
import { Form, Field, defineRule } from "vee-validate";
import { required, email, confirmed } from "@vee-validate/rules";
import BaseCheckbox from "@/components/Inputs/BaseCheckbox.vue";
import Card from "@/components/Cards/Card.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";

// Define rules
defineRule("required", required);
defineRule("email", email);
defineRule("confirmed", confirmed);

// Form data
const subscribe = ref(false);

const submitForm = (values) => {
  alert(
    "Form has been submitted!\n" +
      JSON.stringify({ ...values, subscribe: subscribe.value }, null, 2)
  );
};
</script>
