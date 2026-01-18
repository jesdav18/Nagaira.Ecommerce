using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Nagaira.Ecommerce.Application.Interfaces;
using Nagaira.Ecommerce.Domain.Entities;

namespace Nagaira.Ecommerce.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IAppSettingService _appSettingService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IAppSettingService appSettingService, ILogger<SmtpEmailService> logger)
    {
        _appSettingService = appSettingService;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(Order order, User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var subject = $"Confirmacion de pedido {order.OrderNumber}";
        var body = BuildOrderHtml(order, user);

        try
        {
            await SendEmailAsync(user.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order confirmation email");
        }
    }

    public async Task SendWelcomeAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var subject = "Bienvenido a Nagaira";
        var body = BuildWelcomeHtml(user);

        try
        {
            await SendEmailAsync(user.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email");
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var enabled = await _appSettingService.GetSettingValueAsync("email_enabled");
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var host = await _appSettingService.GetSettingValueAsync("smtp_host");
        var portValue = await _appSettingService.GetSettingValueAsync("smtp_port");
        var username = await _appSettingService.GetSettingValueAsync("smtp_username");
        var password = await _appSettingService.GetSettingValueAsync("smtp_password");
        var fromEmail = await _appSettingService.GetSettingValueAsync("smtp_from_email");
        var fromName = await _appSettingService.GetSettingValueAsync("smtp_from_name");
        var useSslValue = await _appSettingService.GetSettingValueAsync("smtp_use_ssl");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP settings missing. Email not sent.");
            return;
        }

        var port = 587;
        if (!string.IsNullOrWhiteSpace(portValue) && int.TryParse(portValue, out var parsedPort))
        {
            port = parsedPort;
        }

        var useSsl = !string.Equals(useSslValue, "false", StringComparison.OrdinalIgnoreCase);
        var senderEmail = string.IsNullOrWhiteSpace(fromEmail) ? username : fromEmail;
        var senderName = string.IsNullOrWhiteSpace(fromName) ? "Nagaira" : fromName;

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message);
    }

    private static string BuildOrderHtml(Order order, User user)
    {
        var sb = new StringBuilder();
        sb.Append("<h2>Gracias por tu pedido</h2>");
        sb.Append($"<p>Hola {WebUtility.HtmlEncode(user.FirstName)} {WebUtility.HtmlEncode(user.LastName)},</p>");
        sb.Append($"<p>Tu pedido <strong>{order.OrderNumber}</strong> fue recibido.</p>");

        sb.Append("<table style=\"width:100%;border-collapse:collapse;\">");
        sb.Append("<thead><tr>");
        sb.Append("<th style=\"text-align:left;border-bottom:1px solid #ddd;padding:8px;\">Producto</th>");
        sb.Append("<th style=\"text-align:center;border-bottom:1px solid #ddd;padding:8px;\">Cantidad</th>");
        sb.Append("<th style=\"text-align:right;border-bottom:1px solid #ddd;padding:8px;\">Subtotal</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var item in order.Items)
        {
            var name = WebUtility.HtmlEncode(item.Product?.Name ?? "Producto");
            sb.Append("<tr>");
            sb.Append($"<td style=\"padding:8px;\">{name}</td>");
            sb.Append($"<td style=\"text-align:center;padding:8px;\">{item.Quantity}</td>");
            sb.Append($"<td style=\"text-align:right;padding:8px;\">{item.Subtotal:0.00}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");

        sb.Append("<p style=\"margin-top:16px;\">");
        sb.Append($"Subtotal: {order.Subtotal:0.00}<br>");
        sb.Append($"Impuesto: {order.Tax:0.00}<br>");
        sb.Append($"Envio: {order.ShippingCost:0.00}<br>");
        sb.Append($"<strong>Total: {order.Total:0.00}</strong>");
        sb.Append("</p>");

        return sb.ToString();
    }

    private static string BuildWelcomeHtml(User user)
    {
        var sb = new StringBuilder();
        var name = $"{user.FirstName} {user.LastName}".Trim();
        var greetingName = string.IsNullOrWhiteSpace(name) ? "cliente" : WebUtility.HtmlEncode(name);
        sb.Append("<h2>Bienvenido a Nagaira</h2>");
        sb.Append($"<p>Hola {greetingName},</p>");
        sb.Append("<p>Gracias por crear tu cuenta. Ahora puedes acceder a ofertas exclusivas y un checkout mas rapido.</p>");
        sb.Append("<p>Si necesitas ayuda, respondemos a este correo.</p>");
        return sb.ToString();
    }
}
