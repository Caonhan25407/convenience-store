<template>
    <aside class="sidebar">
        <a href="#">Dashboard</a>
        <RouterLink to="/productPage">Products</RouterLink>
        <a href="#">Phân loại</a>
        <a href="#">Đơn hàng</a>
        <a href="#">Hàng tồn kho</a>
        <a href="#">Người dùng</a>
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
  background-color: #e7e7e7;
  border-right: 3px solid #00D4EA;
}

.sidebar a.router-link-active {
    background-color: #e7e7e7;
    border-right: 3px solid #00D4EA;
}
</style>