<template>
  <Card>
    <template #header>
      <h5 class="title">Edit Profile</h5>
    </template>

    <form @submit.prevent="updateProfile">
      <div class="row">
        <div class="col-md-5">
          <BaseInput
            type="text"
            label="Company"
            :disabled="true"
            placeholder="Company"
            v-model="user.company"
          />
        </div>
        <div class="col-md-3">
          <BaseInput
            type="text"
            label="Username"
            placeholder="Username"
            v-model="user.username"
          />
        </div>
        <div class="col-md-4">
          <BaseInput
            type="email"
            label="Email address"
            placeholder="mike@email.com"
            v-model="user.email"
          />
        </div>
      </div>

      <div class="row">
        <div class="col-md-6">
          <BaseInput
            type="text"
            label="First Name"
            placeholder="First Name"
            v-model="user.firstName"
          />
        </div>
        <div class="col-md-6">
          <BaseInput
            type="text"
            label="Last Name"
            placeholder="Last Name"
            v-model="user.lastName"
          />
        </div>
      </div>

      <div class="row">
        <div class="col-md-12">
          <BaseInput
            type="text"
            label="Address"
            placeholder="Home Address"
            v-model="user.address"
          />
        </div>
      </div>

      <div class="row">
        <div class="col-md-4">
          <BaseInput
            type="text"
            label="City"
            placeholder="City"
            v-model="user.city"
          />
        </div>
        <div class="col-md-4">
          <BaseInput
            type="text"
            label="Country"
            placeholder="Country"
            v-model="user.country"
          />
        </div>
        <div class="col-md-4">
          <BaseInput
            label="Postal Code"
            placeholder="ZIP Code"
            v-model="user.postalCode"
          />
        </div>
      </div>

      <div class="row">
        <div class="col-md-12">
          <BaseInput label="About Me">
            <textarea
              class="form-control"
              placeholder="ZIP Code"
              v-model="user.aboutMe"
            ></textarea>
          </BaseInput>
        </div>
      </div>

      <BaseButton native-type="submit" type="primary" class="btn-fill">
        Save
      </BaseButton>
    </form>
  </Card>
</template>

<script setup>
import { reactive } from "vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import BaseButton from "@/components/BaseButton.vue";
import Card from "@/components/Cards/Card.vue";

const user = reactive({
  company: "Creative Code Inc.",
  username: "michael23",
  email: "",
  firstName: "Mike",
  lastName: "Andrew",
  address: "Bld Mihail Kogalniceanu, nr. 8 Bl 1, Sc 1, Ap 09",
  city: "New York",
  country: "USA",
  postalCode: "",
  aboutMe: `Lamborghini Mercy, Your chick she so thirsty, I'm in that two seat Lambo.`,
});

function updateProfile() {
  alert("Your data: " + JSON.stringify(user, null, 2));
}
</script>

<style scoped>
/* Optional styles */
</style>
