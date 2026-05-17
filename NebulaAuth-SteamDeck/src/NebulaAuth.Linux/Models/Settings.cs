using Newtonsoft.Json;

namespace NebulaAuth.Linux.Models;

public class AppSettings
{
    [JsonProperty("language")]
    public string Language { get; set; } = "en";

    [JsonProperty("auto_confirm_trades")]
    public bool AutoConfirmTrades { get; set; }

    [JsonProperty("auto_confirm_market")]
    public bool AutoConfirmMarket { get; set; }

    [JsonProperty("confirm_interval_seconds")]
    public int ConfirmIntervalSeconds { get; set; } = 30;

    [JsonProperty("minimize_to_tray")]
    public bool MinimizeToTray { get; set; }

    [JsonProperty("legacy_mafile_mode")]
    public bool LegacyMafileMode { get; set; } = true;

    [JsonProperty("auto_update")]
    public bool AutoUpdate { get; set; } = true;

    [JsonProperty("encryption_password")]
    public string? EncryptionPassword { get; set; }

    [JsonProperty("mafiles_directory")]
    public string MafilesDirectory { get; set; } = "maFiles";

    [JsonProperty("selected_group")]
    public string? SelectedGroup { get; set; }

    [JsonProperty("window_width")]
    public double WindowWidth { get; set; } = 800;

    [JsonProperty("window_height")]
    public double WindowHeight { get; set; } = 600;

    [JsonProperty("gamepad_mode")]
    public bool GamepadMode { get; set; } = true;
}
