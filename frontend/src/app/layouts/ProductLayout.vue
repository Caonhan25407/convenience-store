<script setup lang="ts">
import { onMounted, ref } from 'vue'
import {
  createProduct,
  getProducts,
  updateProduct,
  deleteProduct,
} from '@/services/productService'
import type { Product, ProductRequest } from '@/types/product'
import Navbar from '../component/Navbar.vue'
import Sidebar from '../component/Sidebar.vue'

const products = ref<Product[]>([])
const message = ref('')
const loading = ref(false)
const editingId = ref<number | null>(null)

const form = ref<ProductRequest>({
  productCode: '',
  name: '',
  price: 0,
  stockQuantity: 0,
})

function resetForm() {
  editingId.value = null

  form.value = {
    productCode: '',
    name: '',
    price: 0,
    stockQuantity: 0,
  }
}

async function loadProducts() {
  try {
    products.value = await getProducts()
  } catch {
    message.value = 'Không tải được danh sách sản phẩm'
  }
}

async function handleSubmit() {
  try {
    loading.value = true

    if (editingId.value === null) {
      const product = await createProduct(form.value)
      message.value = `Đã thêm: ${product.name}`
    } else {
      const product = await updateProduct(
        editingId.value,
        form.value,
      )

      message.value = `Đã cập nhật: ${product.name}`
    }

    resetForm()
    await loadProducts()
  } catch (error) {
    message.value = error instanceof Error
      ? error.message
      : 'Thao tác thất bại'
  } finally {
    loading.value = false
  }
}

function handleEdit(product: Product) {
  editingId.value = product.id

  form.value = {
    productCode: product.productCode,
    name: product.name,
    price: product.price,
    stockQuantity: product.stockQuantity,
  }
}

async function handleDelete(id: number) {
  if (!confirm('Bạn có chắc muốn xóa sản phẩm này?')) {
    return
  }

  try {
    await deleteProduct(id)

    message.value = 'Đã xóa sản phẩm'

    await loadProducts()
  } catch {
    message.value = 'Xóa sản phẩm thất bại'
  }
}

onMounted(loadProducts)
</script>

<template>
  <div class="body">
    <Navbar />

    <div class="layout">
        <Sidebar />
        <main class="content">
            <div class="parent">

            <div class="div1">
                <h2>Quản lý sản phẩm</h2>

            <p v-if="message">
              {{ message }}
            </p>
          </div>

          <div class="div2">
            <h3>
              {{
                editingId === null
                  ? 'Thêm sản phẩm'
                  : 'Tùy chỉnh sản phẩm'
              }}
            </h3>

            <div class="form-scroll">
              <form @submit.prevent="handleSubmit">

                <div class="form-group">
                  <label>Mã sản phẩm</label>

                  <input
                    v-model="form.productCode"
                    type="text"
                    placeholder="SP001"
                    required
                  />
                </div>

                <div class="form-group">
                  <label>Tên sản phẩm</label>

                  <input
                    v-model="form.name"
                    type="text"
                    placeholder="Mì Hảo Hảo"
                    required
                  />
                </div>

                <div class="form-group">
                  <label>Giá</label>

                  <input
                    v-model.number="form.price"
                    type="number"
                    min="0"
                    required
                  />
                </div>

                <div class="form-group">
                  <label>Số lượng còn lại</label>

                  <input
                    v-model.number="form.stockQuantity"
                    type="number"
                    min="0"
                    required
                  />
                </div>

                <button
                  class="btn-add"
                  type="submit"
                  :disabled="loading"
                >
                  {{
                    loading
                      ? 'Đang xử lý...'
                      : editingId === null
                        ? 'Thêm sản phẩm'
                        : 'Cập nhật sản phẩm'
                  }}
                </button>

                <button
                  v-if="editingId !== null"
                  class="btn-cancel"
                  type="button"
                  @click="resetForm"
                >
                  Hủy
                </button>

              </form>
            </div>
          </div>

          <div class="div3">
            <h3>Tổng quan</h3>

            <p>
              Tổng sản phẩm:
              <strong>{{ products.length }}</strong>
            </p>
          </div>

          <div class="div4">
            <h3>Danh mục sản phẩm</h3>

            <div class="table-scroll">
              <table class="product-table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Mã sản phẩm</th>
                    <th>Tên sản phẩm</th>
                    <th>Giá</th>
                    <th>Số lượng</th>
                    <th>Thao tác</th>
                  </tr>
                </thead>

                <tbody>
                  <tr
                    v-for="product in products"
                    :key="product.id"
                  >
                    <td>
                      {{ product.id }}
                    </td>

                    <td>
                      {{ product.productCode }}
                    </td>

                    <td>
                      {{ product.name }}
                    </td>

                    <td>
                      {{ product.price.toLocaleString() }}
                    </td>

                    <td>
                      {{ product.stockQuantity }}
                    </td>

                    <td class="actions">
                      <button
                        class="btn-edit"
                        type="button"
                        @click="handleEdit(product)"
                      >
                        Sửa
                      </button>

                      <button
                        class="btn-delete"
                        type="button"
                        @click="handleDelete(product.id)"
                      >
                        Xóa
                      </button>
                    </td>
                  </tr>

                  <tr v-if="products.length === 0">
                    <td colspan="6">
                      No products
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Anton&family=Audiowide&family=Be+Vietnam+Pro:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&family=Kanit:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&family=Montserrat:ital,wght@0,100..900;1,100..900&display=swap');

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

