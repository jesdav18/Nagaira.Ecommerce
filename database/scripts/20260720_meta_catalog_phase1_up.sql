-- Meta Catalog Phase 1
-- Adds product brand and persistent sync state used as the initial work queue.

ALTER TABLE products
    ADD COLUMN IF NOT EXISTS brand character varying(255);

CREATE TABLE IF NOT EXISTS meta_product_sync_states (
    product_id uuid NOT NULL,
    retailer_id character varying(64) NOT NULL,
    status character varying(30) NOT NULL,
    last_payload_hash character varying(128),
    last_synced_at timestamp with time zone,
    last_attempt_at timestamp with time zone,
    last_error_code character varying(100),
    last_error_message character varying(2000),
    retry_count integer NOT NULL DEFAULT 0,
    lock_id uuid,
    locked_until_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone,
    CONSTRAINT pk_meta_product_sync_states PRIMARY KEY (product_id),
    CONSTRAINT fk_meta_product_sync_states_products_product_id
        FOREIGN KEY (product_id)
        REFERENCES products (id)
        ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_meta_product_sync_states_retailer_id
    ON meta_product_sync_states (retailer_id);

CREATE INDEX IF NOT EXISTS ix_meta_product_sync_states_status
    ON meta_product_sync_states (status);

CREATE INDEX IF NOT EXISTS ix_meta_product_sync_states_locked_until_at
    ON meta_product_sync_states (locked_until_at);
