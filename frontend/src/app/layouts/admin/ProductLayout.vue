<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import {
  createProduct,
  getProducts,
  updateProduct,
  deleteProduct,
  importProducts,
} from '@/services/productService'
import type { Product, ProductRequest } from '@/types/product'
import Navbar from '../../component/Navbar.vue'
import Sidebar from '../../component/Sidebar.vue'

const products = ref<Product[]>([])
const message = ref('')
const loading = ref(false)
const searchQuery = ref('')
const stockFilter = ref('all')
const isFilterOpen = ref(false)
const page = ref(1)
const pageSize = 20
const totalCount = ref(0)
const totalPages = ref(0)
let searchDebounceTimer: ReturnType<typeof setTimeout> | undefined

const filterForm = ref({
  minPrice: '',
  maxPrice: '',
  minStock: '',
  maxStock: '',
})

const appliedFilters = ref({
  minPrice: '',
  maxPrice: '',
  minStock: '',
  maxStock: '',
})
const appliedStockFilter = ref('all')

/*
 * null: đang thêm sản phẩm
 * number: đang sửa sản phẩm có id tương ứng
 */
const editingId = ref<number | null>(null)

/*
 * Điều khiển việc đóng/mở form lớn
 */
const isFormOpen = ref(false)

/*
 * Điều khiển việc đóng/mở modal import
 */
const isImportOpen = ref(false)
const importFile = ref<File | null>(null)
const importMessage = ref('')
const importStatus = ref<'idle' | 'processing' | 'success' | 'error'>('idle')

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

/*
 * Mở form để thêm sản phẩm
 */
function openCreateForm() {
  resetForm()
  message.value = ''
  isFormOpen.value = true
}

// Mở form import
function openImportForm() {
  importFile.value = null
  importMessage.value = ''
  importStatus.value = 'idle'
  isImportOpen.value = true
}

/*
 * Đóng form import
 */
function closeImportForm() {
  isImportOpen.value = false
  importFile.value = null
  importMessage.value = ''
  importStatus.value = 'idle'
}

/*
 * Đóng form
 */
function closeForm() {
  isFormOpen.value = false
  resetForm()
}

function openFilterForm() {
  isFilterOpen.value = true
}

function closeFilterForm() {
  isFilterOpen.value = false
}

async function applyFilters() {
  appliedFilters.value = { ...filterForm.value }
  appliedStockFilter.value = stockFilter.value
  page.value = 1
  closeFilterForm()
  await loadProducts()
}

async function resetFilters() {
  filterForm.value = {
    minPrice: '',
    maxPrice: '',
    minStock: '',
    maxStock: '',
  }
  searchQuery.value = ''
  stockFilter.value = 'all'
  appliedFilters.value = { ...filterForm.value }
  appliedStockFilter.value = 'all'
  page.value = 1
  closeFilterForm()
  await loadProducts()
}

async function loadProducts(targetPage = page.value) {
  try {
    loading.value = true
    page.value = targetPage

    const result = await getProducts({
      page: page.value,
      pageSize,
      search: searchQuery.value.trim(),
      ...appliedFilters.value,
      stockStatus: appliedStockFilter.value,
    })

    products.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch {
    message.value = 'Không tải được danh sách sản phẩm'
  } finally {
    loading.value = false
  }
}

function changePage(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value || nextPage === page.value) {
    return
  }

  void loadProducts(nextPage)
}

watch(searchQuery, () => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer)
  }

  searchDebounceTimer = setTimeout(() => {
    page.value = 1
    void loadProducts()
  }, 300)
})

async function handleSubmit() {
  try {
    loading.value = true

    if (editingId.value === null) {
      const product = await createProduct(form.value)

      message.value = `Đã thêm: ${product.name}`
    } else {
      const product = await updateProduct(editingId.value, form.value)

      message.value = `Đã cập nhật: ${product.name}`
    }

    await loadProducts()

    /*
     * Thêm hoặc cập nhật thành công thì đóng form
     */
    closeForm()
  } catch (error) {
    message.value = error instanceof Error ? error.message : 'Thao tác thất bại'
  } finally {
    loading.value = false
  }
}

/*
 * Nhấn nút Sửa:
 * 1. Đưa dữ liệu sản phẩm vào form
 * 2. Mở popup
 */
