<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

defineOptions({ name: 'AdminNavbar' })

const logoUrl = `${import.meta.env.BASE_URL}logo.png`
const auth = useAuthStore()
const router = useRouter()
const loggingOut = ref(false)

const accountName = computed(() => auth.user?.displayName || auth.user?.email || 'Quản trị viên')

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
    await router.replace('/admin/login')
  } catch {
    // Keep the current session visible when the server could not log out.
  } finally {
    loggingOut.value = false
  }
}
</script>

<template>
  <div class="navbar">
    <RouterLink class="brand-link" to="/dashboard" aria-label="CN25 - Dashboard quản trị">
      <img :src="logoUrl" alt="CN25" />
    </RouterLink>
    <input type="text" aria-label="Tìm kiếm trong trang quản trị" placeholder="Tìm kiếm.." />

    <div class="account-summary" :title="auth.user?.email">
      <span class="account-avatar" aria-hidden="true">{{ accountInitials }}</span>
      <span class="account-copy">
        <small>Quản trị viên</small>
        <strong>{{ accountName }}</strong>
      </span>
    </div>

    <button class="logout-button" type="button" :disabled="loggingOut" @click="handleLogout">
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M10 5H5v14h5M14 8l4 4-4 4M8 12h10" />
      </svg>
      <span>{{ loggingOut ? 'Đang thoát...' : 'Đăng xuất' }}</span>
    </button>
  </div>
</template>

<style scoped>
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

  background-color: #0d1828;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-weight: 500;
  font-style: normal;

  border-bottom: 3px solid #00d4ea;
}

.navbar img {
  width: 120px;
  height: auto;
}

.navbar .brand-link {
  padding: 10px;

  color: #007aff;
  text-decoration: none;

  font-size: 1.2rem;
  font-weight: 800;
}

.navbar .brand-link:hover {
  background-color: #1a2939;
}

.navbar input[type='text'] {
  float: right;
  padding: 6px;

  border: 2px solid transparent;
  border-radius: 8px;

  margin-left: auto;
  margin-right: 1%;

  font-size: 17px;
  background-color: #e7e7e7;

  outline: none;
  width: 30%;
  height: 5vh;
}

.navbar input[type='text']:focus {
  border-color: #007aff;
}

.account-summary {
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 9px;
}

.account-avatar {
  width: 36px;
  height: 36px;
  flex: 0 0 auto;
  display: grid;
  place-items: center;
  color: #0d1828;
  background: #66edf5;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 900;
}

.account-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
  color: #fff;
}

.account-copy small {
  color: #8fa6bf;
  font-size: 8px;
  font-weight: 700;
  text-transform: uppercase;
}

.account-copy strong {
  max-width: 150px;
  overflow: hidden;
  font-size: 11px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.logout-button {
  min-width: 118px;
  height: 5vh;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  margin-right: 1%;
  padding: 0 13px;
  color: white;
  background-color: #007aff;
  border: 2px solid #007aff;
  border-radius: 8px;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
}

.logout-button svg {
  width: 17px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.logout-button:hover:not(:disabled) {
  background-color: #0062cc;
  transform: translateY(-1px);
}

.logout-button:disabled {
  opacity: 0.65;
  cursor: wait;
}

@media (max-width: 900px) {
  .account-copy {
    display: none;
  }

  .navbar input[type='text'] {
    width: 26%;
  }
}

@media (max-width: 620px) {
  .navbar input[type='text'] {
    display: none;
  }

  .account-summary {
    margin-left: auto;
  }

  .logout-button {
    min-width: 42px;
    width: 42px;
    padding: 0;
  }

  .logout-button span {
    position: absolute;
    width: 1px;
    height: 1px;
    overflow: hidden;
    clip: rect(0 0 0 0);
  }
}
</style>
