# Meta Catalog configuration

This integration treats Nagaira as the source of truth and uses `Product.Id` as the stable Meta `retailer_id`.

No real tokens or secrets should be committed. Configure these values with environment variables or the deployment secret store.

## Environment variables

- `MetaCatalog__ApiBaseUrl`: Graph API base URL. Default: `https://graph.facebook.com`.
- `MetaCatalog__GraphApiVersion`: Graph API version segment, for example `v25.0`. Keep it configurable and set it from deployment configuration.
- `MetaCatalog__CatalogId`: Meta Commerce Manager catalog ID.
- `MetaCatalog__AccessToken`: Meta access token. Secret value; never log or expose it.
- `MetaCatalog__Currency`: ISO currency code. Business decision: `HNL`.
- `MetaCatalog__PublicBaseUrl`: public storefront base URL used to build product links.
- `MetaCatalog__PublicPriceLevelId`: Nagaira `PriceLevel.Id` for the public retail price.
- `MetaCatalog__SyncEnabled`: enables future worker/API synchronization. Fase 1 leaves this disabled.
- `MetaCatalog__BatchSize`: future batch size for Catalog API calls.
- `MetaCatalog__LockMinutes`: lock duration used to prevent concurrent sync of the same product.
- `MetaCatalog__RequestTimeoutSeconds`: HTTP timeout per Meta Catalog request.

## Catalog API endpoint

Official Meta developer documentation consulted for Phase 2:

- Product Catalog Items Batch: `https://developers.facebook.com/documentation/ads-commerce/marketing-api/reference/product-catalog/items_batch`
- Product Catalog Batch: `https://developers.facebook.com/documentation/ads-commerce/marketing-api/reference/product-catalog/batch`
- Catalog Batch API guide: `https://developers.facebook.com/documentation/ads-commerce/catalog/guides/manage-catalog-items/catalog-batch-api`
- Graph API overview/access token docs: `https://developers.facebook.com/docs/graph-api/overview/`

The client uses Catalog Batch API:

```text
POST {MetaCatalog:ApiBaseUrl}/{MetaCatalog:GraphApiVersion}/{MetaCatalog:CatalogId}/items_batch
```

The request uses an Authorization bearer token header. The token must not be logged, included in exception messages, committed, or exposed to the frontend.

`retailer_id` is always `Product.Id` in canonical Guid `D` format. `UPDATE` requests are idempotent for Nagaira because the same `retailer_id` targets the same catalog item; `DELETE` requests retain that same `retailer_id`.

## Public product URL

Frontend verification in this repository:

- `nagaira-storefront/src/app/app.routes.ts` defines product detail as `p/:slug`.
- `nagaira-storefront/angular.json` production `baseHref` is `/`.
- `nagaira-storefront/src/index.html` has `<base href="/">`.

Therefore the current production route shape is:

```text
{MetaCatalog:PublicBaseUrl}/p/{product.slug}
```

If deployment changes the Angular `baseHref`, update `MetaCatalog__PublicBaseUrl` rather than hard-coding a path in code.
