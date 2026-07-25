using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace ScopeSeal.Api.Logging;

public sealed partial class SensitiveDataLogFilter : ILogEventFilter
{
    private static readonly string[] SensitivePropertyNames =
    [
        "password",
        "Password",
        "authorization",
        "Authorization",
        "cookie",
        "Cookie",
        "token",
        "Token",
        "secret",
        "Secret",
        "otp",
        "Otp",
        "apikey",
        "ApiKey"
    ];

    public bool IsEnabled(LogEvent logEvent)
    {
        foreach (var property in logEvent.Properties)
        {
            if (SensitivePropertyNames.Any(name =>
                    property.Key.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        var rendered = logEvent.RenderMessage();
        if (BearerTokenPattern().IsMatch(rendered) ||
            PasswordFieldPattern().IsMatch(rendered))
        {
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(@"""password""\s*:\s*""[^""]+""", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordFieldPattern();
}
