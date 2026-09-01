<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { getProducts } from '@/services/productService'
import { useCart } from '@/composables/useCart'
import type { Product } from '@/types/product'
import CartDrawer from '../../component/CartDrawer.vue'
import StoreAccountControls from '../../component/StoreAccountControls.vue'

const logoUrl = `${import.meta.env.BASE_URL}logo.png`
const products = ref<Product[]>([])
const loading = ref(false)
const errorMessage = ref('')
const searchQuery = ref('')
const stockStatus = ref('all')
const page = ref(1)
const pageSize = 12
const totalCount = ref(0)
const totalPages = ref(0)
const isCartOpen = ref(false)

const {
  cartItems,
  totalItems,
  totalPrice,
  quantityInCart,
  addToCart,
  increment,
  decrement,
  removeFromCart,
  clearCart,
  syncProducts,
} = useCart()

let searchTimer: ReturnType<typeof setTimeout> | undefined
let latestRequest = 0

const visiblePages = computed(() => {
  if (totalPages.value <= 1) {
    return []
  }

  const lastStart = Math.max(1, totalPages.value - 4)
  const start = Math.max(1, Math.min(page.value - 2, lastStart))
  const end = Math.min(totalPages.value, start + 4)

  return Array.from({ length: end - start + 1 }, (_, index) => start + index)
})

const resultLabel = computed(() => {
  if (loading.value) {
    return 'Đang cập nhật danh sách sản phẩm'
  }

  if (totalCount.value === 0) {
    return 'Chưa có sản phẩm phù hợp'
  }

  return `${totalCount.value} sản phẩm`
})

async function loadProducts(targetPage = page.value) {
  const requestId = ++latestRequest

  try {
    loading.value = true
    errorMessage.value = ''
    page.value = targetPage

    const result = await getProducts({
      page: targetPage,
      pageSize,
      search: searchQuery.value.trim(),
      stockStatus: stockStatus.value,
    })

    if (requestId !== latestRequest) {
      return
    }

    products.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
    syncProducts(result.items)
  } catch (error) {
    if (requestId !== latestRequest) {
      return
    }

    products.value = []
    totalCount.value = 0
    totalPages.value = 0
    errorMessage.value =
      error instanceof Error ? error.message : 'Không thể tải danh sách sản phẩm lúc này'
  } finally {
    if (requestId === latestRequest) {
      loading.value = false
    }
  }
}

function changePage(nextPage: number) {
  if (nextPage < 1 || nextPage > totalPages.value || nextPage === page.value || loading.value) {
    return
  }

  void loadProducts(nextPage)
  document.querySelector('#products')?.scrollIntoView({ behavior: 'smooth' })
}

function clearSearch() {
  searchQuery.value = ''
}

function handleAddToCart(product: Product) {
  if (addToCart(product)) {
    isCartOpen.value = true
  }
}

function isCartLimitReached(product: Product) {
  return quantityInCart(product.id) >= product.stockQuantity
}

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

function productTone(product: Product) {
  return `tone-${Math.abs(product.id) % 4}`
}

watch(searchQuery, () => {
  if (searchTimer) {
    clearTimeout(searchTimer)
  }

  searchTimer = setTimeout(() => {
    page.value = 1
    void loadProducts(1)
  }, 350)
})

watch(stockStatus, () => {
  page.value = 1
  void loadProducts(1)
})

onMounted(() => {
  void loadProducts()
})

onBeforeUnmount(() => {
  if (searchTimer) {
    clearTimeout(searchTimer)
  }

  latestRequest++
})
</script>

