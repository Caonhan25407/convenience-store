<script setup lang="ts">
defineOptions({ name: 'AdminSidebar' })
</script>

<template>
  <aside class="sidebar">
    <RouterLink to="/dashboard">Dashboard</RouterLink>
    <RouterLink to="/productPage">Hàng hóa</RouterLink>
    <RouterLink to="/classifyPage">Phân loại</RouterLink>
    <RouterLink to="/orderPage">Đơn hàng</RouterLink>
    <RouterLink to="/usersPage">Người dùng</RouterLink>
  </aside>
</template>

<style scoped>
/* SIDEBAR */
.sidebar {
  width: 220px;

  display: flex;
  flex-direction: column;

  background-color: white;

  padding: 20px 10px;
}

.sidebar a {
  color: #007aff;
  text-decoration: none;

  padding: 15px;

  border-radius: 8px;

  margin-bottom: 5px;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-weight: 500;
  font-style: normal;
}

.sidebar a:hover {
  background-color: #e7e7e7;
  border-right: 3px solid #00d4ea;
}

.sidebar a.router-link-active {
  background-color: #e7e7e7;
  border-right: 3px solid #00d4ea;
}
</style>
