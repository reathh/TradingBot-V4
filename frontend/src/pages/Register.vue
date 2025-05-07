<template>
  <div class="container">
    <div class="row">
      <!-- Left Info Cards -->
      <div class="col-md-5 ml-auto">
        <div class="info-area info-horizontal mt-5">
          <div class="icon icon-warning">
            <i class="tim-icons icon-wifi"></i>
          </div>
          <div class="description">
            <h3 class="info-title">Marketing</h3>
            <p class="description">
              We've created the marketing campaign of the website. It was a very
              interesting collaboration.
            </p>
          </div>
        </div>

        <div class="info-area info-horizontal">
          <div class="icon icon-primary">
            <i class="tim-icons icon-triangle-right-17"></i>
          </div>
          <div class="description">
            <h3 class="info-title">Fully Coded in HTML5</h3>
            <p class="description">
              We've developed the website with HTML5 and CSS3. The client has
              access to the code using GitHub.
            </p>
          </div>
        </div>

        <div class="info-area info-horizontal">
          <div class="icon icon-info">
            <i class="tim-icons icon-trophy"></i>
          </div>
          <div class="description">
            <h3 class="info-title">Built Audience</h3>
            <p class="description">
              There is also a Fully Customizable CMS Admin Dashboard for this
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
              v-model="fullname"
              placeholder="Full Name"
              addon-left-icon="tim-icons icon-single-02"
              type="text"
              :error="fullnameError"
              :class="[
                { 'has-success': fullnameMeta.valid },
                { 'has-danger': fullnameMeta.invalid },
              ]"
            />

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
import Card from "@/components/Cards/Card.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import BaseButton from "@/components/BaseButton.vue";
import BaseCheckbox from "@/components/Inputs/BaseCheckbox.vue";

useForm({
  validationSchema: yup.object({
    fullname: yup.string().required("Full name is required"),
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
  value: fullname,
  errorMessage: fullnameError,
  meta: fullnameMeta,
} = useField("fullname");
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
  alert(
    `Registered with:\nName: ${fullname.value}\nEmail: ${email.value}\nPassword: ${password.value}`
  );
};
</script>
