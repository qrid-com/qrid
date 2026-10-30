using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Qrs.App.EmailTools.Avalonia.Models;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// Synchronizes every message and attachment from every account and folder into the
/// local encrypted <see cref="MailDatabase"/>. Sync is incremental per folder (only
/// UIDs newer than the last stored one are fetched) and resets a folder when the
/// server reports a new UIDVALIDITY.
/// </summary>
public sealed class MailSyncService
{
    private readonly MailTransportService _mailTransport;

    public MailSyncService(MailTransportService mailTransport)
    {
        _mailTransport = mailTransport;
    }

    public async Task<int> SyncAllAsync(IEnumerable<EmailAccount> accounts, MailDatabase database, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var totalSaved = 0;

        foreach (var account in accounts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(account.EmailAddress))
                continue;

            progress?.Report($"Syncing {account.EmailAddress}...");
            await database.UpsertAccountAsync(account.AccountId, account.EmailAddress, account.DisplayName, account.Provider.ToString(), cancellationToken);

            var folders = await _mailTransport.GetFoldersAsync(account, cancellationToken);
            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalSaved += await SyncFolderAsync(account, folder, database, progress, cancellationToken);
            }
        }

        progress?.Report($"Sync complete. {totalSaved} new message(s) stored.");
        return totalSaved;
    }

    private async Task<int> SyncFolderAsync(EmailAccount account, EmailFolder folder, MailDatabase database, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var state = await database.GetFolderSyncStateAsync(account.AccountId, folder.FullName, cancellationToken);
        var sinceUid = state?.LastSeenUid ?? 0;

        var result = await _mailTransport.FetchFolderForSyncAsync(account, folder.FullName, sinceUid, state?.UidValidity, cancellationToken);

        // A changed UIDVALIDITY invalidates every previously stored UID for this folder, so the
        // transport re-fetches the whole folder and we drop the stale local copy before saving.
        if (result.UidValidityChanged)
        {
            await database.ClearFolderMessagesAsync(account.AccountId, folder.FullName, cancellationToken);
            sinceUid = 0;
        }

        foreach (var message in result.Messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await database.SaveMessageAsync(account.AccountId, folder.FullName, message, cancellationToken);
        }

        var lastSeenUid = Math.Max(sinceUid, result.HighestUid);
        await database.UpsertFolderAsync(account.AccountId, folder.FullName, folder.DisplayName, result.UidValidity, lastSeenUid, cancellationToken);

        if (result.Messages.Count > 0)
            progress?.Report($"{account.EmailAddress}/{folder.DisplayName}: stored {result.Messages.Count} message(s).");

        return result.Messages.Count;
    }
}
