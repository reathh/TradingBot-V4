<template>
  <div class="container">
    <div class="row">
      <div class="col-md-6 mx-auto text-center">
        <h1 class="title">Pick the best plan for you</h1>
        <h4 class="description">
          You have Free Unlimited Updates and Premium Support on each package.
        </h4>
      </div>
    </div>

    <div class="row">
      <div
        v-for="plan in pricingPlans"
        :key="plan.name"
        class="col-lg-3 col-md-6"
      >
        <Card
          type="pricing"
          :class="plan.cardClass"
          footer-classes="text-center mb-3 mt-3"
        >
          <h1 class="card-title">{{ plan.name }}</h1>
          <img class="card-img" :src="plan.image" :alt="plan.name" />
          <ul class="list-group">
            <li
              v-for="(feature, index) in plan.features"
              :key="index"
              class="list-group-item"
            >
              {{ feature }}
            </li>
          </ul>
          <div class="card-prices">
            <h3 class="text-on-front"><span>$</span>{{ plan.price }}</h3>
            <h5 class="text-on-back">{{ plan.price }}</h5>
            <p class="plan">{{ plan.description }}</p>
          </div>

          <template #footer>
            <BaseButton :type="plan.buttonType" round class="btn-just-icon">
              Get started
            </BaseButton>
          </template>
        </Card>
      </div>
    </div>

    <div class="col-md-12 mt-5">
      <h3 class="title">Professional Plan</h3>
    </div>

    <div class="row">
      <div class="col-md-4">
        <p>
          Premium pricing is the strategy of consistently pricing at, or near,
          the high end of the possible price range to help attract
          status-conscious consumers. The high pricing of a premium product ...
        </p>
      </div>
      <div class="col-md-6 ms-auto">
        <BaseProgress
          type="warning"
          :show-value="false"
          badge="500GB"
          :value="75"
        />
        <BaseProgress
          type="primary"
          :show-value="false"
          badge="4 years"
          :value="50"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import Card from "@/components/Cards/Card.vue";
import BaseButton from "@/components/BaseButton.vue";
import BaseProgress from "@/components/BaseProgress.vue";

const pricingPlans = [
  {
    name: "pro",
    price: 95,
    description: "Professional plan",
    features: ["300 messages", "150 emails", "24/7 Support"],
    image: "/img/card-primary.png",
    cardClass: "card-primary",
    buttonType: "primary",
  },
  {
    name: "basic",
    price: 57,
    description: "Basic plan",
    features: ["50 messages", "100 emails", "24/7 Support"],
    image: "/img/card-success.png",
    cardClass: "card-success card-white",
    buttonType: "success",
  },
  {
    name: "mid",
    price: 72,
    description: "Medium plan",
    features: ["200 messages", "130 emails", "24/7 Support"],
    image: "/img/card-warning.png",
    cardClass: "card-warning card-raised card-white",
    buttonType: "warning",
  },
  {
    name: "trial",
    price: 9,
    description: "Trial plan",
    features: ["50 messages", "50 emails", "No Support"],
    image: "/img/card-danger.png",
    cardClass: "card-danger card-white",
    buttonType: "danger",
  },
];
</script>

<style scoped></style>