<template>
  <div class="store-shell">
    <a class="skip-link" href="#products">Đi đến danh sách sản phẩm</a>

    <header class="store-header">
      <div class="header-inner">
        <RouterLink class="brand" to="/store" aria-label="CN25 - Trang chủ cửa hàng">
          <img :src="logoUrl" alt="CN25" />
          <span class="brand-copy">
            <strong>CN25</strong>
            <small>Cửa hàng tiện lợi</small>
          </span>
        </RouterLink>

        <nav class="store-nav" aria-label="Điều hướng chính">
          <a href="#products">Sản phẩm</a>
        </nav>

        <div class="account-slot">
          <StoreAccountControls @logout="clearCart" />
        </div>

        <button
          class="cart-trigger"
          type="button"
          :aria-label="`Mở giỏ hàng, ${totalItems} sản phẩm`"
          @click="isCartOpen = true"
        >
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="M5 8h14l-1 13H6L5 8Z" />
            <path d="M9 10V6a3 3 0 0 1 6 0v4" />
          </svg>
          <span>Giỏ hàng</span>
          <em v-if="totalItems > 0">{{ totalItems > 99 ? '99+' : totalItems }}</em>
        </button>
      </div>
    </header>

    <main>
      <section class="hero" aria-labelledby="hero-title">
        <div class="hero-inner">
          <div class="hero-copy">
            <p class="eyebrow">
              <span></span>
              Tiện lợi mỗi ngày
            </p>

            <h1 id="hero-title">
              Món bạn cần,
              <span>luôn thật gần.</span>
            </h1>

            <p class="hero-description">
              Khám phá những sản phẩm thiết yếu với giá rõ ràng và tình trạng hàng hóa được cập nhật
              trực tiếp từ cửa hàng.
            </p>

            <div class="hero-actions">
              <a class="primary-action" href="#products">Xem sản phẩm</a>
            </div>
          </div>
        </div>
      </section>

      <section id="products" class="catalog" aria-labelledby="catalog-title">
        <div class="catalog-inner">
          <div class="section-heading">
            <div>
              <p class="section-kicker">Danh mục cửa hàng</p>
              <h2 id="catalog-title">Sản phẩm dành cho bạn</h2>
            </div>

            <p class="result-count" aria-live="polite">{{ resultLabel }}</p>
          </div>

          <div class="catalog-toolbar" role="search">
            <div class="search-field">
              <label class="sr-only" for="store-search">Tìm kiếm sản phẩm</label>
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <circle cx="11" cy="11" r="7" />
                <path d="m16 16 4 4" />
              </svg>
              <input
                id="store-search"
                v-model="searchQuery"
                type="search"
                placeholder="Tìm theo tên hoặc mã sản phẩm..."
                autocomplete="off"
              />
              <button
                v-if="searchQuery"
                type="button"
                aria-label="Xóa nội dung tìm kiếm"
                @click="clearSearch"
              >
                ×
              </button>
            </div>

            <div class="filter-field">
              <label class="sr-only" for="stock-filter">Lọc theo tình trạng hàng</label>
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M4 6h16M7 12h10M10 18h4" />
              </svg>
              <select id="stock-filter" v-model="stockStatus">
                <option value="all">Tất cả sản phẩm</option>
                <option value="in-stock">Còn hàng</option>
                <option value="out-of-stock">Hết hàng</option>
              </select>
            </div>
          </div>

          <div v-if="errorMessage" class="state-panel error-panel" role="alert">
            <div>
              <h3>Chưa thể tải sản phẩm</h3>
              <p>{{ errorMessage }}</p>
            </div>
            <button type="button" @click="loadProducts()">Thử lại</button>
          </div>

          <div
            v-else-if="loading"
            class="product-grid"
            aria-busy="true"
            aria-label="Đang tải sản phẩm"
          >
            <article v-for="item in 8" :key="item" class="product-card skeleton-card">
              <div class="skeleton skeleton-visual"></div>
              <div class="skeleton skeleton-line short"></div>
              <div class="skeleton skeleton-line"></div>
              <div class="skeleton skeleton-line price"></div>
            </article>
          </div>

          <div v-else-if="products.length === 0" class="state-panel empty-panel">
            <div>
              <h3>Không tìm thấy sản phẩm</h3>
              <p>Hãy thử từ khóa khác hoặc đổi tình trạng hàng hóa.</p>
            </div>
            <button v-if="searchQuery" type="button" @click="clearSearch">Xóa tìm kiếm</button>
          </div>

          <div v-else class="product-grid">
            <article
              v-for="product in products"
              :key="product.id"
              class="product-card"
              :class="{ 'is-out-of-stock': product.stockQuantity === 0 }"
            >
              <div class="product-visual" :class="productTone(product)">
                <span class="stock-badge" :class="{ unavailable: product.stockQuantity === 0 }">
                  {{ product.stockQuantity > 0 ? 'Còn hàng' : 'Hết hàng' }}
                </span>
                <span class="product-monogram">{{ productInitials(product.name) }}</span>
                <i class="shape shape-one" aria-hidden="true"></i>
                <i class="shape shape-two" aria-hidden="true"></i>
              </div>

              <div class="product-info">
                <p class="product-code">{{ product.productCode }}</p>
                <h3>{{ product.name }}</h3>
                <div class="product-meta">
                  <strong>{{ formatCurrency(product.price) }}</strong>
                  <span v-if="product.stockQuantity > 0">
                    Còn {{ product.stockQuantity }} sản phẩm
                  </span>
                  <span v-else class="out-of-stock-copy">Tạm thời hết hàng</span>
                </div>

                <button
                  class="add-cart"
                  type="button"
                  :disabled="product.stockQuantity === 0 || isCartLimitReached(product)"
                  @click="handleAddToCart(product)"
                >
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M5 8h14l-1 13H6L5 8Z" />
                    <path d="M9 10V6a3 3 0 0 1 6 0v4M12 13v5M9.5 15.5h5" />
                  </svg>
                  <span v-if="product.stockQuantity === 0">Hết hàng</span>
                  <span v-else-if="isCartLimitReached(product)">Đã thêm tối đa</span>
                  <span v-else>Thêm vào giỏ</span>
                </button>
              </div>
            </article>
          </div>

          <nav
            v-if="!loading && !errorMessage && totalPages > 1"
            class="pagination"
            aria-label="Phân trang sản phẩm"
          >
            <button
              type="button"
              :disabled="page === 1"
              aria-label="Trang trước"
              @click="changePage(page - 1)"
            >
              <span aria-hidden="true">‹</span>
            </button>

            <button
              v-for="pageNumber in visiblePages"
              :key="pageNumber"
              type="button"
              :class="{ active: pageNumber === page }"
              :aria-current="pageNumber === page ? 'page' : undefined"
              :aria-label="`Trang ${pageNumber}`"
              @click="changePage(pageNumber)"
            >
              {{ pageNumber }}
            </button>

            <button
              type="button"
              :disabled="page === totalPages"
              aria-label="Trang sau"
              @click="changePage(page + 1)"
            >
              <span aria-hidden="true">›</span>
            </button>
          </nav>
        </div>
      </section>
    </main>

    <footer class="store-footer">
      <div>
        <RouterLink class="footer-brand" to="/store">
          <img :src="logoUrl" alt="CN25" />
        </RouterLink>
        <p>CN25 — tiện lợi cho mọi khoảnh khắc trong ngày.</p>
        <a href="#products">Quay lại danh sách sản phẩm ↑</a>
      </div>
    </footer>

    <CartDrawer
      v-if="isCartOpen"
      :items="cartItems"
      :total-items="totalItems"
      :total-price="totalPrice"
      @close="isCartOpen = false"
      @increment="increment"
      @decrement="decrement"
      @remove="removeFromCart"
      @clear="clearCart"
    />
  </div>
