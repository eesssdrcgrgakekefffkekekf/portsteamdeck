using System.Net;
using System.Text.RegularExpressions;
using NebulaAuth.Linux.Models;
using NLog;

namespace NebulaAuth.Linux.Services;

/// <summary>
/// Handles fetching and accepting/declining trade confirmations from Steam
/// </summary>
public class ConfirmationService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly MafileService _mafileService;
    private readonly ProxyService _proxyService;
    private readonly SteamGuardService _steamGuard = new();
    
    private Timer? _autoConfirmTimer;
    private bool _autoConfirmTrades;
    private bool _autoConfirmMarket;

    public event Action<List<Confirmation>>? ConfirmationsUpdated;
    public event Action<string>? Error;

    public ConfirmationService(MafileService mafileService, ProxyService proxyService)
    {
        _mafileService = mafileService;
        _proxyService = proxyService;
    }

    public void StartAutoConfirm(bool trades, bool market, int intervalSeconds)
    {
        _autoConfirmTrades = trades;
        _autoConfirmMarket = market;
        StopAutoConfirm();

        if (!trades && !market) return;

        _autoConfirmTimer = new Timer(
            async _ => await AutoConfirmTick(),
            null,
            TimeSpan.FromSeconds(intervalSeconds),
            TimeSpan.FromSeconds(intervalSeconds));

        Logger.Info("Auto-confirm started: trades={Trades}, market={Market}, interval={Interval}s",
            trades, market, intervalSeconds);
    }

    public void StopAutoConfirm()
    {
        _autoConfirmTimer?.Dispose();
        _autoConfirmTimer = null;
    }

    private async Task AutoConfirmTick()
    {
        foreach (var mafile in _mafileService.Mafiles)
        {
            try
            {
                var confirmations = await GetConfirmations(mafile);
                foreach (var conf in confirmations)
                {
                    var shouldConfirm =
                        (conf.Type == ConfirmationType.Trade && _autoConfirmTrades) ||
                        (conf.Type == ConfirmationType.MarketListing && _autoConfirmMarket);

                    if (shouldConfirm)
                    {
                        await AcceptConfirmation(mafile, conf);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Auto-confirm error for {Account}", mafile.AccountName);
            }
        }
    }

    public async Task<List<Confirmation>> GetConfirmations(Mafile mafile)
    {
        var confirmations = new List<Confirmation>();

        try
        {
            var time = _steamGuard.GetSteamTime();
            var confHash = _steamGuard.GenerateConfirmationHash(mafile.IdentitySecret, time, "conf");
            var deviceId = mafile.DeviceId;

            if (string.IsNullOrEmpty(deviceId))
                deviceId = SteamGuardService.GenerateDeviceId(mafile.SteamId);

            var url = $"https://steamcommunity.com/mobileconf/getlist?" +
                      $"p={Uri.EscapeDataString(deviceId)}" +
                      $"&a={mafile.SteamId}" +
                      $"&k={Uri.EscapeDataString(confHash)}" +
                      $"&t={time}" +
                      $"&m=react" +
                      $"&tag=conf";

            using var handler = new HttpClientHandler();
            var proxy = _proxyService.GetWebProxy();
            if (proxy != null)
            {
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            if (mafile.Session != null)
            {
                handler.CookieContainer = new CookieContainer();
                handler.CookieContainer.Add(new Uri("https://steamcommunity.com"),
                    new Cookie("sessionid", mafile.Session.SessionId ?? ""));
                if (!string.IsNullOrEmpty(mafile.Session.SteamLoginSecure))
                {
                    handler.CookieContainer.Add(new Uri("https://steamcommunity.com"),
                        new Cookie("steamLoginSecure", mafile.Session.SteamLoginSecure));
                }
            }

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; U; Android 4.1.1)");

            var response = await client.GetStringAsync(url);
            confirmations = ParseConfirmations(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to get confirmations for {Account}", mafile.AccountName);
            Error?.Invoke($"Failed to get confirmations: {ex.Message}");
        }

        ConfirmationsUpdated?.Invoke(confirmations);
        return confirmations;
    }

    public async Task<bool> AcceptConfirmation(Mafile mafile, Confirmation confirmation)
    {
        return await SendConfirmationAction(mafile, confirmation, "allow");
    }

    public async Task<bool> DenyConfirmation(Mafile mafile, Confirmation confirmation)
    {
        return await SendConfirmationAction(mafile, confirmation, "cancel");
    }

    private async Task<bool> SendConfirmationAction(Mafile mafile, Confirmation confirmation, string action)
    {
        try
        {
            var time = _steamGuard.GetSteamTime();
            var confHash = _steamGuard.GenerateConfirmationHash(mafile.IdentitySecret, time, action);
            var deviceId = mafile.DeviceId;

            if (string.IsNullOrEmpty(deviceId))
                deviceId = SteamGuardService.GenerateDeviceId(mafile.SteamId);

            var url = "https://steamcommunity.com/mobileconf/ajaxop";
            var postData = $"op={action}" +
                           $"&p={Uri.EscapeDataString(deviceId)}" +
                           $"&a={mafile.SteamId}" +
                           $"&k={Uri.EscapeDataString(confHash)}" +
                           $"&t={time}" +
                           $"&m=react" +
                           $"&tag={action}" +
                           $"&cid={confirmation.Id}" +
                           $"&ck={confirmation.Key}";

            using var handler = new HttpClientHandler();
            var proxy = _proxyService.GetWebProxy();
            if (proxy != null)
            {
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; U; Android 4.1.1)");

            var content = new StringContent(postData, System.Text.Encoding.UTF8,
                "application/x-www-form-urlencoded");
            var response = await client.PostAsync(url, content);
            var responseStr = await response.Content.ReadAsStringAsync();

            var success = responseStr.Contains("\"success\":true", StringComparison.OrdinalIgnoreCase);
            if (success)
            {
                Logger.Info("{Action} confirmation {Id} for {Account}",
                    action, confirmation.Id, mafile.AccountName);
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to {Action} confirmation for {Account}",
                action, mafile.AccountName);
            return false;
        }
    }

    private static List<Confirmation> ParseConfirmations(string json)
    {
        var list = new List<Confirmation>();

        try
        {
            // Simple JSON parsing for confirmation list
            var successMatch = Regex.Match(json, @"""success""\s*:\s*(true|false)");
            if (!successMatch.Success || successMatch.Groups[1].Value != "true")
                return list;

            // Match confirmation objects
            var confMatches = Regex.Matches(json,
                @"""id""\s*:\s*""?(\d+)""?.*?""nonce""\s*:\s*""?(\d+)""?.*?""creator_id""\s*:\s*""?(\d+)""?.*?""type""\s*:\s*(\d+)",
                RegexOptions.Singleline);

            foreach (Match m in confMatches)
            {
                var conf = new Confirmation
                {
                    Id = ulong.Parse(m.Groups[1].Value),
                    Key = ulong.Parse(m.Groups[2].Value),
                    Creator = ulong.Parse(m.Groups[3].Value),
                    Type = (ConfirmationType)int.Parse(m.Groups[4].Value)
                };
                list.Add(conf);
            }
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex, "Failed to parse confirmations");
        }

        return list;
    }
}
