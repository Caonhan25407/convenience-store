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
          <section class="div1" aria-labelledby="classify-title">
            <div>
              <h2 id="classify-title">Quản lý phân loại sản phẩm</h2>
              <p>Sắp xếp sản phẩm theo từng nhóm để dễ tìm kiếm và quản lý.</p>
            </div>
          </section>

          <section class="div2" aria-labelledby="classify-list-title">
            <h3 id="classify-list-title">Danh sách phân loại</h3>

            <div class="empty-state">
              <strong>Chưa có dữ liệu phân loại</strong>
              <span>Chức năng phân loại sản phẩm đang được cập nhật.</span>
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

.div1 h2,
.div2 h3 {
  margin-bottom: 15px;
}

.div1 p {
  color: #666;
  font-size: 14px;
}

.div2 {
  grid-column: 1 / 4;
  min-height: 55vh;
  display: flex;
  flex-direction: column;
}

.empty-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 8px;
  color: #666;
  text-align: center;
}

.empty-state strong {
  color: #333;
  font-size: 16px;
}

.empty-state span {
  font-size: 14px;
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
    min-height: 60vh;
  }
}
</style>
