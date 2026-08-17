-- Reverts Meta Catalog Phase 1 schema changes.

DROP TABLE IF EXISTS meta_product_sync_states;

ALTER TABLE products
    DROP COLUMN IF EXISTS brand;
