using Microsoft.Extensions.Options;
using Nagaira.Ecommerce.Application.MetaCatalog;

namespace Nagaira.Ecommerce.Infrastructure.Integrations.MetaCatalog;

public class MetaCatalogOptionsValidator : IValidateOptions<MetaCatalogOptions>
{
    public ValidateOptionsResult Validate(string? name, MetaCatalogOptions options)
    {
        if (!options.SyncEnabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
        {
            failures.Add("MetaCatalog:ApiBaseUrl is required when SyncEnabled is true.");
        }

        if (string.IsNullOrWhiteSpace(options.GraphApiVersion))
        {
            failures.Add("MetaCatalog:GraphApiVersion is required when SyncEnabled is true.");
        }

        if (string.IsNullOrWhiteSpace(options.CatalogId))
        {
            failures.Add("MetaCatalog:CatalogId is required when SyncEnabled is true.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            failures.Add("MetaCatalog:AccessToken is required when SyncEnabled is true.");
        }

        if (options.BatchSize <= 0)
        {
            failures.Add("MetaCatalog:BatchSize must be greater than zero.");
        }

        if (options.RequestTimeoutSeconds <= 0)
        {
            failures.Add("MetaCatalog:RequestTimeoutSeconds must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
