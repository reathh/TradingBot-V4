<template>
  <div class="container">
    <div class="col-lg-4 col-md-6 ml-auto mr-auto">
      <form @submit.prevent="submit">
        <Card class="card-login card-white">
          <template #header>
            <img src="/img/card-primary.png" alt="" />
            <h1 class="card-title">Log in</h1>
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

          <template #footer>
            <BaseButton
              native-type="submit"
              type="primary"
              class="mb-3"
              size="lg"
              block
            >
              Get Started
            </BaseButton>
            <div class="pull-left">
              <h6>
                <router-link class="link footer-link" to="/auth/register"
                  >Create Account</router-link
                >
              </h6>
            </div>
            <div class="pull-right">
              <h6><a href="#" class="link footer-link">Need Help?</a></h6>
            </div>
          </template>
        </Card>
      </form>
    </div>
  </div>
</template>

<script setup>
import { useForm, useField } from "vee-validate";
import * as yup from "yup";
import Card from "@/components/Cards/Card.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import BaseButton from "@/components/BaseButton.vue";

useForm({
  validationSchema: yup.object({
    email: yup.string().required("Email is required").email("Invalid email"),
    password: yup.string().required("Password is required").min(5),
  }),
});

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

const submit = () => {
  alert(`Logging in with\nEmail: ${email.value}\nPassword: ${password.value}`);
};
</script>
