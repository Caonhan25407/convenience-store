<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { createOrder } from '@/services/orderService'
import { useCart } from '@/composables/useCart'
import type { OrderResponse } from '@/types/order'

const logoUrl = `${import.meta.env.BASE_URL}logo.png`
const { cartItems, totalItems, totalPrice, clearCart } = useCart()

const submitting = ref(false)
const errorMessage = ref('')
const completedOrder = ref<OrderResponse | null>(null)

const form = reactive({
  customerName: '',
  phone: '',
  deliveryAddress: '',
  paymentMethod: 'COD' as const,
})

const fieldErrors = reactive({
  customerName: '',
  phone: '',
  deliveryAddress: '',
})

const itemSummary = computed(() =>
  cartItems.value.map((item) => ({
    ...item,
    lineTotal: item.product.price * item.quantity,
  })),
)

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function productInitials(name: string) {
  return name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word.charAt(0))
    .join('')
    .toUpperCase()
}

function normalizePhone(value: string) {
  return value.replace(/[\s.-]/g, '')
}

function validateForm() {
  fieldErrors.customerName = ''
  fieldErrors.phone = ''
  fieldErrors.deliveryAddress = ''

  const customerName = form.customerName.trim()
  const phone = normalizePhone(form.phone)
  const deliveryAddress = form.deliveryAddress.trim()

  if (customerName.length < 2) {
    fieldErrors.customerName = 'Vui lòng nhập tên người nhận'
  }

  if (!/^(?:0\d{9}|\+84\d{9})$/.test(phone)) {
    fieldErrors.phone = 'Nhập 10 số bắt đầu bằng 0 hoặc số có mã +84'
  }

  if (deliveryAddress.length < 8) {
    fieldErrors.deliveryAddress = 'Vui lòng nhập địa chỉ giao hàng đầy đủ'
  }

  return !Object.values(fieldErrors).some(Boolean)
}

async function handleSubmit() {
  if (submitting.value) {
    return
  }

  errorMessage.value = ''

  if (!validateForm()) {
    return
  }

  if (cartItems.value.length === 0) {
    errorMessage.value = 'Giỏ hàng đang trống'
    return
  }

  try {
    submitting.value = true

    const result = await createOrder({
      customerName: form.customerName.trim(),
      phone: normalizePhone(form.phone),
      deliveryAddress: form.deliveryAddress.trim(),
      paymentMethod: 'COD',
      items: cartItems.value.map((item) => ({
        productId: item.product.id,
        quantity: item.quantity,
      })),
    })

    completedOrder.value = result
    clearCart()
    window.scrollTo({ top: 0, behavior: 'smooth' })
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : 'Không thể hoàn tất đơn hàng lúc này'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="checkout-shell">
    <header class="checkout-header">
      <div>
        <RouterLink to="/store" class="checkout-brand" aria-label="Quay lại cửa hàng CN25">
          <img :src="logoUrl" alt="CN25" />
        </RouterLink>
        <span>Thanh toán an toàn</span>
      </div>
    </header>

    <main>
      <section v-if="completedOrder" class="success-card" aria-labelledby="success-title">
        <span class="success-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" />
            <path d="m7.5 12 3 3 6-6" />
          </svg>
        </span>
        <p class="success-kicker">Đặt hàng thành công</p>
        <h1 id="success-title">Cảm ơn bạn đã mua hàng!</h1>
        <p class="success-copy">
          Đơn hàng đã được ghi nhận. Cửa hàng sẽ liên hệ xác nhận trước khi giao.
        </p>

        <dl class="order-result">
          <div>
            <dt>Mã đơn hàng</dt>
            <dd>{{ completedOrder.orderCode }}</dd>
          </div>
          <div>
            <dt>Thanh toán</dt>
            <dd>COD · Khi nhận hàng</dd>
          </div>
          <div>
            <dt>Tổng tiền</dt>
            <dd>{{ formatCurrency(completedOrder.totalAmount) }}</dd>
          </div>
        </dl>

        <RouterLink class="back-store-button" to="/store"> Tiếp tục mua sắm </RouterLink>
      </section>

      <section v-else-if="cartItems.length === 0" class="empty-checkout">
        <span aria-hidden="true">
          <svg viewBox="0 0 24 24">
            <path d="M5 8h14l-1 13H6L5 8Z" />
            <path d="M9 10V6a3 3 0 0 1 6 0v4" />
          </svg>
        </span>
        <h1>Giỏ hàng đang trống</h1>
        <p>Hãy chọn sản phẩm trước khi tiến hành đặt hàng.</p>
        <RouterLink to="/store">Quay lại cửa hàng</RouterLink>
      </section>

      <div v-else class="checkout-layout">
        <section class="checkout-form-card" aria-labelledby="checkout-title">
          <RouterLink class="back-link" to="/store">← Quay lại cửa hàng</RouterLink>
          <p class="section-kicker">Bước cuối cùng</p>
          <h1 id="checkout-title">Thông tin nhận hàng</h1>
          <p class="form-description">
            Điền thông tin bên dưới để cửa hàng chuẩn bị và giao đúng đơn hàng của bạn.
          </p>

          <form novalidate @submit.prevent="handleSubmit">
            <div class="form-field">
              <label for="customer-name">Tên người nhận <span>*</span></label>
              <input
                id="customer-name"
                v-model.trim="form.customerName"
                type="text"
                maxlength="100"
                autocomplete="name"
                placeholder="Ví dụ: Nguyễn Văn An"
                :aria-invalid="Boolean(fieldErrors.customerName)"
                :aria-describedby="fieldErrors.customerName ? 'customer-name-error' : undefined"
              />
              <p v-if="fieldErrors.customerName" id="customer-name-error" class="field-error">
                {{ fieldErrors.customerName }}
              </p>
            </div>

            <div class="form-field">
              <label for="customer-phone">Số điện thoại <span>*</span></label>
              <input
                id="customer-phone"
                v-model="form.phone"
                type="tel"
                maxlength="15"
                inputmode="tel"
                autocomplete="tel"
                placeholder="Ví dụ: 0912345678"
                :aria-invalid="Boolean(fieldErrors.phone)"
                :aria-describedby="fieldErrors.phone ? 'customer-phone-error' : undefined"
              />
              <p v-if="fieldErrors.phone" id="customer-phone-error" class="field-error">
                {{ fieldErrors.phone }}
              </p>
            </div>

            <div class="form-field form-field-wide">
              <label for="delivery-address">Địa chỉ giao hàng <span>*</span></label>
              <textarea
                id="delivery-address"
                v-model.trim="form.deliveryAddress"
                rows="4"
                maxlength="500"
                autocomplete="street-address"
                placeholder="Số nhà, tên đường, phường/xã, quận/huyện, tỉnh/thành phố"
                :aria-invalid="Boolean(fieldErrors.deliveryAddress)"
                :aria-describedby="
                  fieldErrors.deliveryAddress ? 'delivery-address-error' : undefined
                "
              ></textarea>
              <p v-if="fieldErrors.deliveryAddress" id="delivery-address-error" class="field-error">
                {{ fieldErrors.deliveryAddress }}
              </p>
            </div>

            <fieldset class="payment-fieldset">
              <legend>Hình thức thanh toán</legend>
              <label class="payment-option">
                <input v-model="form.paymentMethod" type="radio" value="COD" />
                <span class="payment-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24">
                    <rect x="3" y="6" width="18" height="12" rx="2" />
                    <path d="M7 10h4M7 14h2M17 9v6" />
                  </svg>
                </span>
                <span>
                  <strong>Thanh toán khi nhận hàng (COD)</strong>
                  <small>Trả tiền mặt sau khi nhận và kiểm tra hàng</small>
                </span>
              </label>
            </fieldset>

            <p v-if="errorMessage" class="submit-error" role="alert">
              {{ errorMessage }}
            </p>

            <button class="place-order-button" type="submit" :disabled="submitting">
              <span>{{ submitting ? 'Đang tạo đơn hàng...' : 'Đặt hàng' }}</span>
              <strong v-if="!submitting">{{ formatCurrency(totalPrice) }}</strong>
            </button>
          </form>
        </section>

        <aside class="order-summary" aria-labelledby="summary-title">
          <div class="summary-heading">
            <div>
              <p>Đơn hàng của bạn</p>
              <h2 id="summary-title">{{ totalItems }} sản phẩm</h2>
            </div>
            <RouterLink to="/store">Chỉnh sửa</RouterLink>
          </div>

          <ul>
            <li v-for="item in itemSummary" :key="item.product.id">
              <span class="summary-thumb" aria-hidden="true">
                {{ productInitials(item.product.name) }}
                <em>{{ item.quantity }}</em>
              </span>
              <span class="summary-item-copy">
                <strong>{{ item.product.name }}</strong>
                <small>{{ item.product.productCode }}</small>
              </span>
              <strong>{{ formatCurrency(item.lineTotal) }}</strong>
            </li>
          </ul>

          <dl class="summary-totals">
            <div>
              <dt>Tạm tính</dt>
              <dd>{{ formatCurrency(totalPrice) }}</dd>
            </div>
            <div>
              <dt>Phí giao hàng</dt>
              <dd>Miễn phí</dd>
            </div>
            <div class="grand-total">
              <dt>Tổng cộng</dt>
              <dd>{{ formatCurrency(totalPrice) }}</dd>
            </div>
          </dl>

          <p class="cod-note">Bạn chỉ thanh toán khi đã nhận được hàng.</p>
        </aside>
      </div>
    </main>
  </div>
</template>

<style scoped>
/* ProductLayout visual system */
:global(*) {
  box-sizing: border-box;
}

:global(body) {
  margin: 0;
  min-width: 320px;
  overflow-x: hidden;
  background: #e7e7e7;
  font-family: 'Be Vietnam Pro', sans-serif;
}

:global(button),
:global(input),
:global(textarea) {
  font: inherit;
}

.checkout-shell {
  --navy: #0d1828;
  --blue: #0878f9;
  --cyan: #00cedf;
  --ink: #122033;
  --muted: #64748b;
  --line: #dce6ef;
  min-height: 100vh;
  min-width: 0;
  overflow-x: clip;
  color: var(--ink);
  background: #e7e7e7;
  font-family: 'Be Vietnam Pro', sans-serif;
}

.checkout-header {
  background: var(--navy);
  border-bottom: 3px solid var(--cyan);
  backdrop-filter: none;
}

.checkout-header > div {
  width: min(1180px, calc(100% - 32px));
  min-height: 72px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 0 auto;
}

.checkout-brand {
  display: inline-flex;
}

.checkout-brand img {
  width: 88px;
  height: 38px;
  object-fit: contain;
}

.checkout-header span {
  color: #d7e2ee;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.checkout-shell main {
  width: min(1180px, calc(100% - 20px));
  margin: 0 auto;
  padding: 10px 0 40px;
}

.checkout-layout {
  display: grid;
  grid-template-columns: minmax(0, 1.35fr) minmax(330px, 0.65fr);
  align-items: start;
  gap: 10px;
}

.checkout-form-card,
.order-summary,
.success-card,
.empty-checkout {
  min-width: 0;
  background: #fff;
  border: 0;
  border-radius: 10px;
  box-shadow: none;
}

.checkout-form-card {
  padding: 20px;
}

.back-link {
  display: inline-flex;
  margin-bottom: 22px;
  color: var(--blue);
  font-size: 14px;
  font-weight: 700;
  text-decoration: none;
}

.section-kicker,
.success-kicker {
  margin: 0 0 8px;
  color: var(--blue);
  font-size: 12px;
  font-weight: 900;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

.checkout-form-card h1,
.success-card h1,
.empty-checkout h1 {
  margin: 0;
  color: #172435;
  font-size: 24px;
  letter-spacing: -0.025em;
}

.form-description,
.success-copy,
.empty-checkout p {
  max-width: 570px;
  margin: 10px 0 30px;
  color: var(--muted);
  font-size: 16px;
  line-height: 1.6;
}

.checkout-form-card form {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.form-field {
  min-width: 0;
}

.form-field-wide,
.payment-fieldset,
.submit-error,
.place-order-button {
  grid-column: 1 / -1;
}

.form-field label,
.payment-fieldset legend {
  display: block;
  margin-bottom: 7px;
  color: #34465c;
  font-size: 14px;
  font-weight: 800;
}

.form-field label span {
  color: #e15b64;
}

.form-field input,
.form-field textarea {
  width: 100%;
  min-height: 46px;
  padding: 10px 12px;
  color: var(--ink);
  background: #fff;
  border: 1px solid #c8d2dc;
  border-radius: 6px;
  outline: 0;
  font-size: 16px;
  transition:
    border-color 150ms ease,
    box-shadow 150ms ease;
}

.form-field textarea {
  min-height: 112px;
  resize: vertical;
  line-height: 1.55;
}

.form-field input:focus,
.form-field textarea:focus {
  border-color: var(--blue);
  box-shadow: 0 0 0 3px rgba(8, 120, 249, 0.12);
}

.form-field input[aria-invalid='true'],
.form-field textarea[aria-invalid='true'] {
  border-color: #d95f68;
}

.field-error {
  margin: 6px 0 0;
  color: #bb4650;
  font-size: 13px;
  line-height: 1.45;
}

.payment-fieldset {
  min-width: 0;
  margin-top: 2px;
  padding: 0;
  border: 0;
}

.payment-option {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px;
  background: #eef7ff;
  border: 1px solid #a9cff8;
  border-radius: 8px;
  cursor: pointer;
}

.payment-option input {
  flex: 0 0 auto;
  width: 18px;
  height: 18px;
  accent-color: var(--blue);
}

.payment-icon {
  flex: 0 0 auto;
  width: 40px;
  height: 40px;
  display: grid;
  place-items: center;
  color: var(--blue);
  background: #fff;
  border-radius: 8px;
}

.payment-icon svg {
  width: 22px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.payment-option > span:last-child {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.payment-option strong {
  font-size: 14px;
  line-height: 1.45;
}

.payment-option small {
  color: var(--muted);
  font-size: 13px;
  line-height: 1.45;
}

.submit-error {
  margin: 0;
  padding: 12px 14px;
  color: #a9434c;
  background: #fff1f2;
  border: 1px solid #ffd0d3;
  border-radius: 8px;
  font-size: 14px;
  line-height: 1.5;
}

.place-order-button {
  min-width: 0;
  min-height: 50px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  padding: 0 18px;
  color: #fff;
  background: var(--blue);
  border: 0;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 800;
  cursor: pointer;
  box-shadow: none;
}

.place-order-button:disabled {
  opacity: 0.65;
  cursor: wait;
}

.place-order-button:not(:disabled):hover {
  background: #006be2;
}

.place-order-button strong {
  white-space: nowrap;
}

.order-summary {
  position: sticky;
  top: 10px;
  overflow: hidden;
}

.summary-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 20px;
  border-bottom: 1px solid var(--line);
}

.summary-heading p,
.summary-heading h2 {
  margin: 0;
}

.summary-heading p,
.summary-heading a {
  font-size: 13px;
}

.summary-heading h2 {
  margin-top: 4px;
  font-size: 20px;
}

.summary-heading p {
  color: var(--muted);
}

.summary-heading a {
  color: var(--blue);
  font-weight: 800;
}

.order-summary ul {
  max-height: 350px;
  margin: 0;
  padding: 8px 20px;
  overflow-y: auto;
  list-style: none;
}

.order-summary li {
  display: grid;
  grid-template-columns: 50px minmax(0, 1fr) auto;
  align-items: center;
  gap: 10px;
  padding: 14px 0;
  border-bottom: 1px solid #edf1f5;
}

.summary-thumb {
  position: relative;
  width: 48px;
  height: 52px;
  display: grid;
  place-items: center;
  color: #fff;
  background: linear-gradient(145deg, var(--cyan), var(--blue));
  border-radius: 8px;
  font-size: 14px;
  font-weight: 900;
}

.summary-thumb em {
  position: absolute;
  top: -7px;
  right: -7px;
  min-width: 19px;
  height: 19px;
  display: grid;
  place-items: center;
  padding: 0 4px;
  color: #fff;
  background: var(--navy);
  border: 2px solid #fff;
  border-radius: 999px;
  font-size: 9px;
  font-style: normal;
}

.summary-item-copy {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.summary-item-copy strong,
.order-summary li > strong,
.summary-totals div {
  font-size: 14px;
}

.summary-item-copy strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.summary-item-copy small {
  color: #8292a5;
}

.order-summary li > strong {
  white-space: nowrap;
}

.summary-item-copy small,
.cod-note {
  font-size: 12px;
}

.summary-totals {
  margin: 0;
  padding: 18px 20px 20px;
  background: #fbfdff;
}

.summary-totals div {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 10px;
  color: var(--muted);
}

.summary-totals dt,
.summary-totals dd {
  margin: 0;
}

.summary-totals .grand-total {
  align-items: center;
  margin: 15px 0 0;
  padding-top: 15px;
  color: var(--ink);
  border-top: 1px solid var(--line);
  font-size: 16px;
  font-weight: 800;
}

.grand-total dd {
  color: var(--blue);
  font-size: 20px;
}

.cod-note {
  margin: 0;
  padding: 0 20px 20px;
  color: #728399;
  background: #fbfdff;
  line-height: 1.5;
  text-align: center;
}

.success-card,
.empty-checkout {
  max-width: 760px;
  margin: 0 auto;
  padding: 48px 32px;
  text-align: center;
}

.success-icon,
.empty-checkout > span {
  width: 72px;
  height: 72px;
  display: inline-grid;
  place-items: center;
  margin-bottom: 20px;
  color: #14a36f;
  background: #e8fbf3;
  border-radius: 50%;
}

.success-icon svg,
.empty-checkout svg {
  width: 43px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.7;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.success-copy,
.empty-checkout p {
  margin-right: auto;
  margin-left: auto;
}

.order-result {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  margin: 28px 0;
  padding: 20px 0;
  background: #f7fafc;
  border: 1px solid var(--line);
  border-radius: 8px;
}

.order-result div {
  padding: 0 16px;
  border-right: 1px solid var(--line);
}

.order-result div:last-child {
  border-right: 0;
}

.order-result dt {
  color: var(--muted);
  font-size: 12px;
  text-transform: uppercase;
}

.order-result dd {
  margin: 7px 0 0;
  font-size: 16px;
  font-weight: 900;
}

.back-store-button,
.empty-checkout a {
  min-height: 46px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0 20px;
  color: #fff;
  background: var(--blue);
  border-radius: 8px;
  font-size: 15px;
  font-weight: 800;
  text-decoration: none;
}

.empty-checkout > span {
  color: var(--blue);
  background: #eaf5ff;
}

.empty-checkout a {
  margin-top: 26px;
}

button:focus-visible,
a:focus-visible,
input:focus-visible,
textarea:focus-visible {
  outline: 3px solid rgba(0, 206, 223, 0.45);
  outline-offset: 3px;
}

@media (max-width: 850px) {
  .checkout-layout {
    grid-template-columns: minmax(0, 1fr);
  }

  .order-summary {
    position: static;
    grid-row: auto;
  }
}

@media (max-width: 600px) {
  .checkout-header > div {
    width: calc(100% - 24px);
    min-height: 62px;
  }

  .checkout-header span {
    font-size: 10px;
    letter-spacing: 0.04em;
  }

  .checkout-shell main {
    width: calc(100% - 16px);
    padding: 8px 0 28px;
  }

  .checkout-layout {
    gap: 8px;
  }

  .checkout-form-card {
    padding: 16px;
    border-radius: 10px;
  }

  .checkout-form-card form {
    grid-template-columns: minmax(0, 1fr);
  }

  .form-field,
  .payment-fieldset,
  .submit-error,
  .place-order-button {
    grid-column: 1;
  }

  .back-link {
    margin-bottom: 18px;
  }

  .checkout-form-card h1,
  .success-card h1,
  .empty-checkout h1 {
    font-size: 22px;
  }

  .form-description {
    margin-bottom: 24px;
  }

  .payment-option {
    align-items: flex-start;
    padding: 12px;
  }

  .payment-option input {
    margin-top: 10px;
  }

  .summary-heading,
  .summary-totals {
    padding-right: 16px;
    padding-left: 16px;
  }

  .order-summary ul {
    padding-right: 16px;
    padding-left: 16px;
  }

  .order-summary li {
    grid-template-columns: 44px minmax(0, 1fr);
  }

  .summary-thumb {
    width: 42px;
    height: 46px;
  }

  .order-summary li > strong {
    grid-column: 2;
    color: var(--blue);
  }

  .cod-note {
    padding-right: 16px;
    padding-left: 16px;
  }

  .success-card,
  .empty-checkout {
    width: 100%;
    padding: 36px 16px;
  }

  .order-result {
    grid-template-columns: minmax(0, 1fr);
    gap: 0;
  }

  .order-result div {
    padding: 14px 12px;
    border-right: 0;
    border-bottom: 1px solid var(--line);
  }

  .order-result div:last-child {
    border-bottom: 0;
  }
}

@media (max-width: 360px) {
  .checkout-header span {
    display: none;
  }

  .payment-icon {
    display: none;
  }

  .place-order-button {
    align-items: flex-start;
    flex-direction: column;
    justify-content: center;
    gap: 2px;
    padding-top: 8px;
    padding-bottom: 8px;
  }
}

@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    scroll-behavior: auto !important;
    transition-duration: 0.01ms !important;
  }
}
</style>
