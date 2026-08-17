namespace Nagaira.Ecommerce.Application.MetaCatalog;

public enum MetaCatalogSyncAction
{
    Upsert,
    Delete
}

public record MetaCatalogMappingResult(
    MetaCatalogSyncAction Action,
    string RetailerId,
    MetaCatalogProduct? Item,
    string PayloadHash
);
