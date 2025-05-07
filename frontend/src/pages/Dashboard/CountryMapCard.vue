<template>
  <Card>
    <template #header>
      <h4 class="card-title">Global Sales by Top Locations</h4>
      <p class="card-category">All products that were shipped</p>
    </template>

    <div class="row">
      <div class="col-md-6 ml-auto mr-auto">
        <div class="table-responsive">
          <table class="table">
            <tbody>
              <tr v-for="country in countries" :key="country.name">
                <td>
                  <div class="flag">
                    <img
                      :src="`/img/${country.code}.png`"
                      :alt="country.code"
                    />
                  </div>
                </td>
                <td>{{ country.name }}</td>
                <td class="text-right">{{ country.sales }}</td>
                <td class="text-right">{{ country.percent }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="col-md-6">
        <WorldMap style="height: 300px" />
      </div>
    </div>
  </Card>
</template>

<script setup>
import { defineAsyncComponent, ref } from "vue";
import Card from "@/components/Cards/Card.vue";

const WorldMap = defineAsyncComponent(() =>
  import("@/components/WorldMap/AsyncWorldMap.vue")
);

const countries = ref([
  { code: "US", name: "USA", sales: "2,920", percent: "53.23%" },
  { code: "DE", name: "Germany", sales: "1,300", percent: "20.43%" },
  { code: "AU", name: "Australia", sales: "760", percent: "10.35%" },
  { code: "GB", name: "United Kingdom", sales: "690", percent: "7.87%" },
  { code: "RO", name: "Romania", sales: "600", percent: "5.94%" },
  { code: "BR", name: "Brasil", sales: "550", percent: "4.34%" },
]);
</script>
