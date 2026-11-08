CREATE TABLE IF NOT EXISTS orders (
    id uuid PRIMARY KEY,
    customer_id uuid NOT NULL,
    created_at timestamptz NOT NULL,
    status text NOT NULL CHECK (status IN ('queued', 'paid', 'completed', 'refunded')),
    total_cents bigint NOT NULL CHECK (total_cents >= 0)
);

CREATE INDEX IF NOT EXISTS ix_orders_customer_created_id
    ON orders (customer_id, created_at DESC, id DESC)
    INCLUDE (status, total_cents);

INSERT INTO orders (id, customer_id, created_at, status, total_cents)
SELECT md5('portfolio-order-' || sample.number)::uuid,
       CASE WHEN sample.number % 2 = 0
            THEN '11111111-1111-4111-8111-111111111111'::uuid
            ELSE '22222222-2222-4222-8222-222222222222'::uuid
       END,
       TIMESTAMPTZ '2026-01-01 00:00:00+00' - sample.number * INTERVAL '1 minute',
       CASE sample.number % 4
            WHEN 0 THEN 'completed'
            WHEN 1 THEN 'queued'
            WHEN 2 THEN 'paid'
            ELSE 'refunded'
       END,
       1250 + sample.number * 125
FROM generate_series(1, 120) AS sample(number)
ON CONFLICT (id) DO NOTHING;