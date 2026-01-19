namespace Nagaira.Ecommerce.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? PriceLevelId { get; set; }
    public bool IsActive { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public PriceLevel? PriceLevel { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}

public enum UserRole
{
    Customer = 1,
    Admin = 2,
    SuperAdmin = 3
}
