using System.Text.Json;

namespace Oishipan.Services;

public class ShippingService : IShippingService
{
    private readonly HttpClient _httpClient;

    // Tọa độ cửa hàng Oishipan (Tòa FPT Polytechnic Cần Thơ)
    private const double StoreLat = 10.007624;
    private const double StoreLon = 105.753381;

    public ShippingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Nominatim yêu cầu User-Agent hợp lệ kèm thông tin liên hệ
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OishipanApp/1.0 (contact@oishipan.com)");
    }

    public async Task<decimal> CalculateDistanceAsync(string address)
    {
        var addressesToTry = new List<string> { address };
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        // Thêm các biến thể địa chỉ ít chi tiết hơn (bỏ dần phần số nhà, tên đường...)
        if (parts.Length > 1) addressesToTry.Add(string.Join(", ", parts.Skip(1)));
        if (parts.Length > 2) addressesToTry.Add(string.Join(", ", parts.Skip(2)));

        foreach (var tryAddr in addressesToTry)
        {
            try
            {
                var url = $"https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates?f=json&singleLine={Uri.EscapeDataString(tryAddr)}&maxLocations=1";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var results = JsonSerializer.Deserialize<JsonElement>(content);

                    if (results.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
                    {
                        var firstResult = candidates[0];
                        if (firstResult.TryGetProperty("location", out var location))
                        {
                            if (location.TryGetProperty("y", out var latProp) && location.TryGetProperty("x", out var lonProp) &&
                                latProp.TryGetDouble(out double lat) && lonProp.TryGetDouble(out double lon))
                            {
                                return (decimal)CalculateHaversineDistance(StoreLat, StoreLon, lat, lon);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore and try the next one
            }
        }

        // Không thể xác định vị trí
        return -1;
    }

    public (decimal ShippingFee, decimal SurchargeFee) CalculateFees(decimal distanceInKm)
    {
        if (distanceInKm < 0) return (0, 0); // Không tìm thấy địa chỉ

        if (distanceInKm <= 5)
        {
            return (0, 0);
        }
        if (distanceInKm <= 10)
        {
            return (20000, 0);
        }
        if (distanceInKm <= 15)
        {
            return (25000, 0);
        }

        return (-1, -1); // Vượt quá 15km (không giao hàng)
    }

    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Bán kính Trái Đất theo km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }
}