function handleEdit(product: Product) {
  editingId.value = product.id
  message.value = ''

  form.value = {
    productCode: product.productCode,
    name: product.name,
    price: product.price,
    stockQuantity: product.stockQuantity,
  }

  isFormOpen.value = true
}

async function handleDelete(id: number) {
  const isConfirmed = confirm('Bạn có chắc muốn xóa sản phẩm này?')

  if (!isConfirmed) {
    return
  }

  try {
    await deleteProduct(id)

    message.value = 'Đã xóa sản phẩm'

    await loadProducts()
  } catch (error) {
    message.value = error instanceof Error ? error.message : 'Xóa sản phẩm thất bại'
  }
}

async function handleImport() {
  if (!importFile.value) {
    importMessage.value = 'Vui lòng chọn file CSV cần import'
    importStatus.value = 'error'
    return
  }

  try {
    loading.value = true
    importMessage.value = 'Đang import sản phẩm...'
    importStatus.value = 'processing'

    const result = await importProducts(importFile.value)

    importMessage.value =
      result.failedCount > 0
        ? `${result.message}, bỏ qua ${result.failedCount} mã đã tồn tại`
        : result.message
    importStatus.value = result.successCount > 0 ? 'success' : 'error'
    await loadProducts()
  } catch (error) {
    importMessage.value = error instanceof Error ? error.message : 'Import sản phẩm thất bại'
    importStatus.value = 'error'
  } finally {
    loading.value = false
  }
}

