using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IPaymentMethodRepository : IRepository<PaymentMethod>
{
    Task<IEnumerable<PaymentMethod>> GetActivePaymentMethodsAsync();
    Task<IEnumerable<PaymentMethod>> GetByTypeAsync(string type);
}

