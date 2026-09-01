<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getProducts } from '@/services/productService'
import type { Product } from '@/types/product'
import Navbar from '../../component/Navbar.vue'
import Sidebar from '../../component/Sidebar.vue'

const products = ref<Product[]>([])
const message = ref('')

const totalStockQuantity = computed(() =>
  products.value.reduce((total, product) => total + product.stockQuantity, 0),
)
const outOfStockCount = computed(
  () => products.value.filter((product) => product.stockQuantity === 0).length,
)
const recentProducts = computed(() =>
  [...products.value]
    .sort((first, second) => {
      const dateDifference = Date.parse(second.createdAt) - Date.parse(first.createdAt)

      return Number.isNaN(dateDifference) ? second.id - first.id : dateDifference
    })
    .slice(0, 5),
)

async function loadProducts() {
  try {
    const result = await getProducts({
      page: 1,
      pageSize: 20,
    })

    products.value = result.items
  } catch (error) {
    if (error instanceof Error) {
      message.value = error.message
    }
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
          <section class="div1" aria-labelledby="dashboard-title">
            <div>
              <p class="section-label">Quản trị hệ thống</p>
              <h2 id="dashboard-title">Tổng quan cửa hàng</h2>
              <span>Theo dõi nhanh hàng hóa và tình trạng sản phẩm hiện tại.</span>
            </div>
          </section>

          <section class="div2 stat-card">
            <div class="stat-heading">
              <span>Sản phẩm</span>
              <strong>{{ products.length }}</strong>
            </div>
            <h3>Đang hiển thị</h3>
            <p>Số sản phẩm được tải trong danh sách hiện tại.</p>
          </section>

          <section class="div3 stat-card">
            <div class="stat-heading stock-stat">
              <span>Số lượng</span>
              <strong>{{ totalStockQuantity.toLocaleString('vi-VN') }}</strong>
            </div>
            <h3>Tổng số lượng hàng</h3>
            <p>Tổng số lượng của các sản phẩm đang hiển thị.</p>
          </section>

          <section class="div4 stat-card">
            <div class="stat-heading warning-stat">
              <span>Cần chú ý</span>
              <strong>{{ outOfStockCount }}</strong>
            </div>
            <h3>Sản phẩm hết hàng</h3>
            <p>Số sản phẩm có số lượng hiện tại bằng 0.</p>
          </section>

          <section class="div5" aria-labelledby="recent-products-title">
            <div class="table-heading">
              <div>
                <h3 id="recent-products-title">Sản phẩm gần đây</h3>
                <p>Hiển thị tối đa 5 sản phẩm trong dữ liệu đã tải.</p>
              </div>

              <RouterLink class="view-all-link" to="/productPage">Xem tất cả</RouterLink>
            </div>

            <p v-if="message" class="dashboard-message" role="alert">
              {{ message }}
            </p>

            <div v-else class="table-scroll">
              <table class="product-table">
                <thead>
                  <tr>
                    <th scope="col">Mã sản phẩm</th>
                    <th scope="col">Tên sản phẩm</th>
                    <th scope="col">Giá</th>
                    <th scope="col">Số lượng</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="product in recentProducts" :key="product.id">
                    <td>{{ product.productCode }}</td>
                    <td>{{ product.name }}</td>
                    <td>{{ product.price.toLocaleString('vi-VN') }} đ</td>
                    <td>
                      <span
                        class="stock-badge"
                        :class="{ 'is-out-of-stock': product.stockQuantity === 0 }"
                      >
                        {{ product.stockQuantity }}
                      </span>
                    </td>
                  </tr>

                  <tr v-if="recentProducts.length === 0">
                    <td colspan="4" class="empty-products">Chưa có sản phẩm để hiển thị.</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

.body,
button,
input,
select,
textarea {
  font-family: 'Be Vietnam Pro', sans-serif;
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
  min-width: 0;
  padding: 10px;
}

/* GRID */
.parent {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
}

.div1,
.div2,
.div3,
.div4,
.div5 {
  padding: 20px;
  background-color: white;
  border-radius: 10px;
}

.div1 {
  grid-column: 1 / 4;
  min-height: 100px;
  display: flex;
  align-items: center;
}

.section-label {
  margin-bottom: 6px;
  color: #007aff;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.div1 h2 {
  margin-bottom: 8px;
}

.div1 span,
.stat-card p,
.table-heading p {
  color: #666;
  font-size: 14px;
  line-height: 1.6;
}

.div2,
.div3,
.div4 {
  min-height: 180px;
}

.div5 {
  grid-column: 1 / 4;
  min-height: 360px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.stat-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.stat-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 15px;
}

.stat-heading span {
  color: #666;
  font-size: 14px;
  font-weight: 500;
}

.stat-heading strong {
  min-width: 58px;
  padding: 8px 12px;
  color: #007aff;
  background-color: #edf5ff;
  border-radius: 8px;
  font-size: 22px;
  text-align: center;
}

.stock-stat strong {
  color: #18864b;
  background-color: #edf8f1;
}

.warning-stat strong {
  color: #c44f5a;
  background-color: #fff0f1;
}

.stat-card h3 {
  margin-bottom: 8px;
  font-size: 18px;
}

.table-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 15px;
}

.table-heading h3 {
  margin-bottom: 5px;
  font-size: 18px;
}

.view-all-link {
  flex: 0 0 auto;
  padding: 9px 14px;
  color: #007aff;
  border: 1px solid #007aff;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  text-decoration: none;
}

.view-all-link:hover {
  background-color: #edf5ff;
}

.dashboard-message {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  color: #a0444b;
  background-color: #fff4f5;
  border-radius: 8px;
  text-align: center;
}

.table-scroll {
  flex: 1;
  width: 100%;
  overflow: auto;
}

.product-table {
  width: 100%;
  min-width: 680px;
  border-collapse: collapse;
  font-size: 16px;
}

.product-table th,
.product-table td {
  padding: 12px;
  border-bottom: 1px solid #ddd;
  text-align: left;
}

.product-table th {
  position: sticky;
  top: 0;
  z-index: 1;
  background-color: #f5f5f5;
  font-weight: 600;
  white-space: nowrap;
}

.product-table tbody tr:hover {
  background-color: #f7f9fc;
}

.stock-badge {
  display: inline-flex;
  min-width: 42px;
  justify-content: center;
  padding: 6px 9px;
  color: #127550;
  background-color: #def7eb;
  border-radius: 999px;
  font-size: 13px;
  font-weight: 600;
}

.stock-badge.is-out-of-stock {
  color: #a0444b;
  background-color: #ffe8ea;
}

.empty-products {
  height: 180px;
  color: #777;
  text-align: center !important;
}

button:focus-visible,
input:focus-visible,
select:focus-visible,
a:focus-visible {
  outline: 3px solid rgb(0 122 255 / 25%);
  outline-offset: 2px;
}

@media (max-width: 768px) {
  .layout {
    display: block;
  }

  .layout :deep(.sidebar) {
    width: 100%;
    flex-direction: row;
    gap: 6px;
    padding: 10px;
    overflow-x: auto;
  }

  .layout :deep(.sidebar a) {
    flex: 0 0 auto;
    margin: 0;
    padding: 11px 14px;
    white-space: nowrap;
  }

  .layout :deep(.sidebar a:hover),
  .layout :deep(.sidebar a.router-link-active) {
    border-right: 0;
    border-bottom: 3px solid #00d4ea;
  }

  .content {
    padding: 8px;
  }

  .parent {
    grid-template-columns: 1fr;
    gap: 8px;
  }

  .div1,
  .div2,
  .div3,
  .div4,
  .div5 {
    grid-column: 1;
    min-height: 130px;
    padding: 16px;
  }

  .div1 {
    min-height: 90px;
  }

  .div5 {
    min-height: 380px;
  }

  .table-heading {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
