using System;

namespace Qrs.App.EmailTools.Avalonia.Models;

/// <summary>
/// One row of the "Review and Delete" summary grid. Aggregated from the
/// <c>messagelist</c> table joined with <c>messages</c>, grouped by
/// account, folder and from-address.
/// </summary>
public sealed class DeletionSummaryRow
{
    public string Account { get; init; } = string.Empty;
    public string Folder { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public int ServerMessageCount { get; init; }
    public int ServerAttachmentsCount { get; init; }
    public long ServerTotalSize { get; init; }
    public int ClientMessageCount { get; init; }
    public long ClientTotalSize { get; init; }
    public DateTimeOffset? MinDate { get; init; }
    public DateTimeOffset? MaxDate { get; init; }
}

/// <summary>
/// One row of the "Review and Delete" messages grid (the top 1000 messages
/// queued for deletion). Any server error recorded on the message list is
/// surfaced through <see cref="ErrorMessage"/>.
/// </summary>
public sealed class DeletionMessageRow
{
    public DateTimeOffset? Date { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public int AttachmentCount { get; init; }
    public string BodyPreview { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// A single local message that has been queued for deletion, carrying enough
/// metadata to delete it from the server and/or the local database and to
/// update the corresponding <c>messagelist</c> entries.
/// </summary>
public sealed class DeletionTarget
{
    public long MessageRowId { get; init; }
    public string AccountId { get; init; } = string.Empty;
    public string FolderFullName { get; init; } = string.Empty;
    public uint UidValidity { get; init; }
    public uint Uid { get; init; }
    public string Subject { get; init; } = string.Empty;

    /// <summary>True when the message is on the "delete from server" list.</summary>
    public bool DeleteFromServer { get; init; }

    /// <summary>True when the message is on the "delete from client" list.</summary>
    public bool DeleteFromClient { get; init; }
}
