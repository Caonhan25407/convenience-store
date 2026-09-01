import { computed, ref, watch } from 'vue'
import type { Product } from '@/types/product'

export interface CartItem {
  product: Product
  quantity: number
}

const STORAGE_KEY = 'cn25-customer-cart'

export function clearStoredCart() {
  if (typeof window === 'undefined') {
    return
  }

  try {
    window.localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Logging out still succeeds when storage is unavailable.
  }
}

function isStoredCartItem(value: unknown): value is CartItem {
  if (!value || typeof value !== 'object') {
    return false
  }

  const item = value as Partial<CartItem>
  const product = item.product as Partial<Product> | undefined

  return Boolean(
    product &&
      Number.isInteger(product.id) &&
      typeof product.productCode === 'string' &&
      typeof product.name === 'string' &&
      typeof product.price === 'number' &&
      Number.isInteger(product.stockQuantity) &&
      Number.isInteger(item.quantity) &&
      Number(item.quantity) > 0,
  )
}

function readStoredCart(): CartItem[] {
  if (typeof window === 'undefined') {
    return []
  }

  try {
    const storedValue = window.localStorage.getItem(STORAGE_KEY)

    if (!storedValue) {
      return []
    }

    const parsedValue: unknown = JSON.parse(storedValue)

    if (!Array.isArray(parsedValue)) {
      return []
    }

    return parsedValue
      .filter(isStoredCartItem)
      .filter((item) => item.product.stockQuantity > 0)
      .map((item) => ({
        product: item.product,
        quantity: Math.min(item.quantity, item.product.stockQuantity),
      }))
  } catch {
    return []
  }
}

export function useCart() {
  const cartItems = ref<CartItem[]>(readStoredCart())

  const totalItems = computed(() =>
    cartItems.value.reduce((total, item) => total + item.quantity, 0),
  )

  const totalPrice = computed(() =>
    cartItems.value.reduce(
      (total, item) => total + item.product.price * item.quantity,
      0,
    ),
  )

  function quantityInCart(productId: number) {
    return cartItems.value.find((item) => item.product.id === productId)?.quantity ?? 0
  }

  function addToCart(product: Product) {
    if (product.stockQuantity <= 0) {
      return false
    }

    const existingItem = cartItems.value.find(
      (item) => item.product.id === product.id,
    )

    if (existingItem) {
      if (existingItem.quantity >= product.stockQuantity) {
        return false
      }

      existingItem.product = product
      existingItem.quantity++
      return true
    }

    cartItems.value.push({ product, quantity: 1 })
    return true
  }

  function increment(productId: number) {
    const item = cartItems.value.find(
      (cartItem) => cartItem.product.id === productId,
    )

    if (item && item.quantity < item.product.stockQuantity) {
      item.quantity++
    }
  }

  function decrement(productId: number) {
    const item = cartItems.value.find(
      (cartItem) => cartItem.product.id === productId,
    )

    if (!item) {
      return
    }

    if (item.quantity === 1) {
      removeFromCart(productId)
      return
    }

    item.quantity--
  }

  function removeFromCart(productId: number) {
    cartItems.value = cartItems.value.filter(
      (item) => item.product.id !== productId,
    )
  }

  function clearCart() {
    cartItems.value = []
  }

  function syncProducts(currentProducts: Product[]) {
    const currentProductMap = new Map(
      currentProducts.map((product) => [product.id, product]),
    )

    cartItems.value = cartItems.value.flatMap((item) => {
      const currentProduct = currentProductMap.get(item.product.id)

      if (!currentProduct) {
        return [item]
      }

      if (currentProduct.stockQuantity <= 0) {
        return []
      }

      return [{
        product: currentProduct,
        quantity: Math.min(item.quantity, currentProduct.stockQuantity),
      }]
    })
  }

  watch(
    cartItems,
    (items) => {
      if (typeof window === 'undefined') {
        return
      }

      try {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(items))
      } catch {
        // The cart still works in memory when storage is unavailable.
      }
    },
    { deep: true },
  )

  return {
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
  }
}
