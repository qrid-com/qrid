using System;
namespace Qrs.App.EmailTools.Avalonia.Models;

public sealed class EmailMessageSummary
{
    public uint UniqueId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public DateTimeOffset? Date { get; init; }
    public bool HasAttachments { get; init; }
}
