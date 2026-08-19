<template>
<div class="navbar">
    <RouterLink to="/adminPage">
        <img src="/logo.png" alt="logo">
    </RouterLink>
    <input type="text" placeholder="Tìm kiếm..">
    <div class="userBlock">
        <h1>Đăng nhập</h1>
    </div>
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

  background-color:#0D1828;
  font-family: "Be Vietnam Pro", sans-serif;
  font-weight: 500;
  font-style: normal;

  border-bottom: 3px solid #00D4EA;
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

.navbar input[type=text] {
    float: right;
    padding: 6px;
    
    border: 2px solid transparent;
    border-radius: 6px;

    margin-left: auto;
    margin-top: 8px;
    margin-right: 16px;

    font-size: 17px;
    background-color: #e7e7e7;

    outline: none;
}

.navbar input[type="text"]:focus {
    border-color: #007AFF;
}

.userBlock {
    margin-right: 2%;
    width: 10%;
    height: 6vh;
    border: 2px solid #007AFF;
    border-radius: 8px;

    font-size: 0.6rem;
    color: white;
    display: flex;
    align-items: center;
    justify-content: center;
}
</style>