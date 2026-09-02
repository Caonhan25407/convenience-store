<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { confirmOrder, getOrders } from '@/services/orderService'
import type { AdminOrder } from '@/types/order'
import Navbar from '../../component/Navbar.vue'
import Sidebar from '../../component/Sidebar.vue'

const orders = ref<AdminOrder[]>([])
const loading = ref(false)
const errorMessage = ref('')
const searchQuery = ref('')
const statusFilter = ref('all')
const page = ref(1)
const pageSize = 15
const totalCount = ref(0)
const totalPages = ref(0)
const expandedOrderIds = ref(new Set<number>())
const confirmingOrderIds = ref(new Set<number>())
const actionMessage = ref('')
const actionError = ref('')

let searchTimer: ReturnType<typeof setTimeout> | undefined
let latestRequest = 0

const statusOptions = [
  { value: 'all', label: 'Tất cả trạng thái' },
  { value: 'PENDING', label: 'Chờ xác nhận' },
  { value: 'CONFIRMED', label: 'Đã xác nhận' },
  { value: 'COMPLETED', label: 'Hoàn thành' },
  { value: 'CANCELLED', label: 'Đã hủy' },
]

async function loadOrders(targetPage = page.value) {
  const requestId = ++latestRequest

  try {
    loading.value = true
    errorMessage.value = ''
    page.value = targetPage

    const result = await getOrders({
      page: targetPage,
      pageSize,
      search: searchQuery.value,
      status: statusFilter.value,
    })

    if (requestId !== latestRequest) {
      return
    }

    const lastAvailablePage = Math.max(1, result.totalPages)

    if (targetPage > lastAvailablePage) {
      await loadOrders(lastAvailablePage)
      return
    }

    orders.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
    expandedOrderIds.value = new Set()
  } catch (error) {
    if (requestId !== latestRequest) {
      return
    }

    orders.value = []
    totalCount.value = 0
    totalPages.value = 0
    errorMessage.value = error instanceof Error ? error.message : 'Không thể tải danh sách đơn hàng'
  } finally {
    if (requestId === latestRequest) {
      loading.value = false
    }
  }
}

function changePage(nextPage: number) {
  if (loading.value || nextPage < 1 || nextPage > totalPages.value || nextPage === page.value) {
    return
  }

  void loadOrders(nextPage)
}

function toggleOrder(orderId: number) {
  const nextExpandedIds = new Set(expandedOrderIds.value)

  if (nextExpandedIds.has(orderId)) {
    nextExpandedIds.delete(orderId)
  } else {
    nextExpandedIds.add(orderId)
  }

  expandedOrderIds.value = nextExpandedIds
}

