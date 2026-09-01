<script setup lang="ts">
import AuthLoginForm from '../../component/AuthLoginForm.vue'
</script>

<template>
  <AuthLoginForm
    portal="CUSTOMER"
    role-label="Cửa hàng CN25"
    title="Chào mừng bạn trở lại"
    action-label="Đăng nhập"
    register-label="Đăng ký"
    register-to="/register"
    switch-prompt="Bạn là quản trị viên?"
    switch-label="Đăng nhập quản trị"
    switch-to="/admin/login"
  />
</template>
