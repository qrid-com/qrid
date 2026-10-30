using System;
using System.Collections.Generic;

namespace Qrs.App.EmailTools.Avalonia.Models;

/// <summary>A fully-fetched message ready to be persisted into the local database.</summary>
public sealed class EmailMessageRecord
{
    public uint Uid { get; init; }
    public uint UidValidity { get; init; }
    public string? MessageId { get; init; }
    public ulong? GmailMessageId { get; init; }
    public ulong? GmailThreadId { get; init; }
    public string Flags { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string Cc { get; init; } = string.Empty;
    public DateTimeOffset? Date { get; init; }
    public long Size { get; init; }
    public long TotalSize { get; init; }
    public long AttachmentsSize { get; init; }
    public int AttachmentsCount { get; init; }

    /// <summary>Denormalized, comma-separated list of attachment file names, ordered by name.</summary>
    public string AttachedFileNames { get; init; } = string.Empty;

    public string BodyText { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
    public byte[] RawMessage { get; init; } = Array.Empty<byte>();
    public IReadOnlyList<EmailAttachmentRecord> Attachments { get; init; } = Array.Empty<EmailAttachmentRecord>();
}

/// <summary>An attachment extracted from a message, stored as a BLOB in the local database.</summary>
public sealed class EmailAttachmentRecord
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string? ContentId { get; init; }
    public string? Disposition { get; init; }
    public long Size { get; init; }
    public byte[] Content { get; init; } = Array.Empty<byte>();
}

/// <summary>Result of fetching a folder: its current UIDVALIDITY plus the new messages.</summary>
public sealed class FolderFetchResult
{
    public uint UidValidity { get; init; }
    public uint HighestUid { get; init; }
    public bool UidValidityChanged { get; init; }
    public IReadOnlyList<EmailMessageRecord> Messages { get; init; } = Array.Empty<EmailMessageRecord>();
}

/// <summary>Per-folder synchronization bookmark stored in the local database.</summary>
public sealed class FolderSyncState
{
    public uint UidValidity { get; init; }
    public uint LastSeenUid { get; init; }
}
