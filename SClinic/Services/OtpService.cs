using Microsoft.Extensions.Caching.Memory;

namespace SClinic.Services;

/// <summary>In-memory OTP service. 6-digit, 5-min TTL, max 3 attempts.</summary>
public class OtpService(IMemoryCache cache, ILogger<OtpService> logger)
{
    private const int TtlMinutes  = 5;
    private const int MaxAttempts = 3;

    private record OtpEntry(string Code, int Attempts);

    private static string CacheKey(string phone) => $"otp:{phone.ToLowerInvariant()}";

    public string Generate(string phone)
    {
        var code = Random.Shared.Next(100_000, 999_999).ToString();
        cache.Set(CacheKey(phone), new OtpEntry(code, 0), TimeSpan.FromMinutes(TtlMinutes));
        logger.LogWarning("[DEV-OTP] {Phone} → {Code}", phone, code);
        return code;
    }

    public bool Verify(string phone, string input)
    {
        if (!cache.TryGetValue(CacheKey(phone), out OtpEntry? e) || e is null) return false;

        if (e.Attempts >= MaxAttempts)
        {
            cache.Remove(CacheKey(phone));
            return false;
        }

        if (e.Code == input.Trim())
        {
            cache.Remove(CacheKey(phone)); // invalidate after success
            return true;
        }

        // Wrong code — increment attempt count
        cache.Set(CacheKey(phone), e with { Attempts = e.Attempts + 1 }, TimeSpan.FromMinutes(TtlMinutes));
        return false;
    }
}