</template>

<style scoped>
:global(html) {
  scroll-behavior: smooth;
}

:global(body) {
  margin: 0;
  min-width: 320px;
  overflow-x: hidden;
  background: #e7e7e7;
}

:global(*) {
  box-sizing: border-box;
}

:global(button),
:global(input),
:global(select) {
  font-family: 'Be Vietnam Pro', sans-serif;
}

.store-shell {
  --navy: #0d1828;
  --blue: #0878f9;
  --cyan: #00cedf;
  --ink: #122033;
  --muted: #64748b;
  --line: #dce6ef;
  --surface: #fff;
  --page: #e7e7e7;
  --radius: 10px;
  min-height: 100vh;
  color: var(--ink);
  background: var(--page);
  font-family: 'Be Vietnam Pro', sans-serif;
}

.skip-link {
  position: fixed;
  top: 10px;
  left: 10px;
  z-index: 100;
  padding: 10px 14px;
  color: #fff;
  background: var(--navy);
  border-radius: 8px;
  transform: translateY(-150%);
}

.skip-link:focus {
  transform: translateY(0);
}

.store-header {
  position: sticky;
  top: 0;
  z-index: 20;
  background: var(--navy);
  border-bottom: 3px solid var(--cyan);
  box-shadow: 0 4px 14px rgba(13, 24, 40, 0.16);
}

