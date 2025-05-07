<template>
  <Form @submit="submitForm" class="form-horizontal">
    <card>
      <template #header>
        <h4 class="card-title">Type Validation</h4>
      </template>

      <div>
        <!-- Required Text -->
        <div class="row">
          <label class="col-sm-2 col-form-label">Required Text</label>
          <div class="col-sm-7">
            <Field
              name="required"
              rules="required"
              v-slot="{ field, errorMessage, meta }"
            >
              <base-input
                v-bind="field"
                :error="errorMessage"
                :class="[
                  { 'has-success': meta.valid },
                  { 'has-danger': meta.invalid },
                ]"
              />
            </Field>
          </div>
          <label class="col-sm-3 label-on-right"
            ><code>required="true"</code></label
          >
        </div>

        <!-- Email -->
        <div class="row">
          <label class="col-sm-2 col-form-label">Email</label>
          <div class="col-sm-7">
            <Field
              name="email"
              rules="required|email"
              v-slot="{ field, errorMessage, meta }"
            >
              <base-input
                v-bind="field"
                type="email"
                :error="errorMessage"
                :class="[
                  { 'has-success': meta.valid },
                  { 'has-danger': meta.invalid },
                ]"
              />
            </Field>
          </div>
          <label class="col-sm-3 label-on-right"
            ><code>email="true"</code></label
          >
        </div>

        <!-- Number -->
        <div class="row">
          <label class="col-sm-2 col-form-label">Number</label>
          <div class="col-sm-7">
            <Field
              name="number"
              rules="required|numeric"
              v-slot="{ field, errorMessage, meta }"
            >
              <base-input
                v-bind="field"
                :error="errorMessage"
                :class="[
                  { 'has-success': meta.valid },
                  { 'has-danger': meta.invalid },
                ]"
              />
            </Field>
          </div>
          <label class="col-sm-3 label-on-right"
            ><code>numeric="true"</code></label
          >
        </div>

        <!-- URL -->
        <div class="row">
          <label class="col-sm-2 col-form-label">URL</label>
          <div class="col-sm-7">
            <Field
              name="url"
              rules="required|regex:/^(https?:\/\/)?([a-z0-9-]+\.)+[a-z]{2,6}(:[0-9]{1,5})?(\/.*)?$/i"
              v-slot="{ field, errorMessage, meta }"
            >
              <base-input
                v-bind="field"
                type="text"
                :error="errorMessage"
                :class="[
                  { 'has-success': meta.valid },
                  { 'has-danger': meta.invalid },
                ]"
              />
            </Field>
          </div>
          <label class="col-sm-3 label-on-right"><code>url="true"</code></label>
        </div>

        <!-- Confirmed Fields -->
        <div class="row">
          <label class="col-sm-2 col-form-label">Confirm</label>
          <div class="col-sm-3">
            <Field
              name="equal"
              rules="required|confirmed:@equalTo"
              v-slot="{ field, errorMessage, meta }"
            >
              <base-input
                v-bind="field"
                type="text"
                :error="errorMessage"
                :class="[
                  { 'has-success': meta.valid },
                  { 'has-danger': meta.invalid },
                ]"
              />
            </Field>
          </div>
          <div class="col-sm-3">
            <Field
              name="equalTo"
              rules="required"
              v-slot="{ field, errorMessage, meta }"
            >
              <base-input
                v-bind="field"
                type="text"
                :error="errorMessage"
                :class="[
                  { 'has-success': meta.valid },
                  { 'has-danger': meta.invalid },
                ]"
              />
            </Field>
          </div>
          <label class="col-sm-4 label-on-right"
            ><code>confirmed="equalTo"</code></label
          >
        </div>
      </div>

      <div class="text-center">
        <base-button type="submit" class="btn btn-primary"
          >Validate inputs</base-button
        >
      </div>
    </card>
  </Form>
</template>

<script setup>
import { Form, Field } from "vee-validate";
import { defineRule } from "vee-validate";
import Card from "@/components/Cards/Card.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import {
  required,
  email,
  numeric,
  confirmed,
  regex,
} from "@vee-validate/rules";

// Register rules
defineRule("required", required);
defineRule("email", email);
defineRule("numeric", numeric);
defineRule("confirmed", confirmed);
defineRule("regex", regex);

// Submission handler
const submitForm = (values) => {
  alert("Form has been submitted!\n" + JSON.stringify(values, null, 2));
};
</script>
