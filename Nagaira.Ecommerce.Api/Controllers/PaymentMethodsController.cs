using Microsoft.AspNetCore.Mvc;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.Controllers;

[ApiController]
[Route("api/payment-methods")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethodService;

    public PaymentMethodsController(IPaymentMethodService paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
    }

    [HttpGet("active")]
    public async Task<ActionResult> GetActivePaymentMethods()
    {
        var paymentMethods = await _paymentMethodService.GetActivePaymentMethodsAsync();
        return Ok(paymentMethods);
    }
}

