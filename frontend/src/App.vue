<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterView } from 'vue-router'
import { getHealth } from './services/api'

const message = ref('Đang kết nối backend...')

onMounted(async () => {
  try {
    const data = await getHealth()
    message.value = data.message
  } catch {
    message.value = 'Không kết nối được backend'
  }
})
</script>

<template>
  <div>
    <h1>StoreWeb</h1>

    <p>{{ message }}</p>

    <RouterView />
  </div>
</template>