.body {
  min-height: 100vh;
  background-color: #e7e7e7;
}   

/* LAYOUT */
.layout {
  display: flex;
  min-height: 90vh;
}

/* CONTENT */
.content {
  flex: 1;
  padding: 10px;
  
}

/* GRID */
.parent {
  display: grid;

  grid-template-columns: 1fr 1fr;

  gap: 10px;
  
}

.div1,
.div2,
.div3,
.div4 {
  background-color: white;
  border-radius: 10px;

  padding: 20px;
}

.div1 {
  grid-column: 1 / 3;
  height: 10vh;
}

.div2 {
  height: 35vh;

  display: flex;
  flex-direction: column;

  overflow: hidden;
}

.div2 h3 {
  flex-shrink: 0;
}

.form-scroll {
  flex: 1;

  overflow-y: auto;
  overflow-x: hidden;

  padding-right: 6px;
}

.form-scroll::-webkit-scrollbar {
  width: 6px;
}

.form-scroll::-webkit-scrollbar-track {
  background: transparent;
}

.form-scroll::-webkit-scrollbar-thumb {
  background-color: #c5c5c5;
  border-radius: 10px;
}

.form-scroll::-webkit-scrollbar-thumb:hover {
  background-color: #999;
}

.div3 {
  height: 35vh;
}

.div4 {
  grid-column: 1 / 3;
  height: 40vh;
}

.div1 h2,
.div2 h3,
.div3 h3,
.div4 h3 {
  font-family: "Be Vietnam Pro", sans-serif;
  margin-bottom: 15px;
}

.form-group {
  display: flex;
  flex-direction: column;
  margin-bottom: 15px;
}

.form-group label {
  font-family: "Be Vietnam Pro", sans-serif;
  font-size: 14px;
  margin-bottom: 5px;
}

.form-group input {
  padding: 10px 12px;

  border: 1px solid #ccc;
  border-radius: 6px;

  outline: none;

  font-family: "Be Vietnam Pro", sans-serif;
}

.form-group input:focus {
  border-color: #007AFF;
}

.btn-add {
  width: 100%;

  padding: 12px;

  border: none;
  border-radius: 6px;

  background-color: #007AFF;
  color: white;

  cursor: pointer;

  font-family: "Be Vietnam Pro", sans-serif;
  font-weight: 600;
}

.btn-add:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.product-table {
  width: 100%;

  border-collapse: collapse;

  font-family: "Be Vietnam Pro", sans-serif;
}

.product-table th,
.product-table td {
  padding: 12px;

  border-bottom: 1px solid #ddd;

  text-align: left;
}

.product-table th {
  background-color: #f5f5f5;
}

.product-table tbody tr:hover {
  background-color: #f7f9fc;
}
</style>