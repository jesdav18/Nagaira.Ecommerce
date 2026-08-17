using System.Net;

namespace Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

public class MetaCatalogApiException : Exception
{
    public MetaCatalogApiException(
        HttpStatusCode? httpStatusCode,
        string safeMessage,
        bool isTransient,
        string? metaErrorCode = null,
        string? metaErrorSubcode = null,
        string? requestId = null,
        Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        HttpStatusCode = httpStatusCode;
        SafeMessage = safeMessage;
        IsTransient = isTransient;
        MetaErrorCode = metaErrorCode;
        MetaErrorSubcode = metaErrorSubcode;
        RequestId = requestId;
    }

    public HttpStatusCode? HttpStatusCode { get; }
    public string? MetaErrorCode { get; }
    public string? MetaErrorSubcode { get; }
    public bool IsTransient { get; }
    public string SafeMessage { get; }
    public string? RequestId { get; }
}
