using Nagaira.Ecommerce.Application.MetaCatalog;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IMetaCatalogPayloadHasher
{
    string HashUpsert(MetaCatalogProduct item);
    string HashDelete(string retailerId);
}