function handleImportFileChange(event: Event) {
  importFile.value = (event.target as HTMLInputElement).files?.[0] ?? null
  importMessage.value = ''
  importStatus.value = 'idle'
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
          <!-- TIÊU ĐỀ -->
          <div class="div1">
            <div>
              <h2>Quản lý sản phẩm</h2>
            </div>
          </div>

          <!-- NÚT MỞ FORM -->
          <div class="div2">
            <h3>Thao tác sản phẩm</h3>

            <button class="btn-open-form" type="button" @click="openCreateForm">
              <span class="plus-icon">+</span>
              Thêm sản phẩm
            </button>

            <button class="btn-import-form" type="button" @click="openImportForm">
              <span class="plus-icon">+</span>
              Import CSV
            </button>

            <p v-if="message" class="message">
              {{ message }}
            </p>
          </div>

          <!-- TỔNG QUAN -->
          <div class="div3">
            <div class="search-filter-heading">
              <div class="search-box">
                <label for="product-search">Tìm kiếm sản phẩm</label>
                <input
                  id="product-search"
                  v-model="searchQuery"
                  type="search"
                  placeholder="Mã hoặc tên sản phẩm"
                />
              </div>

              <button class="btn-open-filter" type="button" @click="openFilterForm">Bộ lọc</button>
            </div>
          </div>

          <!-- DANH SÁCH SẢN PHẨM -->
          <div class="div4">
            <div class="table-heading">
              <h3>Danh sách sản phẩm</h3>

              <div class="total-product">
                <span>Tổng sản phẩm</span>

                <strong>
                  {{ totalCount }}
                </strong>
              </div>
            </div>

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
                  <tr v-for="product in products" :key="product.id">
                    <td>
                      {{ product.id }}
                    </td>

                    <td>
                      {{ product.productCode }}
                    </td>

                    <td>
                      {{ product.name }}
                    </td>

                    <td>{{ product.price.toLocaleString('vi-VN') }} đ</td>

                    <td>
                      {{ product.stockQuantity }}
                    </td>

                    <td class="actions">
                      <button class="btn-edit" type="button" @click="handleEdit(product)">
                        Sửa
                      </button>

                      <button class="btn-delete" type="button" @click="handleDelete(product.id)">
                        Xóa
                      </button>
                    </td>
                  </tr>

                  <tr v-if="products.length === 0">
                    <td colspan="6" class="empty-product">Không tìm thấy sản phẩm phù hợp</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div v-if="totalPages > 1" class="pagination">
              <button type="button" :disabled="page === 1 || loading" @click="changePage(page - 1)">
                Trước
              </button>

              <span>Trang {{ page }} / {{ totalPages }}</span>

              <button
                type="button"
                :disabled="page === totalPages || loading"
                @click="changePage(page + 1)"
              >
                Sau
              </button>
            </div>
          </div>
        </div>
      </main>
    </div>

    <Teleport to="body">
      <div v-if="isFilterOpen" class="modal-overlay" @click.self="closeFilterForm">
        <section class="product-modal" role="dialog" aria-modal="true">
          <div class="modal-header">
            <div>
              <p class="modal-label">DANH SÁCH SẢN PHẨM</p>
              <h2>Bộ lọc sản phẩm</h2>
            </div>

            <button
              class="btn-close"
              type="button"
              aria-label="Đóng bộ lọc"
              @click="closeFilterForm"
            >
              ×
            </button>
          </div>

          <form class="modal-form" @submit.prevent="applyFilters">
            <div class="modal-grid">
              <div class="form-group filter-wide">
                <label for="filter-stock-status">Tình trạng</label>
                <select id="filter-stock-status" v-model="stockFilter">
                  <option value="all">Tất cả sản phẩm</option>
                  <option value="in-stock">Còn hàng</option>
                  <option value="out-of-stock">Hết hàng</option>
                </select>
              </div>

              <div class="form-group">
                <label for="filter-min-price">Giá từ</label>
                <input
                  id="filter-min-price"
                  v-model="filterForm.minPrice"
                  type="number"
                  min="0"
                  step="100"
                  placeholder="Giá tối thiểu"
                />
              </div>

              <div class="form-group">
                <label for="filter-max-price">Giá đến</label>
                <input
                  id="filter-max-price"
                  v-model="filterForm.maxPrice"
                  type="number"
                  min="0"
                  step="100"
                  placeholder="Giá tối đa"
                />
              </div>

              <div class="form-group">
                <label for="filter-min-stock">Số lượng từ</label>
                <input
                  id="filter-min-stock"
                  v-model="filterForm.minStock"
                  type="number"
                  min="0"
                  step="1"
                  placeholder="Số lượng tối thiểu"
                />
              </div>

              <div class="form-group">
                <label for="filter-max-stock">Số lượng đến</label>
                <input
                  id="filter-max-stock"
                  v-model="filterForm.maxStock"
                  type="number"
                  min="0"
                  step="1"
                  placeholder="Số lượng tối đa"
                />
              </div>
            </div>

            <div class="modal-actions">
              <button class="btn-cancel" type="button" @click="resetFilters">Xóa bộ lọc</button>

              <button class="btn-save" type="submit">Áp dụng</button>
            </div>
          </form>
        </section>
      </div>
    </Teleport>

    <!--
      POPUP FORM

      Teleport giúp popup được đưa ra ngoài component
      và hiển thị phủ toàn màn hình.
    -->
    <Teleport to="body">
      <div v-if="isFormOpen" class="modal-overlay" @click.self="closeForm">
        <section class="product-modal" role="dialog" aria-modal="true">
          <!-- HEADER POPUP -->
          <div class="modal-header">
            <div>
              <p class="modal-label">QUẢN LÝ SẢN PHẨM</p>

              <h2>
                {{ editingId === null ? 'Thêm sản phẩm mới' : 'Chỉnh sửa sản phẩm' }}
              </h2>
            </div>

            <button class="btn-close" type="button" aria-label="Đóng form" @click="closeForm">
              ×
            </button>
          </div>

          <!-- FORM -->
          <form class="modal-form" @submit.prevent="handleSubmit">
            <div class="modal-grid">
              <div class="form-group">
                <label for="productCode"> Mã sản phẩm </label>

                <input
                  id="productCode"
                  v-model.trim="form.productCode"
                  type="text"
                  placeholder="Ví dụ: SP001"
                  autocomplete="off"
                  required
                />
              </div>

              <div class="form-group">
                <label for="productName"> Tên sản phẩm </label>

                <input
                  id="productName"
                  v-model.trim="form.name"
                  type="text"
                  placeholder="Ví dụ: Mì Hảo Hảo"
                  autocomplete="off"
                  required
                />
              </div>

              <div class="form-group">
                <label for="price"> Giá sản phẩm </label>

                <input
                  id="price"
                  v-model.number="form.price"
                  type="number"
                  min="0"
                  step="100"
                  placeholder="Ví dụ: 5000"
                  required
                />
              </div>

              <div class="form-group">
                <label for="stockQuantity"> Số lượng còn lại </label>

                <input
                  id="stockQuantity"
                  v-model.number="form.stockQuantity"
                  type="number"
                  min="0"
                  step="1"
                  placeholder="Ví dụ: 100"
                  required
                />
              </div>
            </div>

            <!-- NÚT CUỐI FORM -->
            <div class="modal-actions">
              <button class="btn-cancel" type="button" :disabled="loading" @click="closeForm">
                Hủy
              </button>

              <button class="btn-save" type="submit" :disabled="loading">
                {{
                  loading ? 'Đang xử lý...' : editingId === null ? 'Thêm sản phẩm' : 'Lưu thay đổi'
                }}
              </button>
            </div>
          </form>
        </section>
      </div>
    </Teleport>

    <!--
      IMPORT MODAL
    -->
    <Teleport to="body">
      <div v-if="isImportOpen" class="modal-overlay" @click.self="closeImportForm">
        <section class="product-modal" role="dialog" aria-modal="true">
          <!-- HEADER MODAL -->
          <div class="modal-header">
            <div>
              <p class="modal-label">QUẢN LÝ SẢN PHẨM</p>

              <h2>Import sản phẩm từ CSV</h2>
            </div>

            <button
              class="btn-close"
              type="button"
              aria-label="Đóng form import"
              @click="closeImportForm"
            >
              ×
            </button>
          </div>

          <!-- IMPORT FORM -->
          <form class="modal-form" @submit.prevent="handleImport">
            <div class="modal-grid">
              <div class="form-group import-file-group">
                <label for="importFile"> Chọn file CSV </label>

                <input
                  id="importFile"
                  type="file"
                  accept=".csv,text/csv"
                  @change="handleImportFileChange"
                  required
                />

                <p class="import-hint">
                  File CSV phải có các cột: Mã sản phẩm, Tên sản phẩm, Giá, Số lượng. Sản phẩm hợp
                  lệ sẽ được thêm vào database.
                </p>
              </div>
            </div>

            <!-- IMPORT MESSAGE -->
            <div v-if="importMessage" :class="['import-message-container', importStatus]">
              <p
                :class="['import-message', importStatus]"
                :role="importStatus === 'error' ? 'alert' : 'status'"
                aria-live="polite"
              >
                {{ importMessage }}
              </p>
            </div>

            <!-- FORM ACTIONS -->
            <div class="modal-actions">
              <button class="btn-cancel" type="button" :disabled="loading" @click="closeImportForm">
                Đóng
              </button>

              <button class="btn-save" type="submit" :disabled="loading || !importFile">
                {{ loading ? 'Đang import...' : 'Import' }}
              </button>
            </div>
          </form>
        </section>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

button,
input,
select {
  font-family: 'Be Vietnam Pro', sans-serif;
}

button {
  transition:
    background-color 0.2s,
    border-color 0.2s,
    transform 0.2s;
}

.body {
  min-height: 100vh;
  background-color: #e7e7e7;
  font-family: 'Be Vietnam Pro', sans-serif;
}

/* =========================
   LAYOUT
========================= */

.layout {
  display: flex;
  min-height: 90vh;
}

.content {
  flex: 1;
  min-width: 0;
  padding: 10px;
}

/* =========================
   GRID
========================= */

.parent {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
}

.div1,
.div2,
.div3,
.div4 {
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

.div2,
.div3 {
  min-height: 150px;
}

.div2 {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
}

.div3 {
  grid-column: 2 / 4;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.btn-open-filter {
  width: fit-content;
  padding: 10px 16px;
  margin-right: 15%;
  border: 1px solid #007aff;
  border-radius: 8px;
  background-color: white;
  color: #007aff;
  font-weight: 600;
  cursor: pointer;
}

.btn-open-filter:hover {
  background-color: #edf5ff;
}

.search-filter-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20px;
  width: 100%;
}

.search-box {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 6px;
  max-width: 80%;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.search-box label {
  font-size: 14px;
  font-weight: 500;
}

.search-box input {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #ccc;
  border-radius: 6px;
  outline: none;
  font-size: 16px;
}

.search-box input:focus {
  border-color: #007aff;
}

.filter-controls {
  display: grid;
  grid-template-columns: auto minmax(160px, 1fr);
  align-items: center;
  gap: 10px 14px;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.filter-controls label {
  font-size: 14px;
  font-weight: 500;
}

.filter-controls input,
.filter-controls select {
  width: 100%;
  min-width: 0;
  padding: 10px 12px;
  border: 1px solid #ccc;
  border-radius: 6px;
  background-color: white;
  outline: none;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 16px;
}

.filter-controls input:focus,
.filter-controls select:focus {
  border-color: #007aff;
}

.div4 {
  grid-column: 1 / 4;

  height: 55vh;
  min-height: 300px;

  display: flex;
  flex-direction: column;

  overflow: hidden;
}

.div1 h2,
.div2 h3,
.div3 h3,
.div4 h3 {
  margin-bottom: 15px;

  font-family: 'Be Vietnam Pro', sans-serif;
}

.table-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 15px;
}

.table-heading h3 {
  margin-bottom: 0;
}

.message {
  color: #007aff;

  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 1rem;
  margin-top: 0.6rem;
}

/* =========================
   NÚT MỞ FORM
========================= */

.btn-open-form {
  width: auto;

  display: inline-flex;
  align-items: center;
  gap: 8px;

  padding: 10px 16px;

  border: none;
  border-radius: 8px;

  background-color: #007aff;
  color: white;

  font-weight: 600;

  cursor: pointer;
}

.btn-open-form:hover {
  background-color: #0062cc;
  transform: translateY(-1px);
}

.plus-icon {
  font-size: 20px;
  line-height: 1;
}

/* =========================
   TỔNG QUAN
========================= */

.total-product {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-width: 180px;

  font-family: 'Be Vietnam Pro', sans-serif;
}

.total-product strong {
  min-width: 55px;

  padding: 8px 12px;

  border-radius: 8px;

  background-color: #edf5ff;
  color: #007aff;

  text-align: center;
  font-size: 20px;
}

/* =========================
   BẢNG SẢN PHẨM
========================= */

.table-scroll {
  flex: 1;
  width: 100%;

  overflow: auto;
}

.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding-top: 16px;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.pagination button {
  padding: 8px 14px;
  border: 1px solid #007aff;
  border-radius: 6px;
  background-color: white;
  color: #007aff;
  cursor: pointer;
}

.pagination button:hover:not(:disabled) {
  background-color: #edf5ff;
}

.pagination button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.product-table {
  width: 100%;
  min-width: 800px;

  border-collapse: collapse;

  font-family: 'Be Vietnam Pro', sans-serif;
}

.product-table th,
.product-table td {
  padding: 12px;

  border-bottom: 1px solid #ddd;

  text-align: left;
  vertical-align: middle;
}

.product-table th {
  position: sticky;
  top: 0;
  z-index: 1;

  background-color: #f5f5f5;
}

.product-table tbody tr:hover {
  background-color: #f7f9fc;
}

.actions {
  display: flex;
  gap: 8px;

  white-space: nowrap;
}

.btn-edit,
.btn-delete {
  padding: 7px 13px;

  border-radius: 6px;

  cursor: pointer;
}

.btn-edit {
  border: 1px solid #007aff;

  background-color: white;
  color: #007aff;
}

.btn-edit:hover {
  background-color: #007aff;
  color: white;
}

.btn-delete {
  border: 1px solid #dc3545;

  background-color: white;
  color: #dc3545;
}

.btn-delete:hover {
  background-color: #dc3545;
  color: white;
}

.empty-product {
  padding: 30px !important;

  color: #777;

  text-align: center !important;
}

/* =========================
   POPUP
========================= */

.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;

  display: flex;
  align-items: center;
  justify-content: center;

  padding: 24px;

  background-color: rgb(0 0 0 / 50%);
  backdrop-filter: blur(3px);
}

.product-modal {
  width: min(760px, 100%);
  max-height: 90vh;

  display: flex;
  flex-direction: column;

  overflow: hidden;

  background-color: white;
  border-radius: 16px;
  font-family: 'Be Vietnam Pro', sans-serif;

  box-shadow: 0 20px 60px rgb(0 0 0 / 25%);
}

.modal-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;

  padding: 24px;

  border-bottom: 1px solid #e5e5e5;
}

.modal-header h2 {
  margin-top: 5px;

  color: #222;

  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 24px;
}

.modal-label {
  color: #007aff;

  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
}

.btn-close {
  width: 38px;
  height: 38px;

  flex-shrink: 0;

  border: none;
  border-radius: 50%;

  background-color: #f1f1f1;
  color: #555;

  font-size: 25px;
  line-height: 1;

  cursor: pointer;
}

.btn-close:hover {
  background-color: #e1e1e1;
  color: #111;
}

/* =========================
   FORM TRONG POPUP
========================= */

.modal-form {
  padding: 24px;

  overflow-y: auto;
}

.modal-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 18px;
}

