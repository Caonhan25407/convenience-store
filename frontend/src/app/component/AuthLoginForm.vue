<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { resolveSafeRedirect } from '@/app/router/authGuard'
import { useAuthStore } from '@/stores/auth'
import type { UserRole } from '@/types/auth'

const props = defineProps<{
  portal: UserRole
  roleLabel: string
  title: string
  actionLabel: string
  switchPrompt: string
  switchLabel: string
  switchTo: string
  registerLabel?: string
  registerTo?: string
}>()

const logoUrl = `${import.meta.env.BASE_URL}logo.png`
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const submitting = ref(false)
const showPassword = ref(false)
const submitError = ref('')
const form = reactive({ email: '', password: '' })
const fieldErrors = reactive({ email: '', password: '' })

function clearError(field: keyof typeof fieldErrors) {
  fieldErrors[field] = ''
  submitError.value = ''
}

const registrationTarget = computed(() => {
  const redirect = Array.isArray(route.query.redirect)
    ? route.query.redirect[0]
    : route.query.redirect
  const path = props.registerTo ?? '/register'

  return typeof redirect === 'string' ? { path, query: { redirect } } : path
})

function validateForm() {
  fieldErrors.email = ''
  fieldErrors.password = ''

  if (!form.email) {
    fieldErrors.email = 'Vui lòng nhập email'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    fieldErrors.email = 'Email chưa đúng định dạng'
  }

  if (!form.password) {
    fieldErrors.password = 'Vui lòng nhập mật khẩu'
  }

  return !fieldErrors.email && !fieldErrors.password
}

