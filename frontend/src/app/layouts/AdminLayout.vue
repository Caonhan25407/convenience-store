<script setup lang="ts">
import { onMounted, ref } from 'vue'
import {
  createProduct,
  getProducts,
} from '@/services/productService'

import type {
  Product,
  ProductRequest,
} from '@/types/product'

const products = ref<Product[]>([])

const form = ref<ProductRequest>({
  name: '',
  price: 0,
  stockQuantity: 0,
})

const loading = ref(false)
const message = ref('')

async function loadProducts() {
  try {
    products.value = await getProducts()
  } catch (error) {
    if (error instanceof Error) {
      message.value = error.message
    }
  }
}

async function handleSubmit() {
  try {
    loading.value = true
    message.value = ''

    const product = await createProduct(form.value)

    message.value = `Đã thêm: ${product.name}`

    form.value = {
      name: '',
      price: 0,
      stockQuantity: 0,
    }

    await loadProducts()
  } catch (error) {
    if (error instanceof Error) {
      message.value = error.message
    }
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadProducts()
})
</script>

<template>
  <div class="body">

    <div class="navbar">
      <img src="/logo.png" alt="logo">

      <a href="#">Home</a>
      <a href="#">Search</a>
      <a href="#">Contact</a>
      <a href="#">Login</a>
    </div>

    <div class="layout">

      <aside class="sidebar">
        <h3>Admin</h3>

        <a href="#">Dashboard</a>
        <a href="#">Products</a>
        <a href="#">Categories</a>
        <a href="#">Orders</a>
        <a href="#">Inventory</a>
        <a href="#">Users</a>
      </aside>

      <main class="content">
        <div class="parent">

          <div class="div1">
            <h2>Product Management</h2>

            <p v-if="message">
              {{ message }}
            </p>
          </div>

          <div class="div2">
            <h3>Add Product</h3>

            <form @submit.prevent="handleSubmit">

              <div class="form-group">
                <label>Product name</label>

                <input
                  v-model="form.name"
                  type="text"
                  required
                />
              </div>

              <div class="form-group">
                <label>Price</label>

                <input
                  v-model.number="form.price"
                  type="number"
                  min="0"
                  required
                />
              </div>

              <div class="form-group">
                <label>Stock quantity</label>

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
                    ? 'Adding...'
                    : 'Add Product'
                }}
              </button>

            </form>
          </div>

          <div class="div3">
            <h3>Overview</h3>

            <p>
              Total products:
              <strong>{{ products.length }}</strong>
            </p>
          </div>

          <div class="div4">
            <h3>Product List</h3>

            <table class="product-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Name</th>
                  <th>Price</th>
                  <th>Stock</th>
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
                    {{ product.name }}
                  </td>

                  <td>
                    {{ product.price.toLocaleString() }} đ
                  </td>

                  <td>
                    {{ product.stockQuantity }}
                  </td>
                </tr>

                <tr v-if="products.length === 0">
                  <td colspan="4">
                    No products
                  </td>
                </tr>
              </tbody>
            </table>
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

.navbar {
  display: flex;
  align-items: center;
  gap: 10px;
    
  width: 100%;
  height: 10vh;

  background-color:#e7e7e7;
  font-family: "Be Vietnam Pro", sans-serif;
  font-weight: 500;
  font-style: normal;
}

.navbar img {
  width: 120px;
  height: auto;
}

.navbar a {
  padding: 10px;

  color: #007AFF;
  text-decoration: none;

  font-size: 1.2rem;
  font-weight: 800;
}

.navbar a:hover {
  background-color: #e7e7e7;
}

/* LAYOUT */
.layout {
  display: flex;
  min-height: 90vh;
}

/* SIDEBAR */
.sidebar {
  width: 220px;

  display: flex;
  flex-direction: column;

  background-color: white;

  padding: 20px 10px;
}

.sidebar h3 {
  color: white;
  margin-bottom: 25px;
  padding-left: 15px;
}

.sidebar a {
  color: #007AFF;
  text-decoration: none;

  padding: 15px;

  border-radius: 8px;

  margin-bottom: 5px;
  font-family: "Be Vietnam Pro", sans-serif;
  font-weight: 500;
  font-style: normal;
}

.sidebar a:hover {
  background-color: #007AFF;
  
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
  height: 100px;
}

.div2 {
  height: 200px;
}

.div3 {
  height: 200px;
}

.div4 {
  grid-column: 1 / 3;
  height: 300px;
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