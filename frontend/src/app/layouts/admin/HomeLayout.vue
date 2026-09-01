<script setup lang="ts">
import Navbar from '../../component/Navbar.vue'
import Sidebar from '../../component/Sidebar.vue'
</script>

<template>
  <div class="body">
    <Navbar />

    <div class="layout">
      <Sidebar />
      <main class="content">
        <div class="parent">
          <section class="div1" aria-labelledby="home-title">
            <div>
              <p class="section-label">Quản trị hệ thống</p>
              <h2 id="home-title">Chào mừng đến trang quản trị</h2>
              <span>Chọn một khu vực bên dưới để bắt đầu quản lý cửa hàng.</span>
            </div>
          </section>

          <section class="div2" aria-labelledby="quick-access-title">
            <div class="section-heading">
              <h3 id="quick-access-title">Truy cập nhanh</h3>
              <p>Các chức năng quản trị được sử dụng thường xuyên.</p>
            </div>

            <nav class="quick-links" aria-label="Truy cập nhanh trang quản trị">
              <RouterLink class="quick-link" to="/dashboard">
                <span class="quick-link-number">01</span>
                <strong>Dashboard</strong>
                <small>Xem tổng quan hàng hóa của cửa hàng.</small>
                <span class="quick-link-arrow" aria-hidden="true">→</span>
              </RouterLink>

              <RouterLink class="quick-link" to="/productPage">
                <span class="quick-link-number">02</span>
                <strong>Hàng hóa</strong>
                <small>Quản lý và cập nhật danh sách sản phẩm.</small>
                <span class="quick-link-arrow" aria-hidden="true">→</span>
              </RouterLink>

              <RouterLink class="quick-link" to="/orderPage">
                <span class="quick-link-number">03</span>
                <strong>Đơn hàng</strong>
                <small>Theo dõi các đơn hàng khách đã đặt.</small>
                <span class="quick-link-arrow" aria-hidden="true">→</span>
              </RouterLink>

              <RouterLink class="quick-link" to="/usersPage">
                <span class="quick-link-number">04</span>
                <strong>Người dùng</strong>
                <small>Xem tài khoản quản trị và khách hàng.</small>
                <span class="quick-link-arrow" aria-hidden="true">→</span>
              </RouterLink>
            </nav>
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
.div2 {
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
.section-heading p {
  color: #666;
  font-size: 14px;
  line-height: 1.6;
}

.div2 {
  grid-column: 1 / 4;
  min-height: calc(90vh - 130px);
}

.section-heading {
  margin-bottom: 20px;
}

.section-heading h3 {
  margin-bottom: 5px;
  font-size: 18px;
}

.quick-links {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.quick-link {
  position: relative;
  min-height: 160px;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  padding: 20px;
  color: #222;
  background-color: #fafafa;
  border: 1px solid #e2e2e2;
  border-radius: 10px;
  text-decoration: none;
  transition:
    border-color 0.2s,
    background-color 0.2s,
    transform 0.2s;
}

.quick-link:hover {
  background-color: #f5f9ff;
  border-color: #9bc9ff;
  transform: translateY(-2px);
}

.quick-link-number {
  margin-bottom: 18px;
  color: #007aff;
  font-size: 12px;
  font-weight: 700;
}

.quick-link strong {
  margin-bottom: 7px;
  font-size: 18px;
}

.quick-link small {
  max-width: 320px;
  color: #666;
  font-size: 14px;
  line-height: 1.6;
}

.quick-link-arrow {
  position: absolute;
  top: 18px;
  right: 20px;
  color: #007aff;
  font-size: 22px;
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
  .div2 {
    grid-column: 1;
    padding: 16px;
  }

  .div1 {
    min-height: 90px;
  }

  .div2 {
    min-height: auto;
  }

  .quick-links {
    grid-template-columns: 1fr;
  }

  .quick-link {
    min-height: 145px;
    padding: 16px;
  }
}
</style>