function isExpanded(orderId: number) {
  return expandedOrderIds.value.has(orderId)
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatDate(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

function statusLabel(status: string) {
  const labels: Record<string, string> = {
    PENDING: 'Chờ xác nhận',
    CONFIRMED: 'Đã xác nhận',
    COMPLETED: 'Hoàn thành',
    CANCELLED: 'Đã hủy',
  }

  return labels[status] ?? status
}

function statusClass(status: string) {
  return `status-${status.toLowerCase()}`
}

function isConfirming(orderId: number) {
  return confirmingOrderIds.value.has(orderId)
}

function setConfirming(orderId: number, confirming: boolean) {
  const nextIds = new Set(confirmingOrderIds.value)

  if (confirming) {
    nextIds.add(orderId)
  } else {
    nextIds.delete(orderId)
  }

  confirmingOrderIds.value = nextIds
}

async function handleConfirmOrder(order: AdminOrder) {
  if (order.status !== 'PENDING' || isConfirming(order.id)) {
    return
  }

  actionMessage.value = ''
  actionError.value = ''
  setConfirming(order.id, true)

  try {
    const result = await confirmOrder(order.id)
    actionMessage.value = result.message
    await loadOrders(page.value)
  } catch (error) {
    actionError.value = error instanceof Error ? error.message : 'Không thể xác nhận đơn hàng'
  } finally {
    setConfirming(order.id, false)
  }
}

watch(searchQuery, () => {
  if (searchTimer) {
    clearTimeout(searchTimer)
  }

  searchTimer = setTimeout(() => {
    page.value = 1
    void loadOrders(1)
  }, 350)
})

watch(statusFilter, () => {
  page.value = 1
  void loadOrders(1)
})

onMounted(() => {
  void loadOrders()
})

onBeforeUnmount(() => {
  if (searchTimer) {
    clearTimeout(searchTimer)
  }

  latestRequest++
})
</script>

<template>
  <div class="body">
    <Navbar />

    <div class="layout">
      <Sidebar />

      <main class="content">
        <div class="parent">
          <section class="div1" aria-labelledby="orders-title">
            <div>
              <h2 id="orders-title">Quản lý đơn hàng</h2>
              <p>Theo dõi thông tin khách hàng và sản phẩm đã đặt</p>
            </div>
          </section>

          <section class="div2">
            <h3>Tổng quan</h3>

            <div class="order-total" aria-live="polite">
              <span>Tổng đơn hàng</span>
              <strong>{{ totalCount }}</strong>
            </div>
          </section>

          <section class="div3">
            <div class="order-filters" role="search">
              <div class="search-box">
                <label for="order-search">Tìm kiếm đơn hàng</label>
                <input
                  id="order-search"
                  v-model="searchQuery"
                  type="search"
                  placeholder="Mã đơn, tên, SĐT hoặc địa chỉ..."
                  autocomplete="off"
                />
              </div>

              <div class="filter-box">
                <label for="order-status">Trạng thái</label>
                <select id="order-status" v-model="statusFilter">
                  <option v-for="option in statusOptions" :key="option.value" :value="option.value">
                    {{ option.label }}
                  </option>
                </select>
              </div>
            </div>
          </section>

          <section class="div4" aria-labelledby="order-list-title">
            <div class="table-heading">
              <h3 id="order-list-title">Danh sách đơn hàng</h3>

              <div class="order-action-feedback" aria-live="polite">
                <p v-if="actionMessage" class="action-success" role="status">
                  {{ actionMessage }}
                </p>
                <p v-if="actionError" class="action-error" role="alert">
                  {{ actionError }}
                </p>
              </div>
            </div>

            <div v-if="errorMessage" class="order-state" role="alert">
              <div>
                <h4>Chưa thể tải đơn hàng</h4>
                <p>{{ errorMessage }}</p>
              </div>
              <button type="button" @click="loadOrders()">Thử lại</button>
            </div>

            <template v-else>
              <div class="table-scroll">
                <table class="order-table">
                  <thead>
                    <tr>
                      <th scope="col">Mã đơn</th>
                      <th scope="col">Khách hàng</th>
                      <th scope="col">Địa chỉ giao hàng</th>
                      <th scope="col">Sản phẩm</th>
                      <th scope="col">Tổng tiền</th>
                      <th scope="col">Thanh toán</th>
                      <th scope="col">Trạng thái</th>
                      <th scope="col">Thời gian</th>
                      <th scope="col">Thao tác</th>
                    </tr>
                  </thead>

                  <tbody v-if="loading">
                    <tr>
                      <td colspan="9" class="loading-cell">Đang tải đơn hàng...</td>
                    </tr>
                  </tbody>

                  <tbody v-else-if="orders.length === 0">
                    <tr>
                      <td colspan="9" class="empty-cell">
                        <strong>Chưa có đơn hàng phù hợp</strong>
                        <span>Đơn khách đặt tại cửa hàng sẽ xuất hiện ở đây.</span>
                      </td>
                    </tr>
                  </tbody>

                  <template v-else v-for="order in orders" :key="order.id">
                    <tbody>
                      <tr class="order-row">
                        <td>
                          <strong class="order-code">{{ order.orderCode }}</strong>
                          <small>#{{ order.id }}</small>
                        </td>
                        <td>
                          <strong class="customer-name">{{ order.customerName }}</strong>
                          <a :href="`tel:${order.phone}`">{{ order.phone }}</a>
                        </td>
                        <td>
                          <span class="address" :title="order.deliveryAddress">
                            {{ order.deliveryAddress }}
                          </span>
                        </td>
                        <td>
                          <strong>{{ order.totalQuantity }}</strong>
                          <small>{{ order.itemCount }} loại sản phẩm</small>
                        </td>
                        <td>
                          <strong class="amount">{{ formatCurrency(order.totalAmount) }}</strong>
                        </td>
                        <td>
                          <span class="payment-badge">{{ order.paymentMethod }}</span>
                        </td>
                        <td>
                          <span class="status-badge" :class="statusClass(order.status)">
                            {{ statusLabel(order.status) }}
                          </span>
                        </td>
                        <td>
                          <time :datetime="order.createdAt">{{ formatDate(order.createdAt) }}</time>
                        </td>
                        <td>
                          <div class="order-actions">
                            <button
                              v-if="order.status === 'PENDING'"
                              class="confirm-order-button"
                              type="button"
                              :disabled="isConfirming(order.id)"
                              :aria-label="`Xác nhận đơn hàng ${order.orderCode}`"
                              @click="handleConfirmOrder(order)"
                            >
                              {{ isConfirming(order.id) ? 'Đang xác nhận...' : 'Xác nhận' }}
                            </button>

                            <button
                              class="detail-toggle"
                              type="button"
                              :aria-expanded="isExpanded(order.id)"
                              :aria-controls="`order-detail-${order.id}`"
                              :aria-label="`${isExpanded(order.id) ? 'Ẩn' : 'Xem'} chi tiết ${order.orderCode}`"
                              @click="toggleOrder(order.id)"
                            >
                              {{ isExpanded(order.id) ? '−' : '+' }}
                            </button>
                          </div>
                        </td>
                      </tr>

                      <tr
                        v-if="isExpanded(order.id)"
                        :id="`order-detail-${order.id}`"
                        class="detail-row"
                      >
                        <td colspan="9">
                          <div class="order-detail">
                            <div class="detail-heading">
                              <div>
                                <p>Chi tiết sản phẩm</p>
                                <strong>{{ order.orderCode }}</strong>
                              </div>
                              <span>{{ order.items.length }} dòng sản phẩm</span>
                            </div>

                            <div v-if="order.items.length === 0" class="no-items">
                              Không có dữ liệu chi tiết sản phẩm.
                            </div>

                            <table v-else class="item-table">
                              <thead>
                                <tr>
                                  <th scope="col">Sản phẩm</th>
                                  <th scope="col">Đơn giá</th>
                                  <th scope="col">Số lượng</th>
                                  <th scope="col">Thành tiền</th>
                                </tr>
                              </thead>
                              <tbody>
                                <tr
                                  v-for="item in order.items"
                                  :key="`${order.id}-${item.productCode}`"
                                >
                                  <td>
                                    <strong>{{ item.productName }}</strong>
                                    <small>{{ item.productCode }}</small>
                                  </td>
                                  <td>{{ formatCurrency(item.unitPrice) }}</td>
                                  <td>{{ item.quantity }}</td>
                                  <td>
                                    <strong>{{ formatCurrency(item.lineTotal) }}</strong>
                                  </td>
                                </tr>
                              </tbody>
                            </table>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </template>
                </table>
              </div>

              <nav
                v-if="!loading && totalPages > 1"
                class="pagination"
                aria-label="Phân trang đơn hàng"
              >
                <button type="button" :disabled="page === 1" @click="changePage(page - 1)">
                  Trước
                </button>
                <span>Trang {{ page }} / {{ totalPages }}</span>
                <button type="button" :disabled="page === totalPages" @click="changePage(page + 1)">
                  Sau
                </button>
              </nav>
            </template>
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

.layout {
  display: flex;
  min-height: 90vh;
}

.content {
  flex: 1;
  min-width: 0;
  padding: 10px;
}

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

.div1 h2,
.div2 h3,
.div4 h3 {
  margin-bottom: 15px;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.div1 p {
  color: #666;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 14px;
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

.order-total {
  min-width: 180px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.order-total strong {
  min-width: 55px;
  padding: 8px 12px;
  color: #007aff;
  background-color: #edf5ff;
  border-radius: 8px;
  font-size: 20px;
  text-align: center;
}

.order-filters {
  width: 100%;
  display: grid;
  grid-template-columns: minmax(260px, 1fr) 220px;
  align-items: end;
  gap: 20px;
}

.search-box,
.filter-box {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.search-box label,
.filter-box label {
  font-size: 14px;
  font-weight: 500;
}

.search-box input,
.filter-box select {
  width: 100%;
  min-width: 0;
  padding: 10px 12px;
  background-color: white;
  border: 1px solid #ccc;
  border-radius: 6px;
  outline: none;
  font-size: 16px;
}

.search-box input:focus,
.filter-box select:focus {
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

.order-action-feedback {
  min-height: 22px;
  font-size: 13px;
  font-weight: 600;
  text-align: right;
}

.order-action-feedback p {
  margin: 0;
}

.action-success {
  color: #127550;
}

.action-error {
  color: #a0444b;
}

.table-scroll {
  flex: 1;
  width: 100%;
  overflow: auto;
}

.order-table {
  width: 100%;
  min-width: 1320px;
  border-collapse: collapse;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 16px;
}

.order-table th,
.order-table td {
  padding: 12px;
  border-bottom: 1px solid #ddd;
  text-align: left;
  vertical-align: middle;
}

.order-table th {
  position: sticky;
  top: 0;
  z-index: 1;
  background-color: #f5f5f5;
  font-weight: 600;
  white-space: nowrap;
}

.order-table .order-row:hover {
  background-color: #f7f9fc;
}

.order-table td strong,
.order-table td small,
.order-table td a {
  display: block;
}

.order-table td small,
.order-table td time {
  margin-top: 4px;
  color: #777;
  font-size: 12px;
}

.order-table td a {
  margin-top: 4px;
  color: #007aff;
  font-size: 13px;
  text-decoration: none;
}

.order-table td a:hover {
  text-decoration: underline;
}

.order-code,
.customer-name {
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.order-code {
  color: #007aff;
  font-size: 14px;
}

.address {
  width: 230px;
  display: -webkit-box;
  overflow: hidden;
  color: #555;
  line-height: 1.45;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.amount {
  color: #007aff;
  white-space: nowrap;
}

.payment-badge,
.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 6px 9px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.payment-badge {
  color: #72551d;
  background: #fff5dc;
}

.status-pending {
  color: #8a6415;
  background: #fff4d4;
}

.status-confirmed {
  color: #1266aa;
  background: #e6f3ff;
}

.status-completed {
  color: #127550;
  background: #def7eb;
}

.status-cancelled {
  color: #a0444b;
  background: #ffe8ea;
}

.order-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.confirm-order-button {
  min-height: 34px;
  padding: 0 12px;
  color: white;
  background: #127550;
  border: 1px solid #127550;
  border-radius: 6px;
  font-size: 12px;
  font-weight: 700;
  white-space: nowrap;
  cursor: pointer;
}

.confirm-order-button:hover:not(:disabled) {
  background: #0d6342;
  border-color: #0d6342;
}

.confirm-order-button:disabled {
  opacity: 0.65;
  cursor: wait;
}

.detail-toggle {
  width: 34px;
  height: 34px;
  display: grid;
  place-items: center;
  color: #007aff;
  background: #edf5ff;
  border: 1px solid #b8d8ff;
  border-radius: 6px;
  font-size: 20px;
  cursor: pointer;
}

.detail-toggle:hover {
  background-color: #dfeeff;
}

.detail-row > td {
  padding: 0;
  background: #f7f9fc;
}

.order-detail {
  padding: 18px 22px 22px;
}

.detail-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 12px;
}

.detail-heading p {
  margin-bottom: 4px;
  color: #666;
  font-size: 12px;
}

.detail-heading > span {
  color: #777;
  font-size: 13px;
}

.item-table {
  width: 100%;
  min-width: 680px;
  overflow: hidden;
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  border-collapse: collapse;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 14px;
}

.item-table th,
.item-table td {
  padding: 10px 12px;
}

.item-table tr:last-child td {
  border-bottom: 0;
}

.no-items {
  padding: 18px;
  color: #777;
  background: white;
  border: 1px dashed #ccc;
  border-radius: 8px;
  text-align: center;
}

.loading-cell,
.empty-cell {
  height: 220px;
  color: #777;
  text-align: center !important;
}

.empty-cell strong,
.empty-cell span {
  display: block;
}

.empty-cell span {
  margin-top: 8px;
}

.order-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 24px;
  color: #8b6265;
  font-family: 'Be Vietnam Pro', sans-serif;
  text-align: center;
}

.order-state h4 {
  margin-bottom: 6px;
  color: #333;
  font-size: 16px;
}

.order-state button {
  padding: 9px 14px;
  color: #007aff;
  background-color: white;
  border: 1px solid #007aff;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
}

.order-state button:hover {
  color: white;
  background-color: #007aff;
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
  color: #007aff;
  background-color: white;
  border: 1px solid #007aff;
  border-radius: 6px;
  cursor: pointer;
}

.pagination button:hover:not(:disabled) {
  background-color: #edf5ff;
}

.pagination button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

button:focus-visible,
input:focus-visible,
select:focus-visible,
a:focus-visible {
  outline: 3px solid rgba(0, 122, 255, 0.25);
  outline-offset: 2px;
}

@media (max-width: 900px) {
  .order-filters {
    grid-template-columns: 1fr;
  }

  .order-table {
    min-width: 1240px;
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

  .search-box input,
  .filter-box select {
    font-size: 16px;
  }

  .detail-heading {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