.header-inner,
.hero-inner,
.catalog-inner,
.store-footer > div {
  width: min(1180px, calc(100% - 40px));
  margin: 0 auto;
}

.header-inner {
  min-height: 78px;
  display: flex;
  align-items: center;
  gap: 24px;
}

.brand,
.footer-brand {
  display: inline-flex;
  align-items: center;
  gap: 12px;
  color: var(--ink);
  text-decoration: none;
}

.brand img {
  width: 84px;
  height: 34px;
  object-fit: contain;
}

.brand-copy {
  display: flex;
  flex-direction: column;
  padding-left: 12px;
  border-left: 1px solid #344255;
  line-height: 1.15;
}

.brand-copy strong {
  color: #fff;
  font-size: 14px;
  letter-spacing: 0.05em;
}

.brand-copy small {
  margin-top: 4px;
  color: #9fb0c6;
  font-size: 11px;
}

.store-nav {
  display: flex;
  align-items: center;
  gap: 32px;
  margin-left: auto;
}

.store-nav a {
  color: #c5d0df;
  font-size: 16px;
  font-weight: 600;
  text-decoration: none;
}

.store-nav a:hover {
  color: var(--cyan);
}

.account-slot {
  min-width: 0;
  flex: 0 1 auto;
}

.cart-trigger,
.primary-action {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  color: #fff;
  background: var(--blue);
  border-radius: 8px;
  font-weight: 700;
  text-decoration: none;
  box-shadow: none;
  transition:
    transform 160ms ease,
    box-shadow 160ms ease;
}

.cart-trigger {
  position: relative;
  min-height: 44px;
  padding: 0 20px;
  border: 0;
  font-size: 16px;
  cursor: pointer;
}

