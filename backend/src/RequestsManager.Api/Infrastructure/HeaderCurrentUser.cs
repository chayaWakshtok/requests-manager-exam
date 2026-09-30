using RequestsManager.Application.Abstractions;

namespace RequestsManager.Api.Infrastructure;

/// <summary>
/// The exam has no authentication, so the acting user comes from the X-User-Name header.
/// In production this would read the name claim from the authenticated principal (JWT).
/// </summary>
public sealed class HeaderCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string HeaderName = "X-User-Name";
    private const int MaxLength = 100;

    public string Name
    {
        get
        {
            // Strip control characters: the value is written to the audit table and to logs.
            var value = new string((accessor.HttpContext?.Request.Headers[HeaderName].ToString() ?? string.Empty)
                .Where(c => !char.IsControl(c)).ToArray()).Trim();
            if (value.Length == 0) return "anonymous";
            return value.Length > MaxLength ? value[..MaxLength] : value;
        }
    }
}
