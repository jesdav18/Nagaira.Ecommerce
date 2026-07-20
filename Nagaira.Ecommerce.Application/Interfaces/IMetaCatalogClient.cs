using Nagaira.Ecommerce.Application.MetaCatalog;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IMetaCatalogClient
{
    Task<MetaCatalogBatchResult> SubmitAsync(
        IReadOnlyCollection<MetaCatalogMappingResult> items,
        CancellationToken cancellationToken = default);
}
