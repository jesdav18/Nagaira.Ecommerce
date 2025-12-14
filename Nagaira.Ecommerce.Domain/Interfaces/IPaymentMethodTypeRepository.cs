using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IPaymentMethodTypeRepository : IRepository<PaymentMethodType>
{
    Task<IEnumerable<PaymentMethodType>> GetActivePaymentMethodTypesAsync();
}

