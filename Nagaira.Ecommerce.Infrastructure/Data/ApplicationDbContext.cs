using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<PriceLevel> PriceLevels { get; set; }
    public DbSet<ProductPrice> ProductPrices { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<InventoryBalance> InventoryBalances { get; set; }
    public DbSet<Offer> Offers { get; set; }
    public DbSet<OfferProduct> OfferProducts { get; set; }
    public DbSet<OfferCategory> OfferCategories { get; set; }
    public DbSet<OfferApplication> OfferApplications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PaymentMethodType> PaymentMethodTypes { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Entities that don't have UpdatedAt column
        var entitiesWithoutUpdatedAt = new[] { "Offer", "AuditLog", "InventoryMovement", "OfferProduct", "OfferCategory", "User", "Product", "Category", "PaymentMethod", "PaymentMethodType" };

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Ignore(x => x.UpdatedAt);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.PriceLevel).WithMany(e => e.Users)
                .HasForeignKey(e => e.PriceLevelId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.Ignore(x => x.UpdatedAt);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.HasOne(e => e.Category).WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryBalance).WithOne(e => e.Product)
                .HasForeignKey<InventoryBalance>(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(255);
            entity.Ignore(x => x.UpdatedAt);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasOne(e => e.ParentCategory).WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product).WithMany(e => e.Images).HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Ignore(e => e.UpdatedAt);
            entity.Property(e => e.Tax).HasPrecision(18, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.Status)
                .HasColumnName("status");
            entity.HasOne(e => e.User).WithMany(e => e.Orders).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.HasOne(e => e.Order).WithMany(e => e.Items).HasForeignKey(e => e.OrderId);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("addresses");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User).WithMany(e => e.Addresses).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<PriceLevel>(entity =>
        {
            entity.ToTable("price_levels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.MarkupPercentage).HasPrecision(5, 2).IsRequired();
        });

        modelBuilder.Entity<ProductPrice>(entity =>
        {
            entity.ToTable("product_prices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasOne(e => e.Product).WithMany(e => e.Prices)
                .HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PriceLevel).WithMany(e => e.ProductPrices)
                .HasForeignKey(e => e.PriceLevelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ProductId, e.PriceLevelId }).IsUnique();
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.ToTable("inventory_movements");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.UpdatedAt);
            entity.Property(e => e.MovementType)
                .HasColumnName("movement_type");
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.CostPerUnit).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.HasOne(e => e.Product).WithMany(e => e.InventoryMovements)
                .HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Creator).WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("inventory_balances");
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.AvailableQuantity).IsRequired();
            entity.Property(e => e.ReservedQuantity).IsRequired();
            entity.HasOne(e => e.LastMovement).WithMany().HasForeignKey(e => e.LastMovementId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("offers");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.UpdatedAt);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OfferType)
                .HasColumnName("offer_type");
            entity.Property(e => e.Status)
                .HasColumnName("status");
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.MinPurchaseAmount).HasPrecision(18, 2);
            entity.HasOne(e => e.Creator).WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OfferProduct>(entity =>
        {
            entity.ToTable("offer_products");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.UpdatedAt);
            entity.HasOne(e => e.Offer).WithMany(e => e.Products)
                .HasForeignKey(e => e.OfferId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product).WithMany(e => e.OfferProducts)
                .HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.OfferId, e.ProductId }).IsUnique();
        });

        modelBuilder.Entity<OfferCategory>(entity =>
        {
            entity.ToTable("offer_categories");
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.UpdatedAt);
            entity.HasOne(e => e.Offer).WithMany(e => e.Categories)
                .HasForeignKey(e => e.OfferId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Category).WithMany()
                .HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.OfferId, e.CategoryId }).IsUnique();
        });

        modelBuilder.Entity<OfferApplication>(entity =>
        {
            entity.ToTable("offer_applications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.HasOne(e => e.Offer).WithMany(e => e.Applications)
                .HasForeignKey(e => e.OfferId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.OrderItem).WithMany().HasForeignKey(e => e.OrderItemId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Ignore(e => e.UpdatedAt);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(50);
            entity.Property(e => e.AccountNumber).HasMaxLength(100);
            entity.Property(e => e.BankName).HasMaxLength(255);
            entity.Property(e => e.AccountHolderName).HasMaxLength(255);
            entity.Property(e => e.WalletProvider).HasMaxLength(255);
            entity.Property(e => e.WalletNumber).HasMaxLength(100);
            entity.Property(e => e.QrCodeUrl).HasMaxLength(500);
            entity.Property(e => e.Instructions).HasMaxLength(2000);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Ignore(e => e.UpdatedAt);
        });

        modelBuilder.Entity<PaymentMethodType>(entity =>
        {
            entity.ToTable("payment_method_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Label).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Ignore(e => e.UpdatedAt);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.Property(e => e.Value);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DisplayOrder).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Ignore(e => e.UpdatedAt);
        });

        // Configure all DateTime properties to use timestamp with time zone and convert to UTC
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entityTypeName = entityType.ClrType.Name;
            var ignoredMembers = entityType.GetIgnoredMembers().ToList();
            
            foreach (var property in entityType.GetProperties())
            {
                // Skip ignored properties
                if (ignoredMembers.Contains(property.Name))
                {
                    continue;
                }
                
                // Skip UpdatedAt for entities that don't have it
                if (property.Name == "UpdatedAt" && entitiesWithoutUpdatedAt.Contains(entityTypeName))
                {
                    continue;
                }
                
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetColumnType("timestamp with time zone");
                    property.SetValueConverter(DateTimeValueConverter.Create());
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                    property.SetValueConverter(DateTimeValueConverter.CreateNullable());
                }
            }
        }

        // Apply snake_case naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Convert table names
            var tableName = entity.GetTableName();
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = entity.ClrType.Name;
            }
            entity.SetTableName(ToSnakeCase(tableName));

            // Convert property names (excluir propiedades ignoradas)
            var entityTypeName = entity.ClrType.Name;
            var ignoredMembers = entity.GetIgnoredMembers().ToList();
            
            foreach (var property in entity.GetProperties().ToList())
            {
                if (property.Name == "UpdatedAt" && entitiesWithoutUpdatedAt.Contains(entityTypeName))
                {
                    continue;
                }
                
                if (ignoredMembers.Contains(property.Name))
                {
                    continue;
                }
                
                try
                {
                    var storeObjectId = StoreObjectIdentifier.Table(entity.GetTableName()!, null);
                    var columnName = property.GetColumnName(storeObjectId);
                    
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        property.SetColumnName(ToSnakeCase(columnName), storeObjectId);
                    }
                }
                catch
                {
                }
            }

            // Convert keys
            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()));
            }

            // Convert foreign keys
            foreach (var key in entity.GetForeignKeys())
            {
                key.SetConstraintName(ToSnakeCase(key.GetConstraintName()));
            }

            // Convert indexes
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
            }
        }
    }

    private string ToSnakeCase(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input!;
        
        var startUnderscores = System.Text.RegularExpressions.Regex.Match(input, @"^_+");
        return startUnderscores + System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }

    private static string ConvertMovementTypeToPostgres(InventoryMovementType type)
    {
        return type switch
        {
            InventoryMovementType.Purchase => "purchase",
            InventoryMovementType.Sale => "sale",
            InventoryMovementType.Return => "return",
            InventoryMovementType.Adjustment => "adjustment",
            InventoryMovementType.TransferIn => "transfer_in",
            InventoryMovementType.TransferOut => "transfer_out",
            InventoryMovementType.Damage => "damage",
            InventoryMovementType.Expired => "expired",
            InventoryMovementType.InitialStock => "initial_stock",
            _ => throw new ArgumentException($"Unknown movement type: {type}")
        };
    }

    private static InventoryMovementType ConvertPostgresToMovementType(string value)
    {
        return value switch
        {
            "purchase" => InventoryMovementType.Purchase,
            "sale" => InventoryMovementType.Sale,
            "return" => InventoryMovementType.Return,
            "adjustment" => InventoryMovementType.Adjustment,
            "transfer_in" => InventoryMovementType.TransferIn,
            "transfer_out" => InventoryMovementType.TransferOut,
            "damage" => InventoryMovementType.Damage,
            "expired" => InventoryMovementType.Expired,
            "initial_stock" => InventoryMovementType.InitialStock,
            _ => throw new ArgumentException($"Unknown movement type: {value}")
        };
    }
}
