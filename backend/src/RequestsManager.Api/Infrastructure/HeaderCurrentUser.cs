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
            var value = accessor.HttpContext?.Request.Headers[HeaderName].ToString().Trim();
            if (string.IsNullOrEmpty(value)) return "anonymous";
            return value.Length > MaxLength ? value[..MaxLength] : value;
        }
    }
}
