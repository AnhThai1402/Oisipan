using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontendMvc.Extensions;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static async Task<T?> GetFromJsonAsyncWithOptions<T>(this HttpClient client, string requestUri)
    {
        try
        {
            using var response = await client.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(contentStream, JsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing response from {requestUri}: {ex.Message}");
            return default;
        }
    }

    public static async Task<HttpResponseMessage> PatchAsJsonAsyncWithOptions<T>(this HttpClient client, string requestUri, T value)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(value, JsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");

        return await client.PatchAsync(requestUri, content);
    }
}