.filter-wide {
  grid-column: 1 / -1;
}

.form-group {
  display: flex;
  flex-direction: column;
}

.form-group label {
  margin-bottom: 7px;

  color: #333;

  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 14px;
  font-weight: 600;
}

.form-group input,
.form-group select {
  width: 100%;

  padding: 12px 14px;

  border: 1px solid #ccc;
  border-radius: 8px;
  background-color: white;

  outline: none;

  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 16px;
}

.form-group input:focus,
.form-group select:focus {
  border-color: #007aff;

  box-shadow: 0 0 0 3px rgb(0 122 255 / 12%);
}

/* =========================
   NÚT FORM
========================= */

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;

  margin-top: 25px;
  padding-top: 20px;

  border-top: 1px solid #eee;
}

.btn-cancel,
.btn-save {
  min-width: 120px;

  padding: 11px 18px;

  border-radius: 8px;

  font-weight: 600;

  cursor: pointer;
}

.btn-cancel {
  border: 1px solid #ccc;

  background-color: white;
  color: #555;
}

.btn-cancel:hover {
  background-color: #f3f3f3;
}

.btn-save {
  border: 1px solid #007aff;

  background-color: #007aff;
  color: white;
}

.btn-save:hover {
  background-color: #0062cc;
}

.btn-save:disabled,
.btn-cancel:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* =========================
   IMPORT FORM
