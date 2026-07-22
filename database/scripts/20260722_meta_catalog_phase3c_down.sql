-- Reverts Meta Catalog Phase 3C state columns.

DROP INDEX IF EXISTS ix_meta_product_sync_states_batch_handle;

ALTER TABLE meta_product_sync_states
    DROP COLUMN IF EXISTS last_error_subcode,
    DROP COLUMN IF EXISTS batch_handle,
    DROP COLUMN IF EXISTS last_action,
    DROP COLUMN IF EXISTS pending_payload_hash;
