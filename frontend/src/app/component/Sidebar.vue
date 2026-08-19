<template>
    <aside class="sidebar">
        <a href="#">Dashboard</a>
        <RouterLink to="/productPage">Products</RouterLink>
        <a href="#">Categories</a>
        <a href="#">Orders</a>
        <a href="#">Inventory</a>
        <a href="#">Users</a>
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
  background-color: #007AFF;
  
}
</style>