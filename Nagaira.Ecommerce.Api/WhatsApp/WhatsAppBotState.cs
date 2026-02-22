using System.Collections.Concurrent;

namespace Nagaira.Ecommerce.Api.WhatsApp;

public enum BotMode
{
    Menu,
    AwaitingSearch,
    AwaitingQuantity,
    Human
}

public class QuoteItem
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class BotSession
{
    public BotMode Mode { get; set; } = BotMode.Menu;
    public DateTime LastInteractionUtc { get; set; } = DateTime.UtcNow;
    public Guid? PendingProductId { get; set; }
    public string PendingProductName { get; set; } = string.Empty;
    public decimal PendingProductPrice { get; set; }
    public List<QuoteItem> Quote { get; } = new();
    public Dictionary<int, Guid> CategoryOptions { get; } = new();
    public Dictionary<int, Guid> ProductOptions { get; } = new();
}

public interface IWhatsAppBotState
{
    BotSession GetSession(string phone);
    void ClearSession(string phone);
}

public class InMemoryWhatsAppBotState : IWhatsAppBotState
{
    private readonly ConcurrentDictionary<string, BotSession> _sessions = new();

    public BotSession GetSession(string phone)
    {
        return _sessions.GetOrAdd(phone, _ => new BotSession());
    }

    public void ClearSession(string phone)
    {
        _sessions.TryRemove(phone, out _);
    }
}
