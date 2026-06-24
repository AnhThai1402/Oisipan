using System.Text.Json;

namespace FrontendMvc.Extensions;

public static class SessionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static void SetJson<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value, JsonOptions));
    }

    public static T? GetJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? default : JsonSerializer.Deserialize<T>(value, JsonOptions);
    }
}
