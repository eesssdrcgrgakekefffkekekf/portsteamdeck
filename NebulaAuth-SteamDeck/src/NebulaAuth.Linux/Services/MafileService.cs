using Newtonsoft.Json;
using NebulaAuth.Linux.Models;
using NLog;

namespace NebulaAuth.Linux.Services;

/// <summary>
/// Service for managing mafile storage - loading, saving, importing, exporting.
/// </summary>
public class MafileService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly List<Mafile> _mafiles = new();
    private string _mafilesDirectory = "maFiles";

    public IReadOnlyList<Mafile> Mafiles => _mafiles;
    public event Action? MafilesChanged;

    public void SetDirectory(string directory)
    {
        _mafilesDirectory = directory;
    }

    public string GetMafilesDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = Path.IsPathRooted(_mafilesDirectory)
            ? _mafilesDirectory
            : Path.Combine(baseDir, _mafilesDirectory);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return dir;
    }

    public void LoadMafiles()
    {
        _mafiles.Clear();
        var dir = GetMafilesDirectory();

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            MafilesChanged?.Invoke();
            return;
        }

        var files = Directory.GetFiles(dir, "*.maFile")
            .Concat(Directory.GetFiles(dir, "*.mafile"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var mafile = JsonConvert.DeserializeObject<Mafile>(json);
                if (mafile != null)
                {
                    mafile.FilePath = file;
                    if (string.IsNullOrEmpty(mafile.AccountName))
                    {
                        mafile.AccountName = Path.GetFileNameWithoutExtension(file);
                    }
                    _mafiles.Add(mafile);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load mafile: {File}", file);
            }
        }

        Logger.Info("Loaded {Count} mafiles", _mafiles.Count);
        MafilesChanged?.Invoke();
    }

    public void SaveMafile(Mafile mafile)
    {
        var dir = GetMafilesDirectory();

        if (string.IsNullOrEmpty(mafile.FilePath))
        {
            var fileName = $"{mafile.SteamId}.maFile";
            mafile.FilePath = Path.Combine(dir, fileName);
        }

        var json = JsonConvert.SerializeObject(mafile, Formatting.Indented);
        File.WriteAllText(mafile.FilePath, json);
        Logger.Info("Saved mafile: {File}", mafile.FilePath);
    }

    public Mafile? ImportMafile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var mafile = JsonConvert.DeserializeObject<Mafile>(json);
            if (mafile == null) return null;

            var dir = GetMafilesDirectory();
            var targetFileName = !string.IsNullOrEmpty(mafile.AccountName)
                ? $"{mafile.AccountName}.maFile"
                : $"{mafile.SteamId}.maFile";
            var targetPath = Path.Combine(dir, targetFileName);

            mafile.FilePath = targetPath;
            File.Copy(filePath, targetPath, true);
            
            // Remove existing with same steam id
            _mafiles.RemoveAll(m => m.SteamId == mafile.SteamId);
            _mafiles.Add(mafile);
            MafilesChanged?.Invoke();

            Logger.Info("Imported mafile: {Account}", mafile.AccountName);
            return mafile;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to import mafile: {File}", filePath);
            return null;
        }
    }

    public bool RemoveMafile(Mafile mafile)
    {
        try
        {
            if (File.Exists(mafile.FilePath))
                File.Delete(mafile.FilePath);

            _mafiles.Remove(mafile);
            MafilesChanged?.Invoke();
            Logger.Info("Removed mafile: {Account}", mafile.AccountName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to remove mafile: {Account}", mafile.AccountName);
            return false;
        }
    }

    public IEnumerable<string> GetGroups()
    {
        return _mafiles
            .Where(m => !string.IsNullOrEmpty(m.Group))
            .Select(m => m.Group!)
            .Distinct()
            .OrderBy(g => g);
    }

    public IEnumerable<Mafile> GetByGroup(string? group)
    {
        if (string.IsNullOrEmpty(group))
            return _mafiles;
        return _mafiles.Where(m => m.Group == group);
    }

    public Mafile? FindByLogin(string login)
    {
        return _mafiles.FirstOrDefault(m =>
            m.AccountName.Equals(login, StringComparison.OrdinalIgnoreCase));
    }

    public Mafile? FindBySteamId(ulong steamId)
    {
        return _mafiles.FirstOrDefault(m => m.SteamId == steamId);
    }
}
