<script setup lang="ts">
import { onBeforeUnmount, onMounted } from 'vue'
import type { CartItem } from '@/composables/useCart'

defineProps<{
  items: CartItem[]
  totalItems: number
  totalPrice: number
}>()

const emit = defineEmits<{
  close: []
  increment: [productId: number]
  decrement: [productId: number]
  remove: [productId: number]
  clear: []
}>()

let previousBodyOverflow = ''

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    emit('close')
  }
}

onMounted(() => {
  previousBodyOverflow = document.body.style.overflow
  document.body.style.overflow = 'hidden'
  document.addEventListener('keydown', handleKeydown)
})

onBeforeUnmount(() => {
  document.body.style.overflow = previousBodyOverflow
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div class="cart-overlay" @click.self="emit('close')">
    <aside class="cart-drawer" role="dialog" aria-modal="true" aria-labelledby="cart-title">
      <header class="cart-header">
        <div>
          <p>Giỏ hàng của bạn</p>
          <h2 id="cart-title">
            {{ totalItems > 0 ? `${totalItems} sản phẩm` : 'Chưa có sản phẩm' }}
          </h2>
        </div>

        <button type="button" aria-label="Đóng giỏ hàng" @click="emit('close')">×</button>
      </header>

      <div v-if="items.length === 0" class="cart-empty">
        <span aria-hidden="true"> </span>
        <h3>Giỏ hàng đang trống</h3>
        <p>Chọn một vài sản phẩm cần thiết cho hôm nay nhé.</p>
        <button type="button" @click="emit('close')">Tiếp tục mua sắm</button>
      </div>

      <template v-else>
        <ul class="cart-list" aria-label="Sản phẩm trong giỏ hàng">
          <li v-for="item in items" :key="item.product.id">
            <div class="cart-thumb" aria-hidden="true"></div>

            <div class="cart-item-info">
              <p>{{ item.product.productCode }}</p>
              <h3>{{ item.product.name }}</h3>
              <strong>{{ formatCurrency(item.product.price) }}</strong>

              <div class="cart-item-actions">
                <div class="quantity-control" aria-label="Điều chỉnh số lượng">
                  <button
                    type="button"
                    :aria-label="`Giảm số lượng ${item.product.name}`"
                    @click="emit('decrement', item.product.id)"
                  >
                    −
                  </button>
                  <span aria-live="polite">{{ item.quantity }}</span>
                  <button
                    type="button"
                    :disabled="item.quantity >= item.product.stockQuantity"
                    :aria-label="`Tăng số lượng ${item.product.name}`"
                    @click="emit('increment', item.product.id)"
                  >
                    +
                  </button>
                </div>

                <button
                  class="remove-item"
                  type="button"
                  :aria-label="`Xóa ${item.product.name} khỏi giỏ hàng`"
                  @click="emit('remove', item.product.id)"
                >
                  Xóa
                </button>
              </div>
            </div>
          </li>
        </ul>

        <footer class="cart-summary">
          <button class="clear-cart" type="button" @click="emit('clear')">Xóa giỏ hàng</button>
          <div>
            <span>Tạm tính</span>
            <strong>{{ formatCurrency(totalPrice) }}</strong>
          </div>
          <RouterLink class="confirm-cart" to="/checkout" @click="emit('close')">
            Xác nhận giỏ hàng
            <span aria-hidden="true">→</span>
          </RouterLink>
          <p>Bước tiếp theo: nhập thông tin nhận hàng và thanh toán COD.</p>
        </footer>
      </template>
    </aside>
  </div>
</template>

<style scoped>
.cart-overlay {
  position: fixed;
  inset: 0;
  z-index: 80;
  display: flex;
  justify-content: flex-end;
  background: rgba(13, 24, 40, 0.48);
  backdrop-filter: blur(3px);
  animation: overlay-in 180ms ease-out;
}

.cart-drawer {
  width: min(460px, 100%);
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  color: #122033;
  background: #e7e7e7;
  border-left: 3px solid #00cedf;
  border-radius: 10px 0 0 10px;
  box-shadow: -24px 0 60px rgba(13, 24, 40, 0.18);
  font-family: 'Be Vietnam Pro', sans-serif;
  animation: drawer-in 220ms ease-out;
}

.cart-drawer,
.cart-drawer * {
  box-sizing: border-box;
}

.cart-drawer button,
.cart-drawer a {
  font-family: inherit;
}

.cart-header {
  min-height: 92px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 20px;
  background: #fff;
  border-bottom: 1px solid #dce6ef;
}

.cart-header p,
.cart-header h2 {
  margin: 0;
}

.cart-header p {
  color: #0878f9;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.13em;
  text-transform: uppercase;
}

.cart-header h2 {
  margin-top: 6px;
  font-size: 24px;
  letter-spacing: -0.03em;
}

.cart-header > button {
  width: 44px;
  height: 44px;
  flex: 0 0 auto;
  color: #53657b;
  background: #edf2f6;
  border: 0;
  border-radius: 8px;
  font-size: 24px;
  cursor: pointer;
}

.cart-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  margin: 10px;
  padding: 40px 28px;
  background: #fff;
  border-radius: 10px;
  text-align: center;
}

.cart-empty > span {
  width: 78px;
  height: 78px;
  display: grid;
  place-items: center;
  background: #f3f4f6;
  border: 1px solid #d9dee5;
  border-radius: 8px;
}

.cart-empty h3 {
  margin: 22px 0 8px;
  font-size: 20px;
}

.cart-empty p {
  max-width: 270px;
  margin: 0;
  color: #64748b;
  font-size: 16px;
  line-height: 1.65;
}

.cart-empty button {
  min-height: 44px;
  margin-top: 24px;
  padding: 0 20px;
  color: #fff;
  background: #0878f9;
  border: 0;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
}

.cart-list {
  flex: 1;
  min-height: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 10px;
  overflow-y: auto;
  list-style: none;
}

.cart-list li {
  display: grid;
  grid-template-columns: 76px minmax(0, 1fr);
  gap: 14px;
  padding: 14px;
  background: #fff;
  border: 1px solid #dce6ef;
  border-radius: 10px;
}

.cart-thumb {
  width: 76px;
  height: 84px;
  background: #f3f4f6;
  border: 1px solid #d9dee5;
  border-radius: 8px;
}

.cart-item-info {
  min-width: 0;
}

.cart-item-info > p,
.cart-item-info h3 {
  margin: 0;
}

.cart-item-info > p {
  color: #8292a5;
  font-size: 9px;
  font-weight: 800;
  letter-spacing: 0.1em;
}

.cart-item-info h3 {
  margin: 4px 0 5px;
  overflow: hidden;
  font-size: 16px;
  line-height: 1.4;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cart-item-info > strong {
  color: #0878f9;
  font-size: 16px;
}

.cart-item-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 11px;
}

.quantity-control {
  display: inline-grid;
  grid-template-columns: 38px 40px 38px;
  align-items: center;
  overflow: hidden;
  background: #fff;
  border: 1px solid #dce6ef;
  border-radius: 8px;
}

.quantity-control button {
  height: 40px;
  color: #42526a;
  background: transparent;
  border: 0;
  font-size: 18px;
  cursor: pointer;
}

.quantity-control button:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.quantity-control span {
  font-size: 16px;
  font-weight: 800;
  text-align: center;
}

.remove-item,
.clear-cart {
  padding: 0;
  color: #a55757;
  background: transparent;
  border: 0;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
}

.cart-summary {
  position: relative;
  padding: 20px;
  background: #fff;
  border-top: 1px solid #dce6ef;
}

.cart-summary > div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
}

.cart-summary span {
  color: #64748b;
  font-size: 16px;
}

.cart-summary strong {
  color: #0878f9;
  font-size: 22px;
}

.cart-summary p {
  margin: 10px 0 0;
  color: #8292a5;
  font-size: 12px;
  text-align: right;
}

.confirm-cart {
  width: 100%;
  min-height: 50px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-top: 18px;
  padding: 0 18px;
  color: #fff;
  background: #0878f9;
  border: 0;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 800;
  cursor: pointer;
  text-decoration: none;
  box-shadow: 0 12px 26px rgba(8, 120, 249, 0.2);
}

.confirm-cart span {
  color: inherit;
  font-size: 18px;
}

.clear-cart {
  display: block;
  margin: 0 0 14px auto;
}

button:focus-visible,
a:focus-visible {
  outline: 3px solid rgba(0, 206, 223, 0.45);
  outline-offset: 3px;
}

@keyframes overlay-in {
  from {
    background: rgba(13, 24, 40, 0);
  }
}

@keyframes drawer-in {
  from {
    transform: translateX(100%);
  }
}

@media (max-width: 480px) {
  .cart-drawer {
    width: 100%;
    border-left: 0;
    border-radius: 0;
  }

  .cart-header {
    min-height: 80px;
    padding: 16px;
  }

  .cart-list {
    padding: 8px;
  }

  .cart-list li {
    grid-template-columns: 62px minmax(0, 1fr);
    gap: 12px;
    padding: 12px;
  }

  .cart-thumb {
    width: 62px;
    height: 70px;
  }

  .cart-item-actions {
    align-items: flex-start;
    flex-wrap: wrap;
  }

  .quantity-control {
    grid-template-columns: 36px 38px 36px;
  }

  .cart-summary {
    padding: 16px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .cart-overlay,
  .cart-drawer {
    animation: none;
  }
}
</style>
