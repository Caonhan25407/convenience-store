<template>
<div class="navbar">
    <RouterLink to="/adminPage">
      <img src="/logo.png" alt="logo">
    </RouterLink>
    <a href="#">Search</a>
    <a href="#">Contact</a>
    <a href="#">Login</a>
</div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Anton&family=Audiowide&family=Be+Vietnam+Pro:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&family=Kanit:ital,wght@0,100;0,200;0,300;0,400;0,500;0,600;0,700;0,800;0,900;1,100;1,200;1,300;1,400;1,500;1,600;1,700;1,800;1,900&family=Montserrat:ital,wght@0,100..900;1,100..900&display=swap');

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

.navbar {
  display: flex;
  align-items: center;
  gap: 10px;
    
  width: 100%;
  height: 10vh;

  background-color:#ffffff;
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
</style>