<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const emit = defineEmits<{
  logout: []
}>()

const auth = useAuthStore()
const router = useRouter()
const loggingOut = ref(false)

const accountName = computed(() => auth.user?.displayName || auth.user?.email || 'Khách hàng')

const accountInitials = computed(() =>
  accountName.value
    .trim()
    .split(/\s+/)
    .slice(-2)
    .map((word) => word.charAt(0))
    .join('')
    .toUpperCase(),
)

async function handleLogout() {
  if (loggingOut.value) {
    return
  }

  loggingOut.value = true

  try {
    await auth.logout()
    emit('logout')
    await router.replace('/login')
  } catch {
    // Keep the current session visible when the server could not log out.
  } finally {
    loggingOut.value = false
  }
}
</script>

<template>
  <div class="store-account" :title="auth.user?.email">
    <span class="customer-avatar" aria-hidden="true">{{ accountInitials }}</span>
    <span class="customer-copy">
      <small>Xin chào</small>
      <strong>{{ accountName }}</strong>
    </span>
    <button
      type="button"
      :disabled="loggingOut"
      :aria-label="loggingOut ? 'Đang đăng xuất' : 'Đăng xuất tài khoản khách hàng'"
      @click="handleLogout"
    >
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M10 5H5v14h5M14 8l4 4-4 4M8 12h10" />
      </svg>
    </button>
  </div>
</template>

<style scoped>
.store-account {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.customer-avatar {
  width: 40px;
  height: 40px;
  flex: 0 0 auto;
  display: grid;
  place-items: center;
  color: #fff;
  background: linear-gradient(145deg, #00cedf, #0878f9);
  border: 1px solid rgba(255, 255, 255, 0.24);
  border-radius: 10px;
  font-size: 11px;
  font-weight: 900;
}

.customer-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.customer-copy small {
  color: #9fb0c6;
  font-size: 10px;
}

.customer-copy strong {
  max-width: 115px;
  overflow: hidden;
  color: #fff;
  font-size: 13px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

button {
  width: 40px;
  height: 40px;
  display: grid;
  flex: 0 0 auto;
  place-items: center;
  padding: 0;
  color: #d2ddea;
  background: #17253a;
  border: 1px solid #34465d;
  border-radius: 8px;
  font-family: inherit;
  cursor: pointer;
  transition:
    color 150ms ease,
    background 150ms ease,
    border-color 150ms ease;
}

button:hover:not(:disabled) {
  color: #fff;
  background: #b7444e;
  border-color: #d75a65;
}

button:disabled {
  opacity: 0.6;
  cursor: wait;
}

button svg {
  width: 16px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

button:focus-visible {
  outline: 3px solid rgba(0, 206, 223, 0.45);
  outline-offset: 3px;
}

@media (max-width: 980px) {
  .customer-copy {
    display: none;
  }
}

@media (max-width: 480px) {
  .customer-avatar {
    width: 36px;
    height: 36px;
  }

  button {
    width: 36px;
    height: 36px;
  }
}
</style>
