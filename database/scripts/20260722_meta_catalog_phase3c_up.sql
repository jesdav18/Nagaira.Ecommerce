-- Meta Catalog Phase 3C
-- Adds state needed to reconcile accepted batch requests without resubmitting products.

ALTER TABLE meta_product_sync_states
    ADD COLUMN IF NOT EXISTS pending_payload_hash character varying(128),
    ADD COLUMN IF NOT EXISTS last_action character varying(20),
    ADD COLUMN IF NOT EXISTS batch_handle character varying(255),
    ADD COLUMN IF NOT EXISTS last_error_subcode character varying(100);

CREATE INDEX IF NOT EXISTS ix_meta_product_sync_states_batch_handle
    ON meta_product_sync_states (batch_handle);
