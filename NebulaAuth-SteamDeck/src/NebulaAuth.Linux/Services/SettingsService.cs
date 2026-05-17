using Newtonsoft.Json;
using NebulaAuth.Linux.Models;
using NLog;

namespace NebulaAuth.Linux.Services;

/// <summary>
/// Manages application settings persistence
/// </summary>
public class SettingsService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string SettingsFileName = "settings.json";
    
    public AppSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        Load();
    }

    private string GetSettingsPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, SettingsFileName);
    }

    public void Load()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            Settings = new AppSettings();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load settings");
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var path = GetSettingsPath();
            var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save settings");
        }
    }
}
