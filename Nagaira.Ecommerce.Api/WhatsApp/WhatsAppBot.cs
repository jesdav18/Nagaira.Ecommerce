using System.Globalization;
using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Application.DTOs;
using Nagaira.Ecommerce.Application.Interfaces;

namespace Nagaira.Ecommerce.Api.WhatsApp;

public record IncomingMessage(string From, string? Text, string? ButtonId, string? ListId);

public interface IWhatsAppBot
{
    Task HandleMessageAsync(IncomingMessage message, CancellationToken ct = default);
}

public class WhatsAppBot : IWhatsAppBot
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IWhatsAppClient _client;
    private readonly IWhatsAppBotState _state;
    private readonly WhatsAppOptions _options;

    private static readonly string[] HumanKeywords = { "asesor", "vendedor", "humano" };

    public WhatsAppBot(
        IProductService productService,
        ICategoryService categoryService,
        IWhatsAppClient client,
        IWhatsAppBotState state,
        IOptions<WhatsAppOptions> options)
    {
        _productService = productService;
        _categoryService = categoryService;
        _client = client;
        _state = state;
        _options = options.Value;
    }

    public async Task HandleMessageAsync(IncomingMessage message, CancellationToken ct = default)
    {
        if (!_options.BotEnabled || string.IsNullOrWhiteSpace(message.From))
        {
            return;
        }

        var session = _state.GetSession(message.From);
        if (DateTime.UtcNow - session.LastInteractionUtc > TimeSpan.FromHours(6))
        {
            session.Mode = BotMode.Menu;
            session.Quote.Clear();
            session.PendingProductId = null;
        }
        session.LastInteractionUtc = DateTime.UtcNow;

        var text = (message.Text ?? string.Empty).Trim();
        var actionId = message.ButtonId ?? message.ListId ?? string.Empty;

        if (_options.HumanHandoffEnabled && IsHumanKeyword(text))
        {
            session.Mode = BotMode.Human;
            await _client.SendTextAsync(message.From, "Un vendedor te atendera en este chat. Escribi BOT para volver al menu.", ct);
            return;
        }

        if (session.Mode == BotMode.Human)
        {
            if (text.Equals("bot", StringComparison.OrdinalIgnoreCase) || text.Equals("menu", StringComparison.OrdinalIgnoreCase))
            {
                session.Mode = BotMode.Menu;
                await SendMainMenuAsync(message.From, ct);
            }
            return;
        }

        if (TryResolveOption(text, session.CategoryOptions, out var resolvedCategoryId))
        {
            session.CategoryOptions.Clear();
            await SendProductsByCategoryAsync(message.From, resolvedCategoryId, ct);
            return;
        }

        if (TryResolveOption(text, session.ProductOptions, out var resolvedProductId))
        {
            session.ProductOptions.Clear();
            await SendProductDetailAsync(message.From, resolvedProductId, session, ct);
            return;
        }

        if (TryParseActionFromText(text, "agregar", out var addId))
        {
            session.PendingProductId = addId;
            session.Mode = BotMode.AwaitingQuantity;
            await _client.SendTextAsync(message.From, "Cuantos queres agregar?", ct);
            return;
        }

        if (TryParseActionFromText(text, "ver", out var viewId))
        {
            await SendProductDetailAsync(message.From, viewId, session, ct);
            return;
        }

        if (IsMenuCommand(text, actionId))
        {
            session.Mode = BotMode.Menu;
            await SendMainMenuAsync(message.From, ct);
            return;
        }

        if (actionId.StartsWith("cat:", StringComparison.OrdinalIgnoreCase))
        {
            var categoryId = actionId["cat:".Length..];
            if (Guid.TryParse(categoryId, out var catId))
            {
                await SendProductsByCategoryAsync(message.From, catId, ct);
            }
            return;
        }

        if (actionId.StartsWith("prod:", StringComparison.OrdinalIgnoreCase))
        {
            var productId = actionId["prod:".Length..];
            if (Guid.TryParse(productId, out var prodId))
            {
                await SendProductDetailAsync(message.From, prodId, session, ct);
            }
            return;
        }

        if (actionId.StartsWith("add:", StringComparison.OrdinalIgnoreCase))
        {
            var productId = actionId["add:".Length..];
            if (Guid.TryParse(productId, out var prodId))
            {
                session.PendingProductId = prodId;
                session.Mode = BotMode.AwaitingQuantity;
                await _client.SendTextAsync(message.From, "Cuantos queres agregar?", ct);
            }
            return;
        }

        if (actionId.Equals("menu", StringComparison.OrdinalIgnoreCase))
        {
            await SendMainMenuAsync(message.From, ct);
            return;
        }

        if (actionId.Equals("categories", StringComparison.OrdinalIgnoreCase))
        {
            await SendCategoriesAsync(message.From, ct);
            return;
        }

        if (actionId.Equals("search", StringComparison.OrdinalIgnoreCase))
        {
            session.Mode = BotMode.AwaitingSearch;
            await _client.SendTextAsync(message.From, "Escribi el nombre o SKU del producto:", ct);
            return;
        }

        if (actionId.Equals("offers", StringComparison.OrdinalIgnoreCase))
        {
            await _client.SendTextAsync(message.From, "Por ahora no tenemos ofertas disponibles.", ct);
            return;
        }

        if (actionId.Equals("human", StringComparison.OrdinalIgnoreCase) && _options.HumanHandoffEnabled)
        {
            session.Mode = BotMode.Human;
            await _client.SendTextAsync(message.From, "Un vendedor te atendera en este chat. Escribi BOT para volver al menu.", ct);
            return;
        }

        if (actionId.Equals("quote", StringComparison.OrdinalIgnoreCase))
        {
            await SendQuoteAsync(message.From, session, ct);
            return;
        }

        if (actionId.Equals("finish", StringComparison.OrdinalIgnoreCase))
        {
            await SendQuoteAsync(message.From, session, ct, includeFinish: true);
            return;
        }

        if (session.Mode == BotMode.AwaitingSearch)
        {
            session.Mode = BotMode.Menu;
            await SendSearchResultsAsync(message.From, text, ct);
            return;
        }

        if (session.Mode == BotMode.AwaitingQuantity && session.PendingProductId.HasValue)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty) && qty > 0)
            {
                await AddPendingProductAsync(message.From, session, qty, ct);
            }
            else
            {
                await _client.SendTextAsync(message.From, "Cantidad invalida. Escribi un numero.", ct);
            }
            return;
        }

        if (_options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                await SendCategoriesAsync(message.From, ct);
                return;
            }
            if (text.Equals("2", StringComparison.OrdinalIgnoreCase))
            {
                session.Mode = BotMode.AwaitingSearch;
                await _client.SendTextAsync(message.From, "Escribi el nombre o SKU del producto:", ct);
                return;
            }
            if (text.Equals("3", StringComparison.OrdinalIgnoreCase))
            {
                await _client.SendTextAsync(message.From, "Por ahora no tenemos ofertas disponibles.", ct);
                return;
            }
            if (text.Equals("4", StringComparison.OrdinalIgnoreCase) && _options.HumanHandoffEnabled)
            {
                session.Mode = BotMode.Human;
                await _client.SendTextAsync(message.From, "Un vendedor te atendera en este chat. Escribi BOT para volver al menu.", ct);
                return;
            }
        }

        if (text.Equals("categorias", StringComparison.OrdinalIgnoreCase))
        {
            await SendCategoriesAsync(message.From, ct);
            return;
        }

        if (text.Equals("buscar", StringComparison.OrdinalIgnoreCase))
        {
            session.Mode = BotMode.AwaitingSearch;
            await _client.SendTextAsync(message.From, "Escribi el nombre o SKU del producto:", ct);
            return;
        }

        if (text.Equals("cotizacion", StringComparison.OrdinalIgnoreCase) || text.Equals("ver cotizacion", StringComparison.OrdinalIgnoreCase))
        {
            await SendQuoteAsync(message.From, session, ct);
            return;
        }

        if (text.Equals("vendedor", StringComparison.OrdinalIgnoreCase) || text.Equals("asesor", StringComparison.OrdinalIgnoreCase))
        {
            session.Mode = BotMode.Human;
            await _client.SendTextAsync(message.From, "Un vendedor te atendera en este chat. Escribi BOT para volver al menu.", ct);
            return;
        }

        await SendMainMenuAsync(message.From, ct);
    }

    private bool IsHumanKeyword(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var tokens = text.ToLowerInvariant();
        var keywords = (_options.HumanHandoffKeywords ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var key in keywords.Length > 0 ? keywords : HumanKeywords)
        {
            if (tokens.Contains(key)) return true;
        }
        return false;
    }

    private static bool IsMenuCommand(string text, string actionId)
    {
        if (!string.IsNullOrWhiteSpace(actionId)) return false;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Equals("menu", StringComparison.OrdinalIgnoreCase)
            || text.Equals("inicio", StringComparison.OrdinalIgnoreCase);
    }

    private Task SendMainMenuAsync(string to, CancellationToken ct)
    {
        if (_options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var text = "Menu:\n1) Ver categorias\n2) Buscar producto\n3) Ofertas\n4) Hablar con vendedor\n\nResponde con el numero.";
            return _client.SendTextAsync(to, text, ct);
        }

        var sections = new[]
        {
            new WhatsAppListSection
            {
                Title = "Menu",
                Rows = new List<WhatsAppListRow>
                {
                    new() { Id = "categories", Title = "Ver categorias" },
                    new() { Id = "search", Title = "Buscar producto" },
                    new() { Id = "offers", Title = "Ofertas" },
                    new() { Id = "human", Title = "Hablar con vendedor" }
                }
            }
        };

        return _client.SendListAsync(to, "Bienvenido. Que queres hacer?", "Opciones", sections, ct);
    }

    private async Task SendCategoriesAsync(string to, CancellationToken ct)
    {
        var categories = (await _categoryService.GetAllActiveCategoriesAsync()).ToList();
        if (categories.Count == 0)
        {
            await _client.SendTextAsync(to, "No hay categorias disponibles.", ct);
            return;
        }

        if (_options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var lines = new List<string> { "Categorias:" };
            sessionFor(to).CategoryOptions.Clear();
            var index = 1;
            foreach (var category in categories.Take(9))
            {
                sessionFor(to).CategoryOptions[index] = category.Id;
                lines.Add($"{index}) {category.Name}");
                index++;
            }
            lines.Add("Responde con el numero.");
            await _client.SendTextAsync(to, string.Join("\n", lines), ct);
            return;
        }

        var sections = new List<WhatsAppListSection>();
        var chunk = categories.Take(30).ToList();
        var rows = chunk.Select(c => new WhatsAppListRow
        {
            Id = $"cat:{c.Id}",
            Title = c.Name,
            Description = c.Description?.Length > 60 ? c.Description[..60] + "..." : c.Description
        }).ToList();

        sections.Add(new WhatsAppListSection { Title = "Categorias", Rows = rows });
        await _client.SendListAsync(to, "Elegi una categoria:", "Ver categorias", sections, ct);
    }

    private async Task SendProductsByCategoryAsync(string to, Guid categoryId, CancellationToken ct)
    {
        var products = (await _productService.GetProductsByCategoryAsync(categoryId)).ToList();
        if (products.Count == 0)
        {
            await _client.SendTextAsync(to, "No hay productos en esta categoria.", ct);
            return;
        }

        if (_options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var lines = new List<string> { "Productos:" };
            sessionFor(to).ProductOptions.Clear();
            var index = 1;
            foreach (var product in products.Take(9))
            {
                var price = GetMinoristaPrice(product);
                var desc = price.HasValue ? $"L. {price.Value:0.00}" : "Sin precio";
                sessionFor(to).ProductOptions[index] = product.Id;
                lines.Add($"{index}) {product.Name} - {desc}");
                index++;
            }
            lines.Add("Responde con el numero.");
            await _client.SendTextAsync(to, string.Join("\n", lines), ct);
            return;
        }

        var rows = products.Take(10).Select(p =>
        {
            var price = GetMinoristaPrice(p);
            var desc = price.HasValue ? $"L. {price.Value:0.00}" : "Sin precio";
            return new WhatsAppListRow { Id = $"prod:{p.Id}", Title = p.Name, Description = desc };
        }).ToList();

        var sections = new[]
        {
            new WhatsAppListSection { Title = "Productos", Rows = rows }
        };

        await _client.SendListAsync(to, "Productos disponibles:", "Ver productos", sections, ct);
    }

    private async Task SendSearchResultsAsync(string to, string term, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            await _client.SendTextAsync(to, "No recibi el termino de busqueda.", ct);
            return;
        }

        var products = (await _productService.SearchProductsAsync(term)).ToList();
        if (products.Count == 0)
        {
            await _client.SendTextAsync(to, "No encontramos productos con ese termino.", ct);
            return;
        }

        if (_options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var lines = new List<string> { "Resultados:" };
            sessionFor(to).ProductOptions.Clear();
            var index = 1;
            foreach (var product in products.Take(9))
            {
                var price = GetMinoristaPrice(product);
                var desc = price.HasValue ? $"L. {price.Value:0.00}" : "Sin precio";
                sessionFor(to).ProductOptions[index] = product.Id;
                lines.Add($"{index}) {product.Name} - {desc}");
                index++;
            }
            lines.Add("Responde con el numero.");
            await _client.SendTextAsync(to, string.Join("\n", lines), ct);
            return;
        }

        var rows = products.Take(10).Select(p =>
        {
            var price = GetMinoristaPrice(p);
            var desc = price.HasValue ? $"L. {price.Value:0.00}" : "Sin precio";
            return new WhatsAppListRow { Id = $"prod:{p.Id}", Title = p.Name, Description = desc };
        }).ToList();

        var sections = new[]
        {
            new WhatsAppListSection { Title = "Resultados", Rows = rows }
        };

        await _client.SendListAsync(to, "Resultados de busqueda:", "Ver productos", sections, ct);
    }

    private async Task SendProductDetailAsync(string to, Guid productId, BotSession session, CancellationToken ct)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
        {
            await _client.SendTextAsync(to, "Producto no encontrado.", ct);
            return;
        }

        var imageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
            ?? product.Images.FirstOrDefault()?.ImageUrl;

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            await _client.SendImageAsync(to, imageUrl, null, ct);
        }

        var price = GetMinoristaPrice(product);
        var priceText = price.HasValue ? $"L. {price.Value:0.00}" : "Sin precio";
        var msg = $"{product.Name}\nSKU: {product.Sku}\nPrecio minorista: {priceText}";
        if (_options.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        {
            msg += $"\n\nPara agregar: escribi AGREGAR {product.Id}\nPara cotizacion: escribi COTIZACION";
            await _client.SendTextAsync(to, msg, ct);
        }
        else
        {
            await _client.SendButtonsAsync(
                to,
                msg,
                new[]
                {
                    ($"add:{product.Id}", "Agregar a cotizacion"),
                    ("quote", "Ver cotizacion"),
                    ("menu", "Menu")
                },
                ct);
        }

        session.PendingProductName = product.Name;
        session.PendingProductPrice = price ?? 0;
    }

    private async Task AddPendingProductAsync(string to, BotSession session, int quantity, CancellationToken ct)
    {
        var productId = session.PendingProductId;
        if (!productId.HasValue)
        {
            await _client.SendTextAsync(to, "No hay producto pendiente.", ct);
            return;
        }

        var product = await _productService.GetProductByIdAsync(productId.Value);
        if (product == null)
        {
            await _client.SendTextAsync(to, "Producto no encontrado.", ct);
            return;
        }

        var price = GetMinoristaPrice(product) ?? 0;
        var existing = session.Quote.FirstOrDefault(q => q.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            session.Quote.Add(new QuoteItem
            {
                ProductId = product.Id,
                Name = product.Name,
                UnitPrice = price,
                Quantity = quantity
            });
        }

        session.PendingProductId = null;
        session.Mode = BotMode.Menu;

        await _client.SendButtonsAsync(
            to,
            "Agregado a la cotizacion. Que deseas hacer?",
            new[]
            {
                ("categories", "Ver categorias"),
                ("quote", "Ver cotizacion"),
                ("finish", "Finalizar")
            },
            ct);
    }

    private async Task SendQuoteAsync(string to, BotSession session, CancellationToken ct, bool includeFinish = false)
    {
        if (session.Quote.Count == 0)
        {
            await _client.SendTextAsync(to, "Tu cotizacion esta vacia.", ct);
            return;
        }

        var lines = session.Quote.Select(q =>
            $"- {q.Name} x{q.Quantity} = L. {(q.UnitPrice * q.Quantity):0.00}");
        var total = session.Quote.Sum(q => q.UnitPrice * q.Quantity);
        var text = "Cotizacion:\n" + string.Join("\n", lines) + $"\nTotal: L. {total:0.00}";

        if (includeFinish)
        {
            text += "\n\nSi queres hablar con un vendedor, escribi VENDEDOR.";
        }

        await _client.SendTextAsync(to, text, ct);
    }

    private static decimal? GetMinoristaPrice(ProductDto product)
    {
        if (product.Prices == null || product.Prices.Count == 0) return null;
        var minorista = product.Prices.FirstOrDefault(p =>
            (p.PriceLevelName ?? string.Empty).ToLowerInvariant().Contains("minorista") && p.IsActive);
        if (minorista != null) return minorista.Price;
        var active = product.Prices.Where(p => p.IsActive).OrderBy(p => p.MinQuantity).FirstOrDefault();
        return active?.Price;
    }

    private static bool TryParseActionFromText(string text, string verb, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;
        if (!parts[0].Equals(verb, StringComparison.OrdinalIgnoreCase)) return false;
        return Guid.TryParse(parts[1], out id);
    }

    private static bool TryResolveOption(string text, Dictionary<int, Guid> options, out Guid id)
    {
        id = Guid.Empty;
        if (options.Count == 0) return false;
        if (!int.TryParse(text.Trim(), out var index)) return false;
        return options.TryGetValue(index, out id);
    }

    private BotSession sessionFor(string phone) => _state.GetSession(phone);
}
