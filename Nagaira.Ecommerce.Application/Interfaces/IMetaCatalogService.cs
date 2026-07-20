using Nagaira.Ecommerce.Application.MetaCatalog;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IMetaCatalogService
{
    Task<MetaCatalogMappingResult?> BuildProductMappingAsync(Guid productId, CancellationToken cancellationToken = default);
    Task MarkProductPendingAsync(Guid productId, CancellationToken cancellationToken = default);
}
