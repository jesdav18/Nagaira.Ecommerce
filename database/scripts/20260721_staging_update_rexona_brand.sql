-- Staging-only data correction for Meta Catalog Phase 3B0.
-- Do not run this script against production.

UPDATE products
SET brand = 'Rexona'
WHERE id = '950c72d7-4bc3-4e58-8098-45738e66fc4e'
  AND brand = 'Nagaira Test';
