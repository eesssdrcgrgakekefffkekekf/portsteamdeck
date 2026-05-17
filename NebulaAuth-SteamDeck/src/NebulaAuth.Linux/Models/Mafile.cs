using Newtonsoft.Json;

namespace NebulaAuth.Linux.Models;

/// <summary>
/// Steam Guard mafile - contains all data needed for 2FA code generation and confirmations.
/// Compatible with SDA format.
/// </summary>
public class Mafile
{
    [JsonProperty("shared_secret")]
    public string SharedSecret { get; set; } = string.Empty;

    [JsonProperty("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonProperty("revocation_code")]
    public string RevocationCode { get; set; } = string.Empty;

    [JsonProperty("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonProperty("server_time")]
    public long ServerTime { get; set; }

    [JsonProperty("account_name")]
    public string AccountName { get; set; } = string.Empty;

    [JsonProperty("token_gid")]
    public string TokenGid { get; set; } = string.Empty;

    [JsonProperty("identity_secret")]
    public string IdentitySecret { get; set; } = string.Empty;

    [JsonProperty("secret_1")]
    public string Secret1 { get; set; } = string.Empty;

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonProperty("fully_enrolled")]
    public bool FullyEnrolled { get; set; }

    [JsonProperty("Session")]
    public SessionData? Session { get; set; }

    [JsonProperty("steam_id")]
    public ulong SteamId { get; set; }

    // NebulaAuth extended fields
    [JsonProperty("proxy")]
    public string? Proxy { get; set; }

    [JsonProperty("password")]
    public string? Password { get; set; }

    [JsonProperty("group")]
    public string? Group { get; set; }

    /// <summary>
    /// File path on disk (not serialized)
    /// </summary>
    [JsonIgnore]
    public string FilePath { get; set; } = string.Empty;
}

public class SessionData
{
    [JsonProperty("SessionID")]
    public string? SessionId { get; set; }

    [JsonProperty("SteamLogin")]
    public string? SteamLogin { get; set; }

    [JsonProperty("SteamLoginSecure")]
    public string? SteamLoginSecure { get; set; }

    [JsonProperty("WebCookie")]
    public string? WebCookie { get; set; }

    [JsonProperty("OAuthToken")]
    public string? OAuthToken { get; set; }

    [JsonProperty("SteamID")]
    public ulong SteamId { get; set; }

    [JsonProperty("AccessToken")]
    public string? AccessToken { get; set; }

    [JsonProperty("RefreshToken")]
    public string? RefreshToken { get; set; }
}
