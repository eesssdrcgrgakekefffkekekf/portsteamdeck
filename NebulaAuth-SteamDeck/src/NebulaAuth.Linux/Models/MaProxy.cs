using Newtonsoft.Json;

namespace NebulaAuth.Linux.Models;

public class MaProxy
{
    [JsonProperty("host")]
    public string Host { get; set; } = string.Empty;

    [JsonProperty("port")]
    public int Port { get; set; }

    [JsonProperty("username")]
    public string? Username { get; set; }

    [JsonProperty("password")]
    public string? Password { get; set; }

    [JsonProperty("type")]
    public ProxyType Type { get; set; } = ProxyType.Http;

    [JsonProperty("name")]
    public string? Name { get; set; }

    public override string ToString()
    {
        var auth = !string.IsNullOrEmpty(Username) ? $"{Username}:{Password}@" : "";
        return $"{Type.ToString().ToLower()}://{auth}{Host}:{Port}";
    }

    public static MaProxy? Parse(string proxyString)
    {
        if (string.IsNullOrWhiteSpace(proxyString)) return null;

        try
        {
            var proxy = new MaProxy();
            var str = proxyString.Trim();

            // Determine type
            if (str.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
            {
                proxy.Type = ProxyType.Socks5;
                str = str[9..];
            }
            else if (str.StartsWith("socks4://", StringComparison.OrdinalIgnoreCase))
            {
                proxy.Type = ProxyType.Socks4;
                str = str[9..];
            }
            else if (str.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                proxy.Type = ProxyType.Http;
                str = str[7..];
            }
            else if (str.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                proxy.Type = ProxyType.Http;
                str = str[8..];
            }

            // Parse auth
            if (str.Contains('@'))
            {
                var parts = str.Split('@');
                var authParts = parts[0].Split(':');
                proxy.Username = authParts[0];
                proxy.Password = authParts.Length > 1 ? authParts[1] : null;
                str = parts[1];
            }

            // Parse host:port
            var hostPort = str.Split(':');
            proxy.Host = hostPort[0];
            proxy.Port = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 8080;

            return proxy;
        }
        catch
        {
            return null;
        }
    }
}

public enum ProxyType
{
    Http,
    Socks4,
    Socks5
}
