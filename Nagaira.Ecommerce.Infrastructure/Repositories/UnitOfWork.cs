using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Nagaira.Ecommerce.Domain.Interfaces;
using Nagaira.Ecommerce.Infrastructure.Data;

namespace Nagaira.Ecommerce.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public IProductRepository Products { get; }
    public IUserRepository Users { get; }
    public IOrderRepository Orders { get; }
    public IPriceLevelRepository PriceLevels { get; }
    public IProductPriceRepository ProductPrices { get; }
    public IInventoryMovementRepository InventoryMovements { get; }
    public IInventoryBalanceRepository InventoryBalances { get; }
    public IOfferRepository Offers { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IAdminRepository Admin { get; }
    public ICategoryRepository Categories { get; }
    public IPaymentMethodRepository PaymentMethods { get; }
    public IPaymentMethodTypeRepository PaymentMethodTypes { get; }
    public IAppSettingRepository AppSettings { get; }
    public ISupplierRepository Suppliers { get; }
    public IProductSupplierRepository ProductSuppliers { get; }

    public UnitOfWork(
        ApplicationDbContext context,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        IPriceLevelRepository priceLevelRepository,
        IProductPriceRepository productPriceRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IInventoryBalanceRepository inventoryBalanceRepository,
        IOfferRepository offerRepository,
        IAuditLogRepository auditLogRepository,
        IAdminRepository adminRepository,
        ICategoryRepository categoryRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IPaymentMethodTypeRepository paymentMethodTypeRepository,
        IAppSettingRepository appSettingRepository,
        ISupplierRepository supplierRepository,
        IProductSupplierRepository productSupplierRepository)
    {
        _context = context;
        Products = productRepository;
        Users = userRepository;
        Orders = orderRepository;
        PriceLevels = priceLevelRepository;
        ProductPrices = productPriceRepository;
        InventoryMovements = inventoryMovementRepository;
        InventoryBalances = inventoryBalanceRepository;
        Offers = offerRepository;
        AuditLogs = auditLogRepository;
        Admin = adminRepository;
        Categories = categoryRepository;
        PaymentMethods = paymentMethodRepository;
        PaymentMethodTypes = paymentMethodTypeRepository;
        AppSettings = appSettingRepository;
        Suppliers = supplierRepository;
        ProductSuppliers = productSupplierRepository;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        return new Repository<T>(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            return _transaction;
        }
        _transaction = await _context.Database.BeginTransactionAsync();
        return _transaction;
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public IExecutionStrategy GetExecutionStrategy()
    {
        return _context.Database.CreateExecutionStrategy();
    }

    public DbContext GetDbContext()
    {
        return _context;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
