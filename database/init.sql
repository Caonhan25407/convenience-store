CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    product_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    price NUMERIC(12,2) NOT NULL CHECK (price >= 0),
    stock_quantity INT NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS app_users (
    id BIGSERIAL PRIMARY KEY,
    email VARCHAR(254) NOT NULL,
    normalized_email VARCHAR(254) NOT NULL UNIQUE,
    display_name VARCHAR(150) NOT NULL CHECK (BTRIM(display_name) <> ''),
    phone VARCHAR(25),
    password_hash TEXT NOT NULL,
    role VARCHAR(20) NOT NULL CHECK (role IN ('ADMIN', 'CUSTOMER')),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    security_stamp UUID NOT NULL,
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS orders (
    id BIGSERIAL PRIMARY KEY,
    order_code VARCHAR(40) NOT NULL UNIQUE,
    customer_user_id BIGINT REFERENCES app_users(id) ON DELETE SET NULL,
    customer_name VARCHAR(150) NOT NULL CHECK (BTRIM(customer_name) <> ''),
    phone VARCHAR(25) NOT NULL CHECK (BTRIM(phone) <> ''),
    delivery_address VARCHAR(500) NOT NULL CHECK (BTRIM(delivery_address) <> ''),
    payment_method VARCHAR(10) NOT NULL CHECK (payment_method = 'COD'),
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING'
        CHECK (status IN ('PENDING', 'CONFIRMED', 'COMPLETED', 'CANCELLED')),
    total_amount NUMERIC(28,2) NOT NULL CHECK (total_amount >= 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE orders
    ADD COLUMN IF NOT EXISTS customer_user_id BIGINT
    REFERENCES app_users(id) ON DELETE SET NULL;

CREATE TABLE IF NOT EXISTS order_items (
    id BIGSERIAL PRIMARY KEY,
    order_id BIGINT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id INT REFERENCES products(id) ON DELETE SET NULL,
    product_code VARCHAR(50) NOT NULL,
    product_name VARCHAR(150) NOT NULL,
    unit_price NUMERIC(12,2) NOT NULL CHECK (unit_price >= 0),
    quantity INT NOT NULL CHECK (quantity > 0),
    line_total NUMERIC(28,2) NOT NULL CHECK (line_total = unit_price * quantity),
    UNIQUE (order_id, product_code)
);

CREATE INDEX IF NOT EXISTS idx_orders_created_at
    ON orders (created_at DESC);

CREATE INDEX IF NOT EXISTS idx_orders_customer_user_id
    ON orders (customer_user_id);

CREATE INDEX IF NOT EXISTS idx_order_items_order_id
    ON order_items (order_id);