async function handleSubmit() {
  submitError.value = ''

  if (!validateForm() || submitting.value) {
    return
  }

  submitting.value = true

  try {
    const credentials = {
      email: form.email,
      password: form.password,
    }

    if (props.portal === 'ADMIN') {
      await auth.loginAdmin(credentials)
    } else {
      await auth.loginCustomer(credentials)
    }

    await router.replace(resolveSafeRedirect(route.query.redirect, props.portal, router))
  } catch (error) {
    submitError.value = error instanceof Error ? error.message : 'Không thể đăng nhập lúc này'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="login-page" :class="portal === 'ADMIN' ? 'admin-login' : 'customer-login'">
    <section class="login-card" :aria-labelledby="`${portal.toLowerCase()}-login-title`">
      <img class="logo" :src="logoUrl" alt="CN25" />
      <p class="role-label">{{ roleLabel }}</p>
      <h1 :id="`${portal.toLowerCase()}-login-title`">{{ title }}</h1>

      <form novalidate :aria-busy="submitting" @submit.prevent="handleSubmit">
        <div class="form-field">
          <label :for="`${portal.toLowerCase()}-email`">Email</label>
          <input
            :id="`${portal.toLowerCase()}-email`"
            v-model.trim="form.email"
            type="email"
            maxlength="254"
            autocomplete="username"
            inputmode="email"
            placeholder="tenban@example.com"
            :aria-invalid="Boolean(fieldErrors.email)"
            :aria-describedby="
              fieldErrors.email ? `${portal.toLowerCase()}-email-error` : undefined
            "
            @input="clearError('email')"
          />
          <p
            v-if="fieldErrors.email"
            :id="`${portal.toLowerCase()}-email-error`"
            class="field-error"
          >
            {{ fieldErrors.email }}
          </p>
        </div>

        <div class="form-field">
          <label :for="`${portal.toLowerCase()}-password`">Mật khẩu</label>
          <div class="password-field">
            <input
              :id="`${portal.toLowerCase()}-password`"
              v-model="form.password"
              :type="showPassword ? 'text' : 'password'"
              autocomplete="current-password"
              placeholder="Nhập mật khẩu"
              :aria-invalid="Boolean(fieldErrors.password)"
              :aria-describedby="
                fieldErrors.password ? `${portal.toLowerCase()}-password-error` : undefined
              "
              @input="clearError('password')"
            />
            <button
              type="button"
              :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
              :aria-pressed="showPassword"
              @click="showPassword = !showPassword"
            >
              {{ showPassword ? 'Ẩn' : 'Hiện' }}
            </button>
          </div>
          <p
            v-if="fieldErrors.password"
            :id="`${portal.toLowerCase()}-password-error`"
            class="field-error"
          >
            {{ fieldErrors.password }}
          </p>
        </div>

        <p v-if="submitError" class="submit-error" role="alert">
          {{ submitError }}
        </p>

        <button class="submit-button" type="submit" :disabled="submitting">
          {{ submitting ? 'Đang đăng nhập...' : actionLabel }}
        </button>
      </form>

      <RouterLink v-if="registerLabel && registerTo" class="register-link" :to="registrationTarget">
        {{ registerLabel }}
      </RouterLink>

      <p class="portal-switch">
        {{ switchPrompt }}
        <RouterLink :to="switchTo">{{ switchLabel }}</RouterLink>
      </p>
    </section>
  </main>
</template>

<style scoped>
:global(body) {
  margin: 0;
  min-width: 320px;
  background: #e7e7e7;
}

:global(*) {
  box-sizing: border-box;
}

:global(button),
:global(input) {
  font: inherit;
}

.login-page {
  --accent: #0878f9;
  --accent-dark: #0062cc;
  --accent-soft: #edf5ff;
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 20px;
  color: #202020;
  background: #e7e7e7;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.customer-login {
  --accent-soft: #eafcff;
}

.login-card {
  position: relative;
  width: min(440px, 100%);
  padding: 30px;
  overflow: hidden;
  background: #fff;
  border: 1px solid #d5d5d5;
  border-radius: 10px;
  box-shadow: 0 8px 24px rgb(0 0 0 / 7%);
}

.login-card::before {
  position: absolute;
  inset: 0 0 auto;
  height: 4px;
  background: linear-gradient(90deg, #0878f9, #00cedf);
  content: '';
}

.logo {
  width: 104px;
  height: 44px;
  display: block;
  margin: 0 auto 18px;
  object-fit: contain;
}

.role-label {
  margin: 0 0 8px;
  color: var(--accent);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-align: center;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  color: #202020;
  font-size: 26px;
  line-height: 1.3;
  text-align: center;
}

form {
  display: grid;
  gap: 17px;
  margin-top: 26px;
}

.form-field label {
  display: block;
  margin-bottom: 8px;
  color: #333;
  font-size: 14px;
  font-weight: 600;
}

.form-field input {
  width: 100%;
  min-height: 48px;
  padding: 10px 12px;
  color: #202020;
  background: #fff;
  border: 1px solid #ccc;
  border-radius: 6px;
  outline: 0;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 16px;
}

.form-field input:focus {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px rgb(8 120 249 / 12%);
}

.form-field input[aria-invalid='true'] {
  border-color: #dc3545;
}

.password-field {
  position: relative;
}

.password-field input {
  padding-right: 72px;
}

.password-field button {
  position: absolute;
  top: 50%;
  right: 7px;
  min-width: 54px;
  padding: 7px 8px;
  color: var(--accent-dark);
  background: var(--accent-soft);
  border: 0;
  border-radius: 6px;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transform: translateY(-50%);
}

.password-field button:hover {
  background: #dcecff;
}

.field-error {
  margin: 7px 0 0;
  color: #b4232f;
  font-size: 12px;
  line-height: 1.45;
}

.submit-error {
  margin: 0;
  padding: 11px 12px;
  color: #a52b36;
  background: #fff1f2;
  border: 1px solid #ffd1d5;
  border-radius: 6px;
  font-size: 13px;
  line-height: 1.5;
}

.submit-button {
  min-height: 48px;
  color: #fff;
  background: var(--accent);
  border: 1px solid var(--accent);
  border-radius: 8px;
  font-family: 'Be Vietnam Pro', sans-serif;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
  transition:
    background-color 0.2s,
    border-color 0.2s,
    transform 0.2s;
}

.submit-button:hover:not(:disabled) {
  background: var(--accent-dark);
  border-color: var(--accent-dark);
  transform: translateY(-1px);
}

.submit-button:disabled {
  opacity: 0.65;
  cursor: wait;
}

.register-link {
  min-height: 48px;
  display: grid;
  place-items: center;
  margin-top: 14px;
  color: var(--accent-dark);
  background: #fff;
  border: 1px solid var(--accent);
  border-radius: 8px;
  font-size: 16px;
  font-weight: 700;
  text-decoration: none;
  transition: background-color 0.2s;
}

.register-link:hover {
  background: var(--accent-soft);
}

.portal-switch {
  margin: 22px 0 0;
  color: #666;
  font-size: 13px;
  line-height: 1.6;
  text-align: center;
}

.portal-switch a {
  color: var(--accent-dark);
  font-weight: 700;
}

button:focus-visible,
a:focus-visible,
input:focus-visible {
  outline: 3px solid rgb(0 206 223 / 40%);
  outline-offset: 2px;
}

@media (max-width: 390px) {
  .login-page {
    place-items: start center;
    padding: 10px;
  }

  .login-card {
    margin-top: 10px;
    padding: 24px 18px;
  }

  .logo {
    width: 94px;
    height: 40px;
    margin-bottom: 14px;
  }

  h1 {
    font-size: 23px;
  }

  form {
    margin-top: 22px;
  }

  .portal-switch {
    margin-top: 18px;
  }
}
</style>
