<template>
  <form @submit.prevent="handleSubmit(onSubmit)">
    <div>
      <h5 class="info-text">
        Let's start with the basic information (with validation)
      </h5>
      <div class="row justify-content-center mt-5">
        <div class="col-sm-5">
          <base-input
            required
            v-model="firstName"
            placeholder="First Name"
            addon-left-icon="tim-icons icon-single-02"
            :error="errors.firstName"
            :class="[
              { 'has-success': meta.firstName?.valid },
              { 'has-danger': meta.firstName?.invalid },
            ]"
          />

          <base-input
            required
            v-model="email"
            placeholder="Email"
            addon-left-icon="tim-icons icon-email-85"
            :error="errors.email"
            :class="[
              { 'has-success': meta.email?.valid },
              { 'has-danger': meta.email?.invalid },
            ]"
          />
        </div>
        <div class="col-sm-5">
          <base-input
            required
            v-model="lastName"
            placeholder="Last Name"
            addon-left-icon="tim-icons icon-caps-small"
            :error="errors.lastName"
            :class="[
              { 'has-success': meta.lastName?.valid },
              { 'has-danger': meta.lastName?.invalid },
            ]"
          />

          <base-input
            required
            v-model="phone"
            placeholder="Phone"
            addon-left-icon="tim-icons icon-mobile"
            :error="errors.phone"
            :class="[
              { 'has-success': meta.phone?.valid },
              { 'has-danger': meta.phone?.invalid },
            ]"
          />
        </div>
        <div class="col-sm-10">
          <base-input
            required
            v-model="address"
            placeholder="Address"
            addon-left-icon="tim-icons icon-square-pin"
            :error="errors.address"
            :class="[
              { 'has-success': meta.address?.valid },
              { 'has-danger': meta.address?.invalid },
            ]"
          />
        </div>
      </div>
    </div>
  </form>
</template>

<script setup>
import { reactive, toRefs } from "vue";
import { useForm } from "vee-validate";
import * as yup from "yup";

// Validation Schema
const schema = yup.object({
  firstName: yup.string().required().min(5),
  lastName: yup.string().required().min(5),
  email: yup.string().required().email(),
  phone: yup.string().required().matches(/^\d+$/, "Must be numeric"),
  address: yup.string().required(),
});

// Form Setup
const { handleSubmit, errors, meta, values } = useForm({
  validationSchema: schema,
});

// Refs to bind v-model
const { firstName, lastName, email, phone, address } = toRefs(values);

const emit = defineEmits(["on-validated"]);

const onSubmit = (formData) => {
  emit("on-validated", formData);
};
</script>