========================= */

.import-file-group input[type='file'] {
  width: 100%;
  padding: 8px 12px;
  border: 2px dashed #007aff;
  border-radius: 6px;
  background-color: #f9f9f9;
  cursor: pointer;
}

.import-file-group input[type='file']::-webkit-file-upload-button {
  padding: 6px 12px;
  margin-right: 8px;
  border: none;
  border-radius: 4px;
  background-color: #007aff;
  color: white;
  font-weight: 600;
  cursor: pointer;
}

.import-hint {
  font-size: 12px;
  color: #666;
  margin-top: 6px;
  font-style: italic;
}

.import-message-container {
  margin-top: 15px;
  padding: 12px;
  background-color: #f5f5f5;
  border-left: 3px solid #007aff;
  border-radius: 6px;
}

.import-message-container.success {
  background-color: #edf8f0;
  border-left-color: #28a745;
}

.import-message-container.error {
  background-color: #fff1f2;
  border-left-color: #dc3545;
}

.import-message {
  margin: 0;
  font-weight: 500;
  font-size: 14px;
}

.import-message.success {
  color: #28a745;
}

.import-message.error {
  color: #dc3545;
}

.import-message.processing {
  color: #0062cc;
}

.import-results {
  margin-top: 12px;
}

.results-summary {
  display: flex;
  gap: 16px;
  margin-bottom: 12px;
  font-weight: 500;
}

