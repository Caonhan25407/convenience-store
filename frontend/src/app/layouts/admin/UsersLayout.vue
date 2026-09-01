<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { getUsers } from '@/services/userService'
import type { AdminUser, UserRole } from '@/types/user'
import Navbar from '../../component/Navbar.vue'
import Sidebar from '../../component/Sidebar.vue'

const users = ref<AdminUser[]>([])
const loading = ref(false)
const errorMessage = ref('')
const searchQuery = ref('')
const roleFilter = ref<UserRole | 'all'>('all')
const page = ref(1)
const pageSize = 15
const totalCount = ref(0)
const totalPages = ref(0)

let searchTimer: ReturnType<typeof setTimeout> | undefined
let latestRequest = 0

async function loadUsers(targetPage = page.value) {
  const requestId = ++latestRequest

  try {
    loading.value = true
    errorMessage.value = ''
    page.value = targetPage

    const result = await getUsers({
      page: targetPage,
      pageSize,
      search: searchQuery.value.trim(),
      role: roleFilter.value,
    })

    if (requestId !== latestRequest) {
      return
    }

    users.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  } catch (error) {
    if (requestId !== latestRequest) {
      return
    }

    users.value = []
    totalCount.value = 0
    totalPages.value = 0
    errorMessage.value =
      error instanceof Error ? error.message : 'Không thể tải danh sách người dùng'
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

  void loadUsers(nextPage)
}

function roleLabel(role: UserRole) {
  return role === 'ADMIN' ? 'Quản trị viên' : 'Khách hàng'
}

function formatDate(value: string | null) {
  if (!value) {
    return 'Chưa đăng nhập'
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

watch(searchQuery, () => {
  if (searchTimer) {
    clearTimeout(searchTimer)
  }

  searchTimer = setTimeout(() => {
    page.value = 1
    void loadUsers(1)
  }, 350)
})

watch(roleFilter, () => {
  page.value = 1
  void loadUsers(1)
})

onMounted(() => {
  void loadUsers()
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
          <section class="div1" aria-labelledby="users-title">
            <div>
              <h2 id="users-title">Quản lý người dùng</h2>
              <p>Danh sách tài khoản quản trị và khách hàng</p>
            </div>
          </section>

          <section class="div2">
            <h3>Tổng quan</h3>

            <div class="total-user" aria-live="polite">
              <span>Tổng người dùng</span>
              <strong>{{ totalCount }}</strong>
            </div>
          </section>

          <section class="div3">
            <div class="users-filters" role="search">
              <div class="search-box">
                <label for="user-search">Tìm kiếm người dùng</label>
                <input
                  id="user-search"
                  v-model="searchQuery"
                  type="search"
                  placeholder="Tên, email hoặc số điện thoại..."
                  autocomplete="off"
                />
              </div>

              <div class="filter-box">
                <label for="user-role">Vai trò</label>
                <select id="user-role" v-model="roleFilter">
                  <option value="all">Tất cả vai trò</option>
                  <option value="ADMIN">Quản trị viên</option>
                  <option value="CUSTOMER">Khách hàng</option>
                </select>
              </div>
            </div>
          </section>

          <section class="div4" aria-labelledby="user-list-title">
            <div class="table-heading">
              <h3 id="user-list-title">Danh sách người dùng</h3>
            </div>

            <div v-if="errorMessage" class="users-state" role="alert">
              <div>
                <h4>Chưa thể tải người dùng</h4>
                <p>{{ errorMessage }}</p>
              </div>
              <button type="button" @click="loadUsers()">Thử lại</button>
            </div>

            <template v-else>
              <div class="table-scroll">
                <table class="user-table">
                  <caption class="sr-only">
                    Danh sách tài khoản người dùng
                  </caption>
                  <thead>
                    <tr>
                      <th scope="col">Tên</th>
                      <th scope="col">Email</th>
                      <th scope="col">Số điện thoại</th>
                      <th scope="col">Vai trò</th>
                      <th scope="col">Trạng thái</th>
                      <th scope="col">Ngày tạo</th>
                      <th scope="col">Đăng nhập gần nhất</th>
                    </tr>
                  </thead>

                  <tbody v-if="loading">
                    <tr>
                      <td colspan="7" class="state-cell">Đang tải người dùng...</td>
                    </tr>
                  </tbody>

                  <tbody v-else-if="users.length === 0">
                    <tr>
                      <td colspan="7" class="state-cell empty-cell">
                        <strong>Không có người dùng phù hợp</strong>
                        <span>Hãy thử từ khóa hoặc vai trò khác.</span>
                      </td>
                    </tr>
                  </tbody>

                  <tbody v-else>
                    <tr v-for="user in users" :key="user.id">
                      <td>
                        <strong class="user-name">{{ user.displayName }}</strong>
                        <small>#{{ user.id }}</small>
                      </td>
                      <td>
                        <a :href="`mailto:${user.email}`">{{ user.email }}</a>
                      </td>
                      <td>
                        <a v-if="user.phone" :href="`tel:${user.phone}`">{{ user.phone }}</a>
                        <span v-else class="muted-value">—</span>
                      </td>
                      <td>
                        <span class="role-badge" :class="`role-${user.role.toLowerCase()}`">
                          {{ roleLabel(user.role) }}
                        </span>
                      </td>
                      <td>
                        <span
                          class="status-badge"
                          :class="user.isActive ? 'is-active' : 'is-inactive'"
                        >
                          {{ user.isActive ? 'Hoạt động' : 'Ngừng hoạt động' }}
                        </span>
                      </td>
                      <td>
                        <time :datetime="user.createdAt">{{ formatDate(user.createdAt) }}</time>
                      </td>
                      <td>
                        <time v-if="user.lastLoginAt" :datetime="user.lastLoginAt">
                          {{ formatDate(user.lastLoginAt) }}
                        </time>
                        <span v-else class="muted-value">Chưa đăng nhập</span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <nav
                v-if="!loading && totalPages > 1"
                class="pagination"
                aria-label="Phân trang người dùng"
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

.total-user {
  min-width: 180px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.total-user strong {
  min-width: 55px;
  padding: 8px 12px;
  color: #007aff;
  background-color: #edf5ff;
  border-radius: 8px;
  font-size: 20px;
  text-align: center;
}

.users-filters {
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

.table-scroll {
  flex: 1;
  width: 100%;
  overflow: auto;
}

.user-table {
  width: 100%;
  min-width: 980px;
  border-collapse: collapse;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 16px;
}

.user-table th,
.user-table td {
  padding: 12px;
  border-bottom: 1px solid #ddd;
  text-align: left;
  vertical-align: middle;
}

.user-table th {
  position: sticky;
  top: 0;
  z-index: 1;
  background-color: #f5f5f5;
  font-weight: 600;
  white-space: nowrap;
}

.user-table tbody tr:hover {
  background-color: #f7f9fc;
}

.user-table td strong,
.user-table td small {
  display: block;
}

.user-table td small {
  margin-top: 4px;
  color: #777;
  font-size: 12px;
}

.user-table td a {
  color: #007aff;
  text-decoration: none;
}

.user-table td a:hover {
  text-decoration: underline;
}

.user-table td time,
.muted-value {
  color: #666;
  font-size: 13px;
  white-space: nowrap;
}

.user-name {
  max-width: 190px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.role-badge,
.status-badge {
  display: inline-flex;
  align-items: center;
  padding: 6px 9px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.role-admin {
  color: #175e9c;
  background: #e3f1ff;
}

.role-customer {
  color: #6c4b13;
  background: #fff2d6;
}

.is-active {
  color: #127550;
  background: #def7eb;
}

.is-inactive {
  color: #a0444b;
  background: #ffe8ea;
}

.state-cell {
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

.users-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 24px;
  color: #8b6265;
  font-family: 'Be Vietnam Pro', sans-serif;
  text-align: center;
}

.users-state h4 {
  margin-bottom: 6px;
  color: #333;
  font-size: 16px;
}

.users-state button {
  padding: 9px 14px;
  color: #007aff;
  background-color: white;
  border: 1px solid #007aff;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
}

.users-state button:hover {
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
  .users-filters {
    grid-template-columns: 1fr;
  }

  .user-table {
    min-width: 900px;
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
}
</style>
