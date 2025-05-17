<template>
  <div class="container">
    <div class="row">
      <!-- Left Info Cards -->
      <div class="col-md-5 ml-auto">
        <div class="info-area info-horizontal mt-5">
          <div class="icon icon-warning">
            <i class="tim-icons icon-chart-bar-32"></i>
          </div>
          <div class="description">
            <h3 class="info-title">Automated Crypto Trading</h3>
            <p class="description">
              Our advanced algorithm executes trades 24/7, helping you capitalize on market opportunities even while you sleep.
            </p>
          </div>
        </div>

        <div class="info-area info-horizontal">
          <div class="icon icon-primary">
            <i class="tim-icons icon-money-coins"></i>
          </div>
          <div class="description">
            <h3 class="info-title">Proven Performance</h3>
            <p class="description">
              Our trading bot has consistently outperformed market benchmarks with a 27% average monthly return across all trading pairs.
            </p>
          </div>
        </div>

        <div class="info-area info-horizontal">
          <div class="icon icon-info">
            <i class="tim-icons icon-lock-circle"></i>
          </div>
          <div class="description">
            <h3 class="info-title">Risk Management</h3>
            <p class="description">
              Built-in protection strategies safeguard your capital with smart stop-loss and position sizing algorithms.
              product.
            </p>
          </div>
        </div>
      </div>

      <!-- Right Form -->
      <div class="col-md-7 mr-auto">
        <form @submit.prevent="submit">
          <Card class="card-register card-white">
            <template #header>
              <img
                class="card-img"
                src="/img/card-primary.png"
                alt="Card image"
              />
              <h4 class="card-title">Register</h4>
            </template>

            <BaseInput
              v-model="email"
              placeholder="Email"
              addon-left-icon="tim-icons icon-email-85"
              type="email"
              :error="emailError"
              :class="[
                { 'has-success': emailMeta.valid },
                { 'has-danger': emailMeta.invalid },
              ]"
            />

            <BaseInput
              v-model="password"
              placeholder="Password"
              addon-left-icon="tim-icons icon-lock-circle"
              type="password"
              :error="passwordError"
              :class="[
                { 'has-success': passwordMeta.valid },
                { 'has-danger': passwordMeta.invalid },
              ]"
            />

            <div v-if="authError" class="text-danger mb-3">
              {{ authError }}
            </div>

            <BaseCheckbox class="text-left">
              I agree to the <a href="#something">terms and conditions</a>.
            </BaseCheckbox>

            <template #footer>
              <BaseButton
                native-type="submit"
                type="primary"
                round
                block
                size="lg"
                :loading="loading"
              >
                Get Started
              </BaseButton>
            </template>
          </Card>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useForm, useField } from "vee-validate";
import * as yup from "yup";
import { ref, computed } from "vue";
import { useAuthStore } from "@/stores/auth";
import Card from "@/components/Cards/Card.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import BaseButton from "@/components/BaseButton.vue";
import BaseCheckbox from "@/components/Inputs/BaseCheckbox.vue";

const authStore = useAuthStore();
const authError = computed(() => authStore.error);
const loading = computed(() => authStore.loading);

useForm({
  validationSchema: yup.object({
    email: yup
      .string()
      .required("Email is required")
      .email("Must be a valid email"),
    password: yup
      .string()
      .required("Password is required")
      .min(6, "Minimum 6 characters"),
  }),
});

// Form fields
const {
  value: email,
  errorMessage: emailError,
  meta: emailMeta,
} = useField("email");
const {
  value: password,
  errorMessage: passwordError,
  meta: passwordMeta,
} = useField("password");

const submit = async () => {
  try {
    await authStore.register({
      email: email.value,
      password: password.value
    });
  } catch (error) {
    // Error is already handled in the store
  }
};
</script>