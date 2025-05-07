<template>
  <div class="wizard-container">
    <div class="card card-wizard active" id="wizardProfile">
      <form @submit.prevent>
        <div class="card-header text-center">
          <slot name="header">
            <h3 v-if="title" class="card-title">{{ title }}</h3>
            <h5 v-if="subTitle" class="description">{{ subTitle }}</h5>
          </slot>

          <div class="wizard-navigation">
            <div class="progress-with-circle">
              <div
                class="progress-bar"
                role="progressbar"
                :style="{ width: `${progress}%` }"
              ></div>
            </div>
            <ul class="nav nav-pills">
              <li
                v-for="(tab, index) in tabs"
                :key="tab.tabId"
                :id="`step-${tab.tabId}`"
                class="nav-item wizard-tab-link"
                :style="linkWidth.value"
              >
                <a
                  class="nav-link"
                  @click="navigateToTab(index)"
                  :class="{
                    'disabled-wizard-link': !tab.checked,
                    active: tab.active,
                    checked: tab.checked,
                  }"
                >
                  <slot name="tab" :tab="tab">
                    <span>{{ tab.label }}</span>
                  </slot>
                </a>
              </li>
            </ul>
          </div>
        </div>

        <div class="card-body">
          <div class="tab-content">
            <slot :activeIndex="activeTabIndex" :activeTab="activeTab" />
          </div>
        </div>

        <div class="card-footer">
          <slot name="footer" :next-tab="nextTab" :prev-tab="prevTab">
            <div class="pull-right">
              <BaseButton
                v-if="activeTabIndex < tabCount - 1"
                type="primary"
                wide
                @click="nextTab"
              >
                {{ nextButtonText }}
              </BaseButton>
              <BaseButton v-else wide @click="nextTab">
                {{ finishButtonText }}
              </BaseButton>
            </div>
            <div class="pull-left">
              <BaseButton
                v-if="activeTabIndex > 0"
                type="primary"
                wide
                @click="prevTab"
              >
                {{ prevButtonText }}
              </BaseButton>
            </div>
            <div class="clearfix"></div>
          </slot>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import {
  ref,
  computed,
  provide,
  defineProps,
  onMounted,
  watch,
  nextTick,
} from "vue";
import BaseButton from "@/components/BaseButton.vue";

const props = defineProps({
  startIndex: { type: Number, default: 0 },
  title: { type: String, default: "Title" },
  subTitle: { type: String, default: "Subtitle" },
  prevButtonText: { type: String, default: "Previous" },
  nextButtonText: { type: String, default: "Next" },
  finishButtonText: { type: String, default: "Finish" },
  vertical: { type: Boolean, default: false },
});

const emit = defineEmits(["tab-change", "update:startIndex"]);

const tabs = ref([]);
const activeTabIndex = ref(0);
const tabLinkWidth = ref(0);

const tabCount = computed(() => tabs.value.length);
const linkWidth = computed(() => ({
  width: vertical ? "100%" : `${100 / tabCount.value}%`,
}));

const activeTab = computed(() => tabs.value[activeTabIndex.value]);
const stepPercentage = computed(() => (1 / (tabCount.value * 2)) * 100);
const progress = computed(() => {
  const step = stepPercentage.value;
  return activeTabIndex.value > 0
    ? step * (activeTabIndex.value * 2 + 1)
    : step;
});

function addTab(tab) {
  const index = tabs.value.length;
  const tabId = `${(tab.title || "").replace(/ /g, "")}${index}`;
  tab.tabId = tabId;
  if (index === 0) {
    tab.active = true;
    tab.checked = true;
  }
  tabs.value.splice(index, 0, tab);
}

function removeTab(tab) {
  const index = tabs.value.indexOf(tab);
  if (index !== -1) {
    tabs.value.splice(index, 1);
  }
}

provide("addTab", addTab);
provide("removeTab", removeTab);

async function validate(tab = activeTab.value) {
  if (tab.beforeChange) {
    try {
      const res = await tab.beforeChange();
      tab.hasError = !res;
      return res;
    } catch {
      tab.hasError = true;
      return false;
    }
  }
  return true;
}

async function nextTab() {
  const isValid = await validate();
  if (isValid && activeTabIndex.value < tabCount.value - 1) {
    activeTabIndex.value++;
  }
  return isValid;
}

function prevTab() {
  if (activeTabIndex.value > 0) {
    activeTabIndex.value--;
  }
}

async function navigateToTab(index) {
  if (tabs.value[index].checked) {
    if (index > activeTabIndex.value) {
      const valid = await nextTab();
      if (valid) navigateToTab(index);
    } else {
      activeTabIndex.value = index;
    }
  }
}

function onResize() {
  const link = document.querySelector(".wizard-tab-link");
  if (link) {
    tabLinkWidth.value = link.clientWidth;
  }
}

onMounted(() => {
  activeTabIndex.value = props.startIndex;
  nextTick(() => {
    if (tabs.value[activeTabIndex.value]) {
      tabs.value[activeTabIndex.value].active = true;
      tabs.value[activeTabIndex.value].checked = true;
    }
    onResize();
  });

  window.addEventListener("resize", () => {
    onResize();
  });
});

watch(activeTabIndex, (newVal, oldVal) => {
  const oldTab = tabs.value[oldVal];
  const newTab = tabs.value[newVal];
  if (oldTab) oldTab.active = false;
  if (newTab) {
    newTab.active = true;
    newTab.checked = true;
  }
  emit("tab-change", oldTab, newTab);
  emit("update:startIndex", newVal);
});
</script>

<style scoped lang="scss">
.tab-content {
  display: flex;
  .tab-pane {
    display: block;
    animation: fadeIn 0.5s;
    width: 100%;
  }
}

.wizard-navigation .nav-link {
  &.active,
  &.checked {
    cursor: pointer;
  }
}

.disabled-wizard-link {
  cursor: not-allowed;
}
</style>
