<template>
  <div>
    <div class="row">
      <RouteBreadCrumb />
    </div>

    <Card>
      <template #header>
        <h4 class="card-title">Table with Links</h4>
      </template>
      <div class="row">
        <div class="col-sm-12">
          <ElTable :data="tableData">
            <ElTableColumn min-width="50" label="#" align="center">
              <template #default="{ row }">
                <div class="photo">
                  <img :src="row.img" alt="Table image" />
                </div>
              </template>
            </ElTableColumn>

            <ElTableColumn prop="name" min-width="150" label="Name" />
            <ElTableColumn prop="job" min-width="200" label="Job Position" />
            <ElTableColumn
              prop="since"
              min-width="150"
              align="center"
              label="Since"
            />
            <ElTableColumn
              prop="salary"
              min-width="150"
              align="right"
              label="Salary"
            />

            <ElTableColumn min-width="150" align="right" label="Actions">
              <template #default="{ $index }">
                <div class="text-right table-actions">
                  <ElTooltip
                    content="Refresh"
                    effect="light"
                    :open-delay="300"
                    placement="top"
                  >
                    <BaseButton
                      :type="$index > 2 ? 'success' : 'neutral'"
                      icon
                      size="sm"
                      class="btn-link"
                    >
                      <i class="tim-icons icon-refresh-01" />
                    </BaseButton>
                  </ElTooltip>
                  <ElTooltip
                    content="Delete"
                    effect="light"
                    :open-delay="300"
                    placement="top"
                  >
                    <BaseButton
                      :type="$index > 2 ? 'danger' : 'neutral'"
                      icon
                      size="sm"
                      class="btn-link"
                    >
                      <i class="tim-icons icon-simple-remove" />
                    </BaseButton>
                  </ElTooltip>
                </div>
              </template>
            </ElTableColumn>
          </ElTable>
        </div>
      </div>
    </Card>

    <Card>
      <template #header>
        <h4 class="card-title">Striped Table with Checkboxes</h4>
      </template>
      <div class="row table-full-width">
        <div class="col-sm-12">
          <ElTable :data="secondTableData">
            <ElTableColumn width="60" label="#" align="center">
              <template #default="{ row }">
                <div class="checkbox-cell">
                  <BaseCheckbox v-model="row.checked" />
                </div>
              </template>
            </ElTableColumn>

            <ElTableColumn prop="name" min-width="180" label="Product Name" />
            <ElTableColumn prop="type" min-width="180" label="Type" />

            <ElTableColumn label="Milestone" min-width="130">
              <template #default="{ row }">
                <BaseProgress
                  :value="row.progress"
                  :label="row.progressText"
                  value-position="right"
                />
              </template>
            </ElTableColumn>

            <ElTableColumn
              prop="quantity"
              min-width="80"
              align="center"
              label="QTY"
            />
            <ElTableColumn
              prop="price"
              min-width="120"
              align="right"
              label="Price"
            />
            <ElTableColumn
              prop="amount"
              min-width="120"
              align="right"
              label="Amount"
            />
          </ElTable>
        </div>
      </div>
    </Card>

    <div class="row">
      <div class="col-md-12">
        <Card>
          <template #header>
            <h4 class="card-title">Shopping Cart Table</h4>
            <br />
          </template>
          <ShoppingTable />
        </Card>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ElTable, ElTableColumn, ElTooltip } from "element-plus";
import "element-plus/es/components/table/style/css";
import "element-plus/es/components/table-column/style/css";
import "element-plus/es/components/tooltip/style/css";

import BaseButton from "@/components/BaseButton.vue";
import BaseCheckbox from "@/components/Inputs/BaseCheckbox.vue";
import BaseProgress from "@/components/BaseProgress.vue";
import RouteBreadCrumb from "@/components/Breadcrumb/RouteBreadCrumb.vue";
import ShoppingTable from "./ExtendedTables/ShoppingTable.vue";
import Card from "@/components/Cards/Card.vue";

const tableData = [
  {
    id: 1,
    img: "/img/tania.jpg",
    name: "Tania Mike",
    job: "Develop",
    progress: 25,
    since: 2013,
    salary: "€ 99,225",
  },
  {
    id: 2,
    img: "/img/robi.jpg",
    name: "John Doe",
    job: "CEO",
    progress: 77,
    since: 2012,
    salary: "€ 89,241",
  },
  {
    id: 3,
    img: "/img/lora.jpg",
    name: "Alexa Mike",
    job: "Design",
    progress: 41,
    since: 2010,
    salary: "€ 92,144",
  },
  {
    id: 4,
    img: "/img/jana.jpg",
    name: "Jana Monday",
    job: "Marketing",
    progress: 50,
    since: 2013,
    salary: "€ 49,990",
  },
  {
    id: 5,
    img: "/img/mike.jpg",
    name: "Paul Dickens",
    job: "Develop",
    progress: 100,
    since: 2015,
    salary: "€ 69,201",
  },
  {
    id: 6,
    img: "/img/emilyz.jpg",
    name: "Manuela Rico",
    job: "Manager",
    progress: 15,
    since: 2012,
    salary: "€ 99,201",
  },
];

const secondTableData = [
  {
    id: 1,
    name: "Moleskine Agenda",
    checked: true,
    type: "Office",
    progress: 25,
    progressText: "V1.2.0",
    quantity: 25,
    price: "€ 49",
    amount: "€ 1,225",
  },
  {
    id: 2,
    name: "Stabilo Pen",
    checked: false,
    type: "Office",
    progress: 45,
    progressText: "V1.4.0",
    quantity: 30,
    price: "€ 10",
    amount: "€ 300",
  },
  {
    id: 3,
    name: "A4 Paper Pack",
    checked: true,
    type: "Office",
    progress: 10,
    progressText: "V2.0.0",
    quantity: 10,
    price: "€ 10.99",
    amount: "€ 109",
  },
  {
    id: 4,
    name: "Apple iPad",
    checked: false,
    type: "Meeting",
    progress: 80,
    progressText: "V1.5.0",
    quantity: 10,
    price: "€ 499.00",
    amount: "€ 4,990",
  },
  {
    id: 5,
    name: "Apple iPhone",
    checked: true,
    type: "Communication",
    progress: 50,
    progressText: "V1.0.0",
    quantity: 10,
    price: "€ 599.00",
    amount: "€ 5,990",
  },
];
</script>

<style lang="scss">
.checkbox-cell {
  height: 40px;
  display: flex;
  justify-content: center;
  align-items: center;
}
</style>
