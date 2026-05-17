namespace NebulaAuth.Linux.Models;

public class Confirmation
{
    public ulong Id { get; set; }
    public ulong Key { get; set; }
    public ulong Creator { get; set; }
    public ConfirmationType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Icon { get; set; } = string.Empty;
}

public enum ConfirmationType
{
    Unknown = 0,
    Trade = 2,
    MarketListing = 3,
    AccountRecovery = 6
}
