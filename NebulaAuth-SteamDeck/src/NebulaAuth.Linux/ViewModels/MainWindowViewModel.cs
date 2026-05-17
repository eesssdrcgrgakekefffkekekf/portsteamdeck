using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NebulaAuth.Linux.Models;
using NebulaAuth.Linux.Services;
using NLog;

namespace NebulaAuth.Linux.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly MafileService _mafileService;
    private readonly SteamGuardService _steamGuardService;
    private readonly SettingsService _settingsService;
    private readonly ProxyService _proxyService;
    private readonly ConfirmationService _confirmationService;

    private Timer? _codeRefreshTimer;

    [ObservableProperty] private string _currentCode = "-----";
    [ObservableProperty] private int _secondsRemaining = 30;
    [ObservableProperty] private double _codeProgress = 1.0;
    [ObservableProperty] private Mafile? _selectedAccount;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _isAutoConfirmTrades;
    [ObservableProperty] private bool _isAutoConfirmMarket;
    [ObservableProperty] private string? _selectedGroup;
    [ObservableProperty] private bool _isSettingsVisible;
    [ObservableProperty] private bool _isConfirmationsVisible;

    public ObservableCollection<Mafile> Accounts { get; } = new();
    public ObservableCollection<Mafile> FilteredAccounts { get; } = new();
    public ObservableCollection<string> Groups { get; } = new();
    public ObservableCollection<Confirmation> Confirmations { get; } = new();

    public MainWindowViewModel(
        MafileService mafileService,
        SteamGuardService steamGuardService,
        SettingsService settingsService,
        ProxyService proxyService,
        ConfirmationService confirmationService)
    {
        _mafileService = mafileService;
        _steamGuardService = steamGuardService;
        _settingsService = settingsService;
        _proxyService = proxyService;
        _confirmationService = confirmationService;

        _confirmationService.ConfirmationsUpdated += OnConfirmationsUpdated;
        _mafileService.MafilesChanged += OnMafilesChanged;

        Initialize();
    }

    private async void Initialize()
    {
        StatusText = "Aligning time with Steam servers...";
        await _steamGuardService.AlignTimeAsync();
        StatusText = "Loading accounts...";

        _mafileService.SetDirectory(_settingsService.Settings.MafilesDirectory);
        _mafileService.LoadMafiles();
        RefreshAccountsList();

        // Start code refresh timer (1 second intervals)
        _codeRefreshTimer = new Timer(_ => RefreshCode(), null, 0, 1000);

        // Apply auto-confirm settings
        IsAutoConfirmTrades = _settingsService.Settings.AutoConfirmTrades;
        IsAutoConfirmMarket = _settingsService.Settings.AutoConfirmMarket;

        StatusText = $"Loaded {Accounts.Count} accounts";
    }

    private void RefreshCode()
    {
        var seconds = _steamGuardService.GetSecondsUntilExpiry();
        SecondsRemaining = seconds;
        CodeProgress = seconds / 30.0;

        if (SelectedAccount != null)
        {
            CurrentCode = _steamGuardService.GenerateCode(SelectedAccount);
        }
        else
        {
            CurrentCode = "-----";
        }
    }

    private void OnMafilesChanged()
    {
        RefreshAccountsList();
    }

    private void RefreshAccountsList()
    {
        Accounts.Clear();
        foreach (var m in _mafileService.Mafiles)
            Accounts.Add(m);

        Groups.Clear();
        Groups.Add("All");
        foreach (var g in _mafileService.GetGroups())
            Groups.Add(g);

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedGroupChanged(string? value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredAccounts.Clear();

        var accounts = string.IsNullOrEmpty(SelectedGroup) || SelectedGroup == "All"
            ? _mafileService.Mafiles
            : _mafileService.GetByGroup(SelectedGroup);

        foreach (var account in accounts)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                var search = SearchText.Trim();
                if (!account.AccountName.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                    !account.SteamId.ToString().Contains(search))
                    continue;
            }

            FilteredAccounts.Add(account);
        }
    }

    partial void OnSelectedAccountChanged(Mafile? value)
    {
        RefreshCode();
        if (value != null)
        {
            StatusText = $"Selected: {value.AccountName} ({value.SteamId})";
        }
    }

    [RelayCommand]
    private async Task CopyCode()
    {
        if (string.IsNullOrEmpty(CurrentCode) || CurrentCode == "-----") return;
        
        // Copy to clipboard using xclip/wl-copy on Linux
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"echo -n '{CurrentCode}' | xclip -selection clipboard 2>/dev/null || echo -n '{CurrentCode}' | wl-copy 2>/dev/null\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            StatusText = $"Code copied: {CurrentCode}";
        }
        catch
        {
            StatusText = $"Code: {CurrentCode} (copy manually)";
        }
    }

    [RelayCommand]
    private async Task RefreshConfirmations()
    {
        if (SelectedAccount == null)
        {
            StatusText = "Select an account first";
            return;
        }

        StatusText = "Fetching confirmations...";
        await _confirmationService.GetConfirmations(SelectedAccount);
        StatusText = $"Found {Confirmations.Count} confirmations";
    }

    [RelayCommand]
    private async Task AcceptConfirmation(Confirmation? confirmation)
    {
        if (SelectedAccount == null || confirmation == null) return;

        var result = await _confirmationService.AcceptConfirmation(SelectedAccount, confirmation);
        if (result)
        {
            Confirmations.Remove(confirmation);
            StatusText = $"Confirmation {confirmation.Id} accepted";
        }
        else
        {
            StatusText = "Failed to accept confirmation";
        }
    }

    [RelayCommand]
    private async Task DenyConfirmation(Confirmation? confirmation)
    {
        if (SelectedAccount == null || confirmation == null) return;

        var result = await _confirmationService.DenyConfirmation(SelectedAccount, confirmation);
        if (result)
        {
            Confirmations.Remove(confirmation);
            StatusText = $"Confirmation {confirmation.Id} denied";
        }
        else
        {
            StatusText = "Failed to deny confirmation";
        }
    }

    [RelayCommand]
    private async Task AcceptAll()
    {
        if (SelectedAccount == null) return;

        var count = 0;
        foreach (var conf in Confirmations.ToList())
        {
            if (await _confirmationService.AcceptConfirmation(SelectedAccount, conf))
            {
                Confirmations.Remove(conf);
                count++;
            }
        }
        StatusText = $"Accepted {count} confirmations";
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsVisible = !IsSettingsVisible;
        IsConfirmationsVisible = false;
    }

    [RelayCommand]
    private void ToggleConfirmations()
    {
        IsConfirmationsVisible = !IsConfirmationsVisible;
        IsSettingsVisible = false;
    }

    partial void OnIsAutoConfirmTradesChanged(bool value)
    {
        _settingsService.Settings.AutoConfirmTrades = value;
        _settingsService.Save();
        UpdateAutoConfirm();
    }

    partial void OnIsAutoConfirmMarketChanged(bool value)
    {
        _settingsService.Settings.AutoConfirmMarket = value;
        _settingsService.Save();
        UpdateAutoConfirm();
    }

    private void UpdateAutoConfirm()
    {
        if (IsAutoConfirmTrades || IsAutoConfirmMarket)
        {
            _confirmationService.StartAutoConfirm(
                IsAutoConfirmTrades,
                IsAutoConfirmMarket,
                _settingsService.Settings.ConfirmIntervalSeconds);
            StatusText = "Auto-confirm enabled";
        }
        else
        {
            _confirmationService.StopAutoConfirm();
            StatusText = "Auto-confirm disabled";
        }
    }

    [RelayCommand]
    private void ImportMafiles()
    {
        // Will be handled by file dialog in view
        StatusText = "Use the file manager to import .maFile files";
    }

    private void OnConfirmationsUpdated(List<Confirmation> confirmations)
    {
        Confirmations.Clear();
        foreach (var c in confirmations)
            Confirmations.Add(c);
    }
}
