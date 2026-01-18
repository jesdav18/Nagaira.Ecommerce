using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Application.Interfaces;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(Order order, User user);
    Task SendWelcomeAsync(User user);
}
