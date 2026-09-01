<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { resolveSafeRedirect } from '@/app/router/authGuard'
import { useAuthStore } from '@/stores/auth'

const logoUrl = `${import.meta.env.BASE_URL}logo.png`
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const submitting = ref(false)
const submitError = ref('')
const form = reactive({
  displayName: '',
  email: '',
  password: '',
  confirmPassword: '',
})
const fieldErrors = reactive({
  displayName: '',
  email: '',
  password: '',
  confirmPassword: '',
})

function clearError(field: keyof typeof fieldErrors) {
  fieldErrors[field] = ''
  submitError.value = ''
}

function clearPasswordErrors() {
  clearError('password')
  fieldErrors.confirmPassword = ''
}

function validateForm() {
  const displayNameLength = form.displayName.trim().length

  fieldErrors.displayName =
    displayNameLength === 0
      ? 'Vui lòng nhập tên hiển thị'
      : displayNameLength < 2
        ? 'Tên hiển thị phải có ít nhất 2 ký tự'
        : ''
  fieldErrors.email = !form.email
    ? 'Vui lòng nhập email'
    : /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)
      ? ''
      : 'Email chưa đúng định dạng'
  fieldErrors.password = !form.password
    ? 'Vui lòng nhập mật khẩu'
    : form.password.length < 8
      ? 'Mật khẩu phải có ít nhất 8 ký tự'
      : ''
  fieldErrors.confirmPassword = !form.confirmPassword
    ? 'Vui lòng xác nhận mật khẩu'
    : form.confirmPassword !== form.password
      ? 'Mật khẩu xác nhận không khớp'
      : ''

  return Object.values(fieldErrors).every((error) => !error)
}

async function handleSubmit() {
  submitError.value = ''

  if (!validateForm() || submitting.value) {
    return
  }

  submitting.value = true

  try {
    await auth.registerCustomer({
      displayName: form.displayName,
      email: form.email,
      password: form.password,
    })

    await router.replace(resolveSafeRedirect(route.query.redirect, 'CUSTOMER', router))
  } catch (error) {
    submitError.value = error instanceof Error ? error.message : 'Không thể đăng ký lúc này'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="register-page">
    <section class="register-card" aria-labelledby="register-title">
      <img class="logo" :src="logoUrl" alt="CN25" />
      <p class="role-label">Cửa hàng CN25</p>
      <h1 id="register-title">Đăng ký tài khoản</h1>

      <form novalidate :aria-busy="submitting" @submit.prevent="handleSubmit">
        <div class="form-field">
          <label for="register-name">Tên hiển thị</label>
          <input
            id="register-name"
            v-model.trim="form.displayName"
            type="text"
            maxlength="150"
            autocomplete="name"
            placeholder="Nguyễn Văn An"
            :aria-invalid="Boolean(fieldErrors.displayName)"
            :aria-describedby="fieldErrors.displayName ? 'register-name-error' : undefined"
            @input="clearError('displayName')"
          />
          <p v-if="fieldErrors.displayName" id="register-name-error" class="field-error">
            {{ fieldErrors.displayName }}
          </p>
        </div>

        <div class="form-field">
          <label for="register-email">Email</label>
          <input
            id="register-email"
            v-model.trim="form.email"
            type="email"
            maxlength="254"
            autocomplete="username"
            inputmode="email"
            placeholder="tenban@example.com"
            :aria-invalid="Boolean(fieldErrors.email)"
            :aria-describedby="fieldErrors.email ? 'register-email-error' : undefined"
            @input="clearError('email')"
          />
          <p v-if="fieldErrors.email" id="register-email-error" class="field-error">
            {{ fieldErrors.email }}
          </p>
        </div>

        <div class="form-field">
          <label for="register-password">Mật khẩu</label>
          <input
            id="register-password"
            v-model="form.password"
            type="password"
            autocomplete="new-password"
            placeholder="Tối thiểu 8 ký tự"
            :aria-invalid="Boolean(fieldErrors.password)"
            :aria-describedby="
              fieldErrors.password ? 'register-password-error' : 'register-password-help'
            "
            @input="clearPasswordErrors"
          />
          <p v-if="fieldErrors.password" id="register-password-error" class="field-error">
            {{ fieldErrors.password }}
          </p>
          <p v-else id="register-password-help" class="field-help">Tối thiểu 8 ký tự</p>
        </div>

        <div class="form-field">
          <label for="register-confirm-password">Xác nhận mật khẩu</label>
          <input
            id="register-confirm-password"
            v-model="form.confirmPassword"
            type="password"
            autocomplete="new-password"
            placeholder="Nhập lại mật khẩu"
            :aria-invalid="Boolean(fieldErrors.confirmPassword)"
            :aria-describedby="fieldErrors.confirmPassword ? 'register-confirm-error' : undefined"
            @input="clearError('confirmPassword')"
          />
          <p v-if="fieldErrors.confirmPassword" id="register-confirm-error" class="field-error">
            {{ fieldErrors.confirmPassword }}
          </p>
        </div>

        <p v-if="submitError" class="submit-error" role="alert">
          {{ submitError }}
        </p>

        <button class="submit-button" type="submit" :disabled="submitting">
          {{ submitting ? 'Đang đăng ký...' : 'Đăng ký' }}
        </button>
      </form>

      <p class="login-link">
        Đã có tài khoản?
        <RouterLink to="/login">Đăng nhập</RouterLink>
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

.register-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 20px;
  color: #202020;
  background: #e7e7e7;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.register-card {
  position: relative;
  width: min(440px, 100%);
  padding: 30px;
  overflow: hidden;
  background: #fff;
  border: 1px solid #d5d5d5;
  border-radius: 10px;
  box-shadow: 0 8px 24px rgb(0 0 0 / 7%);
}

.register-card::before {
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
  color: #0878f9;
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
  border-color: #0878f9;
  box-shadow: 0 0 0 3px rgb(8 120 249 / 12%);
}

.form-field input[aria-invalid='true'] {
  border-color: #dc3545;
}

.field-error,
.field-help {
  margin: 7px 0 0;
  font-size: 12px;
  line-height: 1.45;
}

.field-error {
  color: #b4232f;
}

.field-help {
  color: #666;
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
  background: #0878f9;
  border: 1px solid #0878f9;
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
  background: #0062cc;
  border-color: #0062cc;
  transform: translateY(-1px);
}

.submit-button:disabled {
  opacity: 0.65;
  cursor: wait;
}

.login-link {
  margin: 22px 0 0;
  color: #666;
  font-size: 13px;
  line-height: 1.6;
  text-align: center;
}

.login-link a {
  color: #0062cc;
  font-weight: 700;
}

button:focus-visible,
a:focus-visible,
input:focus-visible {
  outline: 3px solid rgb(0 206 223 / 40%);
  outline-offset: 2px;
}

@media (max-width: 390px) {
  .register-page {
    place-items: start center;
    padding: 10px;
  }

  .register-card {
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

  .login-link {
    margin-top: 18px;
  }
}
</style>
