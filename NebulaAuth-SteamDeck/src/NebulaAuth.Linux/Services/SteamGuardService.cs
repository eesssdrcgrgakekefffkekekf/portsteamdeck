using System.Security.Cryptography;
using NebulaAuth.Linux.Models;

namespace NebulaAuth.Linux.Services;

/// <summary>
/// Generates Steam Guard 2FA codes from shared_secret.
/// This is a standalone implementation that doesn't require Windows-specific libraries.
/// </summary>
public class SteamGuardService
{
    private static readonly char[] SteamGuardChars = 
        "23456789BCDFGHJKMNPQRTVWXY".ToCharArray();

    private long _timeOffset;

    public void SetTimeOffset(long offset)
    {
        _timeOffset = offset;
    }

    public long GetSteamTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _timeOffset;
    }

    /// <summary>
    /// Get remaining seconds until code expires (30-second window)
    /// </summary>
    public int GetSecondsUntilExpiry()
    {
        var time = GetSteamTime();
        return 30 - (int)(time % 30);
    }

    /// <summary>
    /// Generates the current Steam Guard code for the given mafile
    /// </summary>
    public string GenerateCode(Mafile mafile)
    {
        return GenerateCode(mafile.SharedSecret);
    }

    /// <summary>
    /// Generates Steam Guard code from shared_secret
    /// </summary>
    public string GenerateCode(string sharedSecret)
    {
        if (string.IsNullOrEmpty(sharedSecret))
            return string.Empty;

        var time = GetSteamTime();
        return GenerateCodeForTime(sharedSecret, time);
    }

    public string GenerateCodeForTime(string sharedSecret, long time)
    {
        if (string.IsNullOrEmpty(sharedSecret))
            return string.Empty;

        byte[] sharedSecretBytes;
        try
        {
            sharedSecretBytes = Convert.FromBase64String(sharedSecret);
        }
        catch
        {
            return string.Empty;
        }

        // Get time interval (30-second windows)
        var timeInterval = time / 30L;

        // Convert to byte array (big-endian)
        var timeBytes = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            timeBytes[i] = (byte)(timeInterval & 0xFF);
            timeInterval >>= 8;
        }

        // HMAC-SHA1
        using var hmac = new HMACSHA1(sharedSecretBytes);
        var hash = hmac.ComputeHash(timeBytes);

        // Dynamic truncation
        var offset = hash[^1] & 0x0F;
        var codeInt = (hash[offset] & 0x7F) << 24 |
                      (hash[offset + 1] & 0xFF) << 16 |
                      (hash[offset + 2] & 0xFF) << 8 |
                      (hash[offset + 3] & 0xFF);

        // Generate 5-character code
        var code = new char[5];
        for (var i = 0; i < 5; i++)
        {
            code[i] = SteamGuardChars[codeInt % SteamGuardChars.Length];
            codeInt /= SteamGuardChars.Length;
        }

        return new string(code);
    }

    /// <summary>
    /// Generates confirmation hash for trade confirmations
    /// </summary>
    public string GenerateConfirmationHash(string identitySecret, long time, string tag)
    {
        if (string.IsNullOrEmpty(identitySecret))
            return string.Empty;

        var identitySecretBytes = Convert.FromBase64String(identitySecret);
        var tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);

        var dataLen = 8 + tagBytes.Length;
        var data = new byte[dataLen];

        // Time (big-endian)
        var timeBytes = BitConverter.GetBytes(time);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);
        Array.Copy(timeBytes, data, 8);

        // Tag
        Array.Copy(tagBytes, 0, data, 8, tagBytes.Length);

        using var hmac = new HMACSHA1(identitySecretBytes);
        var hash = hmac.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Generates a device ID from SteamID (same algorithm as Steam mobile app)
    /// </summary>
    public static string GenerateDeviceId(ulong steamId)
    {
        var hash = SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(steamId.ToString()));
        var hex = Convert.ToHexString(hash).ToLower();
        return $"android:{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..32]}";
    }

    /// <summary>
    /// Aligns local time with Steam servers
    /// </summary>
    public async Task AlignTimeAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var sendTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var response = await client.PostAsync(
                "https://api.steampowered.com/ITwoFactorService/QueryTime/v0001",
                new StringContent("steamid=0"));

            var receiveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var content = await response.Content.ReadAsStringAsync();

            // Parse server_time from JSON response
            var serverTimeStr = ExtractJsonValue(content, "server_time");
            if (long.TryParse(serverTimeStr, out var serverTime))
            {
                var roundTrip = (receiveTime - sendTime) / 2;
                _timeOffset = serverTime - receiveTime + roundTrip;
            }
        }
        catch
        {
            // If time alignment fails, use local time (offset = 0)
            _timeOffset = 0;
        }
    }

    private static string? ExtractJsonValue(string json, string key)
    {
        var keyIndex = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
        if (keyIndex < 0) return null;

        var colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex < 0) return null;

        var start = colonIndex + 1;
        while (start < json.Length && (json[start] == ' ' || json[start] == '"'))
            start++;

        var end = start;
        while (end < json.Length && json[end] != '"' && json[end] != ',' && json[end] != '}')
            end++;

        return json[start..end];
    }
}
