using Microsoft.Extensions.Options;
using ScopeSeal.Administration.Configuration;
using ScopeSeal.Administration.Services;

namespace ScopeSeal.Api.Authorization;

public static class AdminOperatorAuth
{
    public const string OperatorKeyHeader = "X-Platform-Operator-Key";

    public static bool IsAuthorized(HttpRequest request, AdministrationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OperatorApiKey))
        {
            return false;
        }

        if (!request.Headers.TryGetValue(OperatorKeyHeader, out var values))
        {
            return false;
        }

        return string.Equals(values.ToString(), options.OperatorApiKey, StringComparison.Ordinal);
    }
}