.cart-trigger svg,
.primary-action svg {
  width: 19px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.cart-trigger:hover,
.primary-action:hover {
  background: #0068df;
  transform: translateY(-1px);
  box-shadow: 0 8px 18px rgba(8, 120, 249, 0.22);
}

.cart-trigger em {
  min-width: 20px;
  height: 20px;
  display: inline-grid;
  place-items: center;
  padding: 0 5px;
  color: var(--blue);
  background: #fff;
  border-radius: 999px;
  font-size: 10px;
  font-style: normal;
  line-height: 1;
}

.hero {
  position: relative;
  overflow: hidden;
  padding-top: 10px;
  background: var(--page);
}

.hero::before {
  content: none;
}

.hero-inner {
  position: relative;
  min-height: 340px;
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  align-items: center;
  gap: 0;
  overflow: hidden;
  padding: 46px 48px;
  background:
    radial-gradient(circle at 88% 18%, rgba(0, 206, 223, 0.18), transparent 25%),
    linear-gradient(135deg, #fff 0%, #f4faff 100%);
  border-radius: var(--radius);
}

.hero-inner::after {
  content: '';
  position: absolute;
  top: -82px;
  right: -62px;
  width: 260px;
  height: 260px;
  border: 34px solid rgba(8, 120, 249, 0.08);
  border-radius: 50%;
}

.hero-copy {
  position: relative;
  z-index: 2;
  max-width: 760px;
}

.eyebrow,
.section-kicker {
  margin: 0 0 18px;
  color: var(--blue);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.eyebrow {
  display: flex;
  align-items: center;
  gap: 10px;
}

.eyebrow span {
  width: 28px;
  height: 2px;
  background: var(--cyan);
}

.hero h1 {
  margin: 0;
  color: var(--navy);
  font-size: clamp(38px, 5vw, 58px);
  line-height: 1.08;
  letter-spacing: -0.045em;
}

.hero h1 span {
  display: block;
  color: var(--blue);
}

.hero-description {
  max-width: 590px;
  margin: 26px 0 0;
  color: #52647b;
  font-size: 16px;
  line-height: 1.7;
}

.hero-actions {
  display: flex;
  align-items: center;
  gap: 24px;
  margin-top: 28px;
}

.primary-action {
  min-height: 46px;
  padding: 0 20px;
  font-size: 16px;
}

.live-note {
  display: inline-flex;
  align-items: center;
  gap: 9px;
  color: #52647b;
  font-size: 12px;
  font-weight: 600;
}

.live-note i {
  width: 9px;
  height: 9px;
  background: #1fc77e;
  border: 2px solid #d8f8e9;
  border-radius: 50%;
  box-shadow: 0 0 0 3px rgba(31, 199, 126, 0.14);
}

.hero-visual {
  position: relative;
  min-height: 410px;
}

.visual-orbit {
  position: absolute;
  border: 1px solid rgba(8, 120, 249, 0.17);
  border-radius: 50%;
}

.orbit-one {
  inset: 16px 0 0 20px;
}

.orbit-two {
  inset: 66px 50px 48px 72px;
  border-style: dashed;
}

.shopping-bag {
  position: absolute;
  top: 82px;
  left: 50%;
  width: 245px;
  height: 270px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #fff;
  background: linear-gradient(145deg, #0b7cf8, #075bd4);
  border-radius: 24px 24px 34px 34px;
  box-shadow: 0 32px 64px rgba(10, 87, 184, 0.28);
  transform: translateX(-50%) rotate(3deg);
}

.shopping-bag::after {
  content: '';
  position: absolute;
  inset: auto 20px 18px;
  height: 4px;
  background: var(--cyan);
  border-radius: 999px;
}

.bag-handle {
  position: absolute;
  top: -54px;
  width: 116px;
  height: 76px;
  border: 16px solid #075ed7;
  border-bottom: 0;
  border-radius: 70px 70px 0 0;
}

.shopping-bag img {
  width: 150px;
  object-fit: contain;
  filter: brightness(0) invert(1);
}

.shopping-bag span {
  margin-top: 20px;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.2em;
  line-height: 1.6;
  text-align: center;
  opacity: 0.78;
}

.floating-card {
  position: absolute;
  z-index: 2;
  display: flex;
  align-items: center;
  gap: 9px;
  padding: 12px 16px;
  color: #24364d;
  background: rgba(255, 255, 255, 0.94);
  border: 1px solid #e1ebf4;
  border-radius: 14px;
  font-size: 12px;
  font-weight: 700;
  box-shadow: 0 16px 36px rgba(36, 54, 77, 0.12);
}

.floating-card svg {
  width: 20px;
  fill: none;
  stroke: var(--blue);
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.card-fast {
  top: 52px;
  left: 4px;
  transform: rotate(-4deg);
}

.card-fresh {
  right: -4px;
  bottom: 42px;
  transform: rotate(4deg);
}

.catalog {
  scroll-margin-top: 86px;
  padding: 10px 0 60px;
  background: var(--page);
}

.catalog-inner {
  padding: 20px;
  background: var(--surface);
  border-radius: var(--radius);
}

.section-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 30px;
}

.section-kicker {
  margin-bottom: 10px;
}

.section-heading h2 {
  margin: 0;
  color: var(--navy);
  font-size: 24px;
  line-height: 1.15;
  letter-spacing: -0.04em;
}

.result-count {
  margin: 0 0 5px;
  color: var(--muted);
  font-size: 16px;
  font-weight: 600;
}

.catalog-toolbar {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 230px;
  gap: 14px;
  margin: 22px 0 20px;
  padding: 16px;
  background: #f7fafc;
  border: 1px solid var(--line);
  border-radius: var(--radius);
}

.search-field,
.filter-field {
  min-height: 44px;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 16px;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 8px;
  transition:
    border-color 150ms ease,
    box-shadow 150ms ease;
}

.search-field:focus-within,
.filter-field:focus-within {
  border-color: var(--blue);
  box-shadow: 0 0 0 4px rgba(8, 120, 249, 0.1);
}

.search-field > svg,
.filter-field > svg {
  flex: 0 0 auto;
  width: 21px;
  fill: none;
  stroke: #7890a7;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.search-field input,
.filter-field select {
  min-width: 0;
  flex: 1;
  color: var(--ink);
  background: transparent;
  border: 0;
  outline: 0;
  font-size: 16px;
}

.search-field input::placeholder {
  color: #91a1b2;
}

.search-field button {
  width: 29px;
  height: 29px;
  color: #708197;
  background: #edf2f6;
  border: 0;
  border-radius: 50%;
  cursor: pointer;
  line-height: 1;
}

.filter-field select {
  cursor: pointer;
}

.product-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
}

.product-card {
  overflow: hidden;
  min-width: 0;
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid #e1e9f0;
  border-radius: var(--radius);
  box-shadow: none;
  transition:
    transform 180ms ease,
    border-color 180ms ease,
    box-shadow 180ms ease;
}

.product-card:not(.skeleton-card):hover {
  border-color: #a9cce9;
  box-shadow: 0 8px 22px rgba(24, 49, 79, 0.08);
  transform: translateY(-2px);
}

.product-card.is-out-of-stock {
  opacity: 0.76;
}

.product-visual {
  position: relative;
  height: 168px;
  display: grid;
  place-items: center;
  overflow: hidden;
  background: linear-gradient(145deg, #edf7ff, #d9edff);
}

.product-visual.tone-1 {
  background: linear-gradient(145deg, #eafcf8, #cef5ec);
}

.product-visual.tone-2 {
  background: linear-gradient(145deg, #fff7e9, #ffebc9);
}

.product-visual.tone-3 {
  background: linear-gradient(145deg, #f3efff, #e2d9ff);
}

.stock-badge {
  position: absolute;
  top: 14px;
  left: 14px;
  z-index: 2;
  padding: 6px 10px;
  color: #087750;
  background: rgba(235, 255, 247, 0.92);
  border: 1px solid rgba(49, 188, 136, 0.2);
  border-radius: 999px;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.stock-badge.unavailable {
  color: #a34747;
  background: rgba(255, 241, 241, 0.94);
  border-color: rgba(190, 79, 79, 0.2);
}

.product-monogram {
  position: relative;
  z-index: 1;
  width: 94px;
  height: 106px;
  display: grid;
  place-items: center;
  color: #fff;
  background: linear-gradient(160deg, var(--cyan), var(--blue));
  border: 5px solid rgba(255, 255, 255, 0.72);
  border-radius: 10px 10px 18px 18px;
  font-size: 28px;
  font-weight: 900;
  letter-spacing: -0.04em;
  box-shadow: 0 18px 30px rgba(24, 119, 207, 0.22);
  transform: rotate(2deg);
}

.tone-1 .product-monogram {
  background: linear-gradient(160deg, #30d6ac, #0d9b85);
  box-shadow: 0 18px 30px rgba(13, 155, 133, 0.2);
}

.tone-2 .product-monogram {
  background: linear-gradient(160deg, #ffbd4a, #ef7c2e);
  box-shadow: 0 18px 30px rgba(218, 111, 35, 0.2);
}

.tone-3 .product-monogram {
  background: linear-gradient(160deg, #9d8cff, #6d5bd1);
  box-shadow: 0 18px 30px rgba(102, 85, 194, 0.2);
}

.shape {
  position: absolute;
  border-radius: 50%;
  border: 1px solid rgba(36, 90, 142, 0.12);
}

.shape-one {
  width: 130px;
  height: 130px;
  right: -55px;
  bottom: -58px;
}

.shape-two {
  width: 70px;
  height: 70px;
  top: 38px;
  left: -30px;
}

.product-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 16px;
}

.product-code {
  margin: 0 0 8px;
  color: #8292a5;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.product-info h3 {
  min-height: 48px;
  margin: 0;
  color: #1d2d42;
  font-size: 16px;
  line-height: 1.5;
  display: -webkit-box;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.product-meta {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 12px;
  margin-top: 17px;
  padding-top: 15px;
  border-top: 1px solid #edf1f5;
}

.product-meta strong {
  color: var(--blue);
  font-size: 17px;
  white-space: nowrap;
}

.product-meta span {
  color: #718197;
  font-size: 12px;
  line-height: 1.4;
  text-align: right;
}

.product-meta .out-of-stock-copy {
  color: #a75a5a;
}

.add-cart {
  width: 100%;
  min-height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 9px;
  margin-top: 16px;
  color: #fff;
  background: var(--blue);
  border: 1px solid var(--blue);
  border-radius: 8px;
  font-size: 16px;
  font-weight: 800;
  cursor: pointer;
  transition:
    transform 150ms ease,
    background 150ms ease;
}

.add-cart svg {
  width: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.add-cart:hover:not(:disabled) {
  background: #0068df;
  transform: translateY(-1px);
}

.add-cart:disabled {
  color: #8190a1;
  background: #eef2f5;
  border-color: #e1e7ec;
  cursor: not-allowed;
}

.state-panel {
  min-height: 250px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 18px;
  padding: 40px;
  background: #fff;
  border: 1px dashed #cbd9e5;
  border-radius: var(--radius);
  text-align: left;
}

.state-panel > span {
  flex: 0 0 auto;
  width: 52px;
  height: 52px;
  display: grid;
  place-items: center;
  color: var(--blue);
  background: #edf7ff;
  border-radius: 16px;
}

.state-panel svg {
  width: 25px;
  fill: none;
  stroke: currentColor;
  stroke-width: 1.8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.state-panel h3 {
  margin: 0;
  font-size: 18px;
}

.state-panel p {
  margin: 7px 0 0;
  color: var(--muted);
  font-size: 16px;
}

.state-panel button {
  min-height: 44px;
  margin-left: 12px;
  padding: 0 16px;
  color: #fff;
  background: var(--blue);
  border: 0;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
}

.error-panel > span {
  color: #c55555;
  background: #fff1f1;
}

.skeleton-card {
  min-height: 337px;
  padding-bottom: 24px;
}

.skeleton {
  background: linear-gradient(90deg, #edf2f6 25%, #f8fafc 50%, #edf2f6 75%);
  background-size: 200% 100%;
  animation: shimmer 1.3s infinite;
}

.skeleton-visual {
  height: 190px;
}

.skeleton-line {
  width: calc(100% - 40px);
  height: 13px;
  margin: 13px 20px 0;
  border-radius: 999px;
}

.skeleton-line.short {
  width: 32%;
  height: 8px;
  margin-top: 22px;
}

.skeleton-line.price {
  width: 45%;
  height: 17px;
}

@keyframes shimmer {
  to {
    background-position: -200% 0;
  }
}

.pagination {
  display: flex;
  justify-content: center;
  gap: 8px;
  margin-top: 42px;
}

.pagination button {
  width: 42px;
  height: 42px;
  display: grid;
  place-items: center;
  color: #596b82;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 8px;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
  transition:
    color 150ms ease,
    background 150ms ease,
    border-color 150ms ease;
}

.pagination button:hover:not(:disabled),
.pagination button.active {
  color: #fff;
  background: var(--blue);
  border-color: var(--blue);
}

.pagination button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.pagination svg {
  width: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.pagination button > span {
  color: inherit;
  font-size: 24px;
  line-height: 1;
}

.store-footer {
  padding: 42px 0;
  color: #9eb0c5;
  background: var(--navy);
  border-top: 3px solid var(--cyan);
}

.store-footer > div {
  display: flex;
  align-items: center;
  gap: 26px;
}

.footer-brand img {
  width: 88px;
  filter: brightness(0) invert(1);
}

.store-footer p {
  margin: 0;
  font-size: 14px;
}

.store-footer > div > a:last-child {
  margin-left: auto;
  color: #c7d5e5;
  font-size: 14px;
  font-weight: 700;
  text-decoration: none;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

button:focus-visible,
a:focus-visible,
input:focus-visible,
select:focus-visible {
  outline: 3px solid rgba(0, 206, 223, 0.45);
  outline-offset: 3px;
}

@media (max-width: 1000px) {
  .hero-inner {
    grid-template-columns: 1fr;
    gap: 0;
  }

  .hero-visual {
    transform: scale(0.88);
  }

  .product-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .header-inner,
  .hero-inner,
  .catalog-inner,
  .store-footer > div {
    width: min(100% - 20px, 1180px);
  }

  .header-inner {
    min-height: 66px;
    gap: 12px;
  }

  .brand-copy,
  .store-nav {
    display: none;
  }

  .cart-trigger {
    min-height: 40px;
    margin-left: 0;
    padding-inline: 16px;
  }

  .account-slot {
    margin-left: auto;
  }

  .hero-inner {
    min-height: auto;
    grid-template-columns: 1fr;
    padding: 36px 26px;
  }

  .hero h1 {
    font-size: clamp(36px, 11vw, 48px);
  }

  .hero-description {
    font-size: 15px;
  }

  .hero-visual {
    min-height: 330px;
    margin-top: -10px;
    transform: none;
  }

  .shopping-bag {
    top: 64px;
    width: 190px;
    height: 215px;
  }

  .bag-handle {
    top: -43px;
    width: 94px;
    height: 60px;
    border-width: 13px;
    border-bottom: 0;
  }

  .shopping-bag img {
    width: 120px;
  }

  .catalog {
    padding: 8px 0 40px;
  }

  .catalog-inner {
    padding: 16px;
  }

  .section-heading {
    align-items: flex-start;
    flex-direction: column;
    gap: 12px;
  }

  .catalog-toolbar {
    grid-template-columns: 1fr;
    margin-top: 26px;
    padding: 14px;
  }

  .product-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
  }

  .product-visual,
  .skeleton-visual {
    height: 166px;
  }

  .product-info {
    padding: 16px;
  }

  .product-meta {
    align-items: flex-start;
    flex-direction: column;
    gap: 6px;
  }

  .product-meta span {
    text-align: left;
  }

  .state-panel {
    min-height: 280px;
    align-items: center;
    flex-direction: column;
    text-align: center;
  }

  .state-panel button {
    margin: 4px 0 0;
  }

  .store-footer > div {
    align-items: flex-start;
    flex-direction: column;
    gap: 12px;
  }

  .store-footer > div > a:last-child {
    margin-left: 0;
  }
}

@media (max-width: 480px) {
  .header-inner,
  .hero-inner,
  .catalog-inner,
  .store-footer > div {
    width: calc(100% - 16px);
  }

  .brand img {
    width: 66px;
  }

  .cart-trigger {
    width: 42px;
    padding-inline: 13px;
  }

  .cart-trigger span {
    display: none;
  }

  .hero-actions {
    align-items: flex-start;
    flex-direction: column;
  }

  .hero-inner {
    padding: 30px 18px;
  }

  .hero-inner::after {
    top: -112px;
    right: -112px;
  }

  .eyebrow,
  .section-kicker {
    font-size: 11px;
  }

  .section-heading h2 {
    font-size: 22px;
  }

  .catalog-inner {
    padding: 14px;
  }

  .catalog-toolbar {
    margin-top: 20px;
    padding: 12px;
  }

  .floating-card {
    padding: 10px 12px;
    font-size: 10px;
  }

  .card-fast {
    left: 0;
  }

  .card-fresh {
    right: 0;
  }

  .product-grid {
    grid-template-columns: 1fr;
  }

  .product-visual,
  .skeleton-visual {
    height: 190px;
  }

  .pagination button {
    width: 38px;
    height: 38px;
  }
}

@media (prefers-reduced-motion: reduce) {
  :global(html) {
    scroll-behavior: auto;
  }

  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    scroll-behavior: auto !important;
    transition-duration: 0.01ms !important;
  }
}
</style>
