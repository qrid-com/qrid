using System;
namespace Qrs.App.EmailTools.Avalonia.Models;

public sealed class EmailMessageDetails
{
    public string Subject { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public DateTimeOffset? Date { get; init; }
    public string Body { get; init; } = string.Empty;
}