.result-item {
  font-size: 13px;
}

.result-item.success-count {
  color: #28a745;
}

.result-item.failed-count {
  color: #dc3545;
}

.error-section {
  margin-top: 10px;
  padding: 8px;
  background-color: white;
  border-left: 3px solid #dc3545;
  border-radius: 4px;
}

.error-section h4 {
  margin: 0 0 8px 0;
  font-size: 13px;
  color: #333;
}

.error-section ul {
  margin: 0;
  padding-left: 20px;
  font-size: 12px;
  color: #666;
  max-height: 100px;
  overflow-y: auto;
}

.error-section li {
  margin-bottom: 4px;
}

.btn-import-form {
  width: 40%;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  margin-top: 5px;

  border: none;
  border-radius: 8px;
  background-color: #28a745;
  color: white;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-import-form:hover {
  background-color: #218838;
  transform: translateY(-1px);
}

@media (max-width: 900px) {
  .search-filter-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .search-box {
    max-width: 100%;
  }

  .btn-open-filter {
    margin-right: 0;
  }
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
  .div4 {
    grid-column: 1;
    padding: 16px;
  }

  .div1 {
    min-height: 90px;
  }

  .div2,
  .div3 {
    min-height: 130px;
  }

  .div4 {
    height: 60vh;
  }

  .modal-grid {
    grid-template-columns: 1fr;
  }

  .modal-overlay {
    padding: 12px;
  }

  .modal-header,
  .modal-form {
    padding: 18px;
  }
}
</style>
