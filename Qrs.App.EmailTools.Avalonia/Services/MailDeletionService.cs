using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qrs.App.EmailTools.Avalonia.Models;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>Progress update raised while a deletion run is in flight.</summary>
public sealed class DeletionProgress
{
    public int Processed { get; init; }
    public int Total { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>Outcome of a deletion run.</summary>
public sealed class DeletionResult
{
    public int ServerDeleted { get; init; }
    public int ClientDeleted { get; init; }
    public int Errors { get; init; }
}

/// <summary>
/// Executes the queued deletions from the <c>messagelist</c>: it first physically
/// deletes messages from the server (to free up server-side storage) and then removes
/// the requested client copies from the local database. A message-list entry is removed
/// once its operation succeeds and kept (with the error recorded) when it fails.
/// </summary>
public sealed class MailDeletionService
{
    private readonly MailTransportService _mailTransport;

    public MailDeletionService(MailTransportService mailTransport)
    {
        _mailTransport = mailTransport;
    }

    public async Task<DeletionResult> DeleteAsync(
        IReadOnlyList<EmailAccount> accounts,
        MailDatabase database,
        IProgress<DeletionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var targets = await database.GetDeletionTargetsAsync(cancellationToken);
        var accountsById = accounts
            .GroupBy(a => a.AccountId)
            .ToDictionary(g => g.Key, g => g.First());

        var total = targets.Count;
        var processed = 0;
        var serverDeleted = 0;
        var clientDeleted = 0;
        var errors = 0;

        // Tracks whether the server-side delete succeeded for each message so the client
        // copy is only removed once the server copy is gone (or was never required).
        var serverOk = new Dictionary<long, bool>();

        // --- Server-side physical deletes first, grouped per folder generation. ---
        var serverGroups = targets
            .Where(t => t.DeleteFromServer)
            .GroupBy(t => (t.AccountId, t.FolderFullName, t.UidValidity));

        foreach (var group in serverGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (accountId, folder, uidValidity) = group.Key;
            var byUid = group.ToDictionary(t => t.Uid);

            if (!accountsById.TryGetValue(accountId, out var account))
            {
                foreach (var target in group)
                {
                    await database.SetMessageListErrorAsync(target.MessageRowId, MessageListType.DeleteFromServer,
                        "No matching account is configured for this message.", cancellationToken);
                    serverOk[target.MessageRowId] = false;
                    processed++;
                    errors++;
                    progress?.Report(new DeletionProgress { Processed = processed, Total = total, Message = $"Skipped {target.Subject} (no account)." });
                }
                continue;
            }

            progress?.Report(new DeletionProgress { Processed = processed, Total = total, Message = $"Deleting {group.Count()} message(s) from {account.EmailAddress}/{folder} on the server..." });

            IReadOnlyDictionary<uint, string?> results;
            try
            {
                results = await _mailTransport.DeleteMessagesFromServerAsync(account, folder, uidValidity, byUid.Keys.ToList(), cancellationToken);
            }
            catch (Exception ex)
            {
                // A connection-level failure fails the whole folder group.
                foreach (var target in group)
                {
                    await database.SetMessageListErrorAsync(target.MessageRowId, MessageListType.DeleteFromServer, ex.Message, cancellationToken);
                    serverOk[target.MessageRowId] = false;
                    processed++;
                    errors++;
                }
                progress?.Report(new DeletionProgress { Processed = processed, Total = total, Message = $"Failed to delete from {account.EmailAddress}/{folder}: {ex.Message}" });
                continue;
            }

            foreach (var (uid, error) in results)
            {
                if (!byUid.TryGetValue(uid, out var target))
                    continue;

                processed++;
                if (error is null)
                {
                    await database.RemoveFromMessageListAsync(target.MessageRowId, MessageListType.DeleteFromServer, cancellationToken);
                    serverOk[target.MessageRowId] = true;
                    serverDeleted++;
                }
                else
                {
                    await database.SetMessageListErrorAsync(target.MessageRowId, MessageListType.DeleteFromServer, error, cancellationToken);
                    serverOk[target.MessageRowId] = false;
                    errors++;
                }

                progress?.Report(new DeletionProgress { Processed = processed, Total = total, Message = $"Server delete: {target.Subject}" });
            }
        }

        // --- Client-side deletes, only when the server copy is gone (or was never queued). ---
        foreach (var target in targets.Where(t => t.DeleteFromClient))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var canDeleteClient = !target.DeleteFromServer || (serverOk.TryGetValue(target.MessageRowId, out var ok) && ok);
            if (!canDeleteClient)
                continue;

            // Deleting the message row cascades to its attachments and remaining list entries.
            await database.DeleteMessageRowAsync(target.MessageRowId, cancellationToken);
            clientDeleted++;

            // Client-only messages were not counted during the server phase; count them now so
            // the progress total is reached exactly once per queued message.
            if (!target.DeleteFromServer)
                processed++;

            progress?.Report(new DeletionProgress { Processed = processed, Total = total, Message = $"Client delete: {target.Subject}" });
        }

        progress?.Report(new DeletionProgress { Processed = total, Total = total, Message = "Deletion complete." });
        return new DeletionResult { ServerDeleted = serverDeleted, ClientDeleted = clientDeleted, Errors = errors };
    }
}
