
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Nagaira.Ecommerce.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IUserRepository Users { get; }
    IOrderRepository Orders { get; }
    IPriceLevelRepository PriceLevels { get; }
    IProductPriceRepository ProductPrices { get; }
    IInventoryMovementRepository InventoryMovements { get; }
    IInventoryBalanceRepository InventoryBalances { get; }
    IOfferRepository Offers { get; }
    IAuditLogRepository AuditLogs { get; }
    IAdminRepository Admin { get; }
    ICategoryRepository Categories { get; }
    IPaymentMethodRepository PaymentMethods { get; }
    IPaymentMethodTypeRepository PaymentMethodTypes { get; }
    IAppSettingRepository AppSettings { get; }
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    IExecutionStrategy GetExecutionStrategy();
    DbContext GetDbContext();
}
