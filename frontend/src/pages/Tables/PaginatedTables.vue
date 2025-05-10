<template>
  <div class="content">
    <div class="col-md-8 ml-auto mr-auto">
      <h2 class="text-center">Paginated Tables</h2>
      <p class="text-center">
        With a selection of custom components & and Element UI components, you
        can built beautiful data tables. For more info check
        <a
          href="http://element.eleme.io/#/en-US/component/table"
          target="_blank"
          >Element UI Table</a
        >
      </p>
    </div>
    <div class="row mt-5">
      <div class="col-12">
        <card card-body-classes="table-full-width">
          <h4 slot="header" class="card-title">Paginated Tables</h4>
          <div>
            <div
              class="col-12 d-flex justify-content-center justify-content-sm-between flex-wrap"
            >
              <el-select
                class="select-primary mb-3 pagination-select"
                v-model="pagination.perPage"
                placeholder="Per page"
              >
                <el-option
                  class="select-primary"
                  v-for="item in pagination.perPageOptions"
                  :key="item"
                  :label="item"
                  :value="item"
                >
                </el-option>
              </el-select>

              <base-input>
                <el-input
                  type="search"
                  class="mb-3 search-input"
                  clearable
                  prefix-icon="el-icon-search"
                  placeholder="Search records"
                  v-model="searchQuery"
                  aria-controls="datatables"
                >
                </el-input>
              </base-input>
            </div>
            <el-table :data="queriedData">
              <el-table-column
                v-for="column in tableColumns"
                :key="column.label"
                :min-width="column.minWidth"
                :prop="column.prop"
                :label="column.label"
              >
              </el-table-column>
              <el-table-column :min-width="135" align="right" label="Actions">
                <div slot-scope="props">
                  <base-button
                    @click.native="handleLike(props.$index, props.row)"
                    class="like btn-link"
                    type="info"
                    size="sm"
                    icon
                  >
                    <i class="tim-icons icon-heart-2"></i>
                  </base-button>
                  <base-button
                    @click.native="handleEdit(props.$index, props.row)"
                    class="edit btn-link"
                    type="warning"
                    size="sm"
                    icon
                  >
                    <i class="tim-icons icon-pencil"></i>
                  </base-button>
                  <base-button
                    @click.native="handleDelete(props.$index, props.row)"
                    class="remove btn-link"
                    type="danger"
                    size="sm"
                    icon
                  >
                    <i class="tim-icons icon-simple-remove"></i>
                  </base-button>
                </div>
              </el-table-column>
            </el-table>
          </div>
          <div
            slot="footer"
            class="col-12 d-flex justify-content-center justify-content-sm-between flex-wrap"
          >
            <div class="">
              <p class="card-category">
                Showing {{ from + 1 }} to {{ to }} of {{ total }} entries
              </p>
            </div>
            <base-pagination
              class="pagination-no-border"
              v-model="pagination.currentPage"
              :per-page="pagination.perPage"
              :total="total"
            >
            </base-pagination>
          </div>
        </card>
      </div>
    </div>
  </div>
</template>
<script setup>
import { ref, computed, watch, onMounted } from "vue";
import {
  ElTable,
  ElTableColumn,
  ElSelect,
  ElOption,
  ElInput,
} from "element-plus";
import BasePagination from "@/components/BasePagination.vue";
import Card from "@/components/Cards/Card.vue";
import BaseButton from "@/components/BaseButton.vue";
import BaseInput from "@/components/Inputs/BaseInput.vue";
import users from "./users";
import Fuse from "fuse.js";
import { useNotifications } from "@/components/Notifications/NotificationPlugin";

// Table Columns
const tableColumns = [
  { prop: "name", label: "Name", minWidth: 200 },
  { prop: "email", label: "Email", minWidth: 250 },
  { prop: "age", label: "Age", minWidth: 100 },
  { prop: "salary", label: "Salary", minWidth: 120 },
];

// States
const tableData = ref(users);
const searchedData = ref([]);
const fuseSearch = ref(null);
const searchQuery = ref("");

// Pagination state
const pagination = ref({
  perPage: 5,
  currentPage: 1,
  perPageOptions: [5, 10, 25, 50],
});

const from = computed(() => {
  return pagination.value.perPage * (pagination.value.currentPage - 1);
});

const to = computed(() => {
  const high = from.value + pagination.value.perPage;
  return Math.min(high, total.value);
});

const total = computed(() => {
  return searchedData.value.length > 0
    ? searchedData.value.length
    : tableData.value.length;
});

const queriedData = computed(() => {
  const data =
    searchedData.value.length > 0 ? searchedData.value : tableData.value;
  return data.slice(from.value, to.value);
});

// Search setup
onMounted(() => {
  fuseSearch.value = new Fuse(tableData.value, {
    keys: ["name", "email"],
    threshold: 0.3,
  });
});

watch(searchQuery, (value) => {
  if (value.trim() === "") {
    searchedData.value = [];
  } else {
    searchedData.value = fuseSearch.value.search(value).map((r) => r.item);
  }
});

// Handlers
const { notify } = useNotifications();

function handleLike(index, row) {
  notify({
    type: 'success',
    title: 'Liked',
    message: `You liked ${row.name}`,
    icon: 'fas fa-heart',
  });
}

function handleEdit(index, row) {
  notify({
    type: 'info',
    title: 'Edit',
    message: `You want to edit ${row.name}`,
    icon: 'fas fa-pencil-alt',
  });
}

function handleDelete(index, row) {
  if (!confirm(`Are you sure you want to delete ${row.name}? This cannot be undone!`)) return;
  deleteRow(row);
  notify({
    type: 'success',
    title: 'Deleted',
    message: `You deleted ${row.name}`,
    icon: 'fas fa-trash',
  });
}

function deleteRow(row) {
  const index = tableData.value.findIndex((r) => r.id === row.id);
  if (index !== -1) tableData.value.splice(index, 1);
}
</script>

