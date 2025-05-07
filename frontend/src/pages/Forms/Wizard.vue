<template>
  <div>
    <div class="row d-flex justify-content-center">
      <div class="col-md-10">
        <SimpleWizard @finish="wizardComplete">
          <template #header>
            <h3 class="card-title">Build your profile</h3>
            <h3 class="description">
              This information will let us know more about you.
            </h3>
          </template>

          <WizardTab :before-change="() => validateStep('step1')">
            <template #label>
              <i class="tim-icons icon-single-02"></i>
              <p>About</p>
            </template>
            <FirstStep ref="step1" @on-validated="onStepValidated" />
          </WizardTab>

          <WizardTab :before-change="() => validateStep('step2')">
            <template #label>
              <i class="tim-icons icon-settings-gear-63"></i>
              <p>Account</p>
            </template>
            <SecondStep ref="step2" @on-validated="onStepValidated" />
          </WizardTab>

          <WizardTab :before-change="() => validateStep('step3')">
            <template #label>
              <i class="tim-icons icon-delivery-fast"></i>
              <p>Address</p>
            </template>
            <ThirdStep ref="step3" />
          </WizardTab>
        </SimpleWizard>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import swal from "sweetalert2";

import FirstStep from "./Wizard/FirstStep.vue";
import SecondStep from "./Wizard/SecondStep.vue";
import ThirdStep from "./Wizard/ThirdStep.vue";
import SimpleWizard from "@/components/Wizard/Wizard.vue";
import WizardTab from "@/components/Wizard/WizardTab.vue";

const step1 = ref(null);
const step2 = ref(null);
const step3 = ref(null);

const wizardModel = ref({});

const validateStep = (stepName) => {
  return stepRefs[stepName]?.value?.validate?.() ?? true;
};

const onStepValidated = (validated, model) => {
  if (validated) {
    wizardModel.value = { ...wizardModel.value, ...model };
  }
};

const wizardComplete = () => {
  swal.fire("Good job!", "You clicked the finish button!", "success");
};

const stepRefs = {
  step1,
  step2,
  step3,
};
</script>
