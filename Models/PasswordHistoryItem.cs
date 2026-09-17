// In-memory only history item. Never serialized, never written to disk.
namespace PasswordGenerator.Models;

public sealed record PasswordHistoryItem(string Value, DateTime CreatedAt)
{
    public string ShortTime => CreatedAt.ToString("HH:mm:ss");
}
