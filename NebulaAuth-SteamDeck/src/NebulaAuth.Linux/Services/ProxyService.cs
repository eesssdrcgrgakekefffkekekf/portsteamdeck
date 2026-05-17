using Newtonsoft.Json;
using NebulaAuth.Linux.Models;
using NLog;

namespace NebulaAuth.Linux.Services;

/// <summary>
/// Manages proxy list persistence and selection
/// </summary>
public class ProxyService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string ProxiesFileName = "proxies.json";
    private readonly List<MaProxy> _proxies = new();

    public IReadOnlyList<MaProxy> Proxies => _proxies;
    public MaProxy? CurrentProxy { get; set; }
    public event Action? ProxiesChanged;

    public ProxyService()
    {
        Load();
    }

    private string GetProxiesPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, ProxiesFileName);
    }

    public void Load()
    {
        var path = GetProxiesPath();
        if (!File.Exists(path)) return;

        try
        {
            var json = File.ReadAllText(path);
            var proxies = JsonConvert.DeserializeObject<List<MaProxy>>(json);
            if (proxies != null)
            {
                _proxies.Clear();
                _proxies.AddRange(proxies);
                ProxiesChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load proxies");
        }
    }

    public void Save()
    {
        try
        {
            var path = GetProxiesPath();
            var json = JsonConvert.SerializeObject(_proxies, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save proxies");
        }
    }

    public void AddProxy(MaProxy proxy)
    {
        _proxies.Add(proxy);
        Save();
        ProxiesChanged?.Invoke();
    }

    public void RemoveProxy(MaProxy proxy)
    {
        _proxies.Remove(proxy);
        if (CurrentProxy == proxy) CurrentProxy = null;
        Save();
        ProxiesChanged?.Invoke();
    }

    public System.Net.WebProxy? GetWebProxy(MaProxy? proxy = null)
    {
        proxy ??= CurrentProxy;
        if (proxy == null) return null;

        var webProxy = new System.Net.WebProxy(proxy.Host, proxy.Port);
        if (!string.IsNullOrEmpty(proxy.Username))
        {
            webProxy.Credentials = new System.Net.NetworkCredential(proxy.Username, proxy.Password);
        }
        return webProxy;
    }
}
