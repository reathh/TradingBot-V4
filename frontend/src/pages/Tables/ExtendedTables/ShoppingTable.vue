<template>
  <div class="table-shopping">
    <el-table style="width: 100%" :data="productsTable">
      <el-table-column min-width="140">
        <template #default="{ row }">
          <div class="img-container">
            <img :src="row.image" alt="product image" />
          </div>
        </template>
      </el-table-column>

      <el-table-column min-width="270" label="Product">
        <template #default="{ row }">
          <div class="td-name">
            <a href="#jacket">{{ row.title }}</a> <br />
            <small>{{ row.description }}</small>
          </div>
        </template>
      </el-table-column>

      <el-table-column min-width="120" label="Color" prop="color" />
      <el-table-column min-width="100" label="Size" prop="size" />

      <el-table-column min-width="100" label="Price" align="center">
        <template #default="{ row }">
          <small>€</small> {{ row.price }}
        </template>
      </el-table-column>

      <el-table-column min-width="160" label="QTY" align="center">
        <template #default="{ row }">
          <div class="btn-group">
            <BaseButton
              type="info"
              class="btn-simple"
              size="sm"
              @click="decreaseQuantity(row)"
            >
              <i class="tim-icons icon-simple-delete"></i>
            </BaseButton>
            <BaseButton type="info" size="sm" @click="increaseQuantity(row)">
              <i class="tim-icons icon-simple-add"></i>
            </BaseButton>
          </div>
          {{ row.quantity }}
        </template>
      </el-table-column>

      <el-table-column min-width="100" label="Amount" align="right">
        <template #default="{ row }">
          <small>€</small> {{ row.amount }}
        </template>
      </el-table-column>

      <el-table-column min-width="60" label="" align="left">
        <template #default>
          <BaseButton type="primary" class="btn-link">
            <i class="tim-icons icon-simple-remove"></i>
          </BaseButton>
        </template>
      </el-table-column>

      <template #append>
        <div class="stats-container">
          <div class="stats-total">
            <div class="stats-total-numbers">
              <div class="td-total">Total</div>
              <div class="td-price mr-2">
                <small>€</small> {{ shoppingTotal }}
              </div>
            </div>
          </div>
          <div class="d-flex justify-content-end">
            <BaseButton type="info" round class="float-right" title="">
              Complete Purchase
              <i class="tim-icons icon-minimal-right"></i>
            </BaseButton>
          </div>
        </div>
      </template>
    </el-table>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import BaseButton from "@/components/BaseButton.vue";

const productsTable = ref([
  {
    image: "/img/jacket.png",
    title: "Suede Biker Jacket ",
    description: "by Saint Laurent",
    color: "Black",
    size: "M",
    price: 3390,
    quantity: 1,
    amount: 3390,
  },
  {
    image: "/img/t-shirt.png",
    title: "Jersey T-Shirt",
    description: "by Balmain",
    color: "Black",
    size: "M",
    price: 499,
    quantity: 2,
    amount: 998,
  },
  {
    image: "/img/gucci.png",
    title: "Slim-Fit Swim Short ",
    description: "by Prada",
    color: "Red",
    size: "M",
    price: 200,
    quantity: 1,
    amount: 200,
  },
]);

const shoppingTotal = computed(() =>
  productsTable.value.reduce((sum, item) => sum + item.amount, 0)
);

function increaseQuantity(row) {
  row.quantity++;
  computeAmount(row);
}

function decreaseQuantity(row) {
  if (row.quantity > 1) {
    row.quantity--;
    computeAmount(row);
  }
}

function computeAmount(row) {
  row.amount = row.quantity * row.price;
}
</script>

<style scoped>
.stats-container {
  display: flex;
  flex-direction: column;
  color: rgba(255, 255, 255, 0.7);
  padding-right: 20px;
}

.stats-total {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 20px;
}

.stats-total-numbers {
  min-width: 240px;
  display: flex;
  justify-content: space-between;
}
</style>
