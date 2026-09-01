<script setup lang="ts">
import AuthLoginForm from '../../component/AuthLoginForm.vue'
</script>

<template>
  <AuthLoginForm
    portal="ADMIN"
    role-label="Khu vực quản trị"
    title="Đăng nhập quản trị"
    action-label="Đăng nhập"
    switch-prompt="Bạn là khách hàng?"
    switch-label="Đăng nhập cửa hàng"
    switch-to="/login"
  />
</template>
