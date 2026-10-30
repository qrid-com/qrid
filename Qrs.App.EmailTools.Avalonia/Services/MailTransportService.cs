using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Qrs.App.EmailTools.Avalonia.Models;

namespace Qrs.App.EmailTools.Avalonia.Services;

public sealed class MailTransportService
{
    private const int MessagePageSize = 50;

    private readonly OAuthTokenService _oauthTokenService;

    public MailTransportService(OAuthTokenService oauthTokenService)
    {
        _oauthTokenService = oauthTokenService;
    }

    public async Task<IReadOnlyList<EmailFolder>> GetFoldersAsync(EmailAccount account, CancellationToken cancellationToken = default)
    {
        using var client = await ConnectImapAsync(account, cancellationToken);
        var folders = await client.GetFoldersAsync(client.PersonalNamespaces[0], false, cancellationToken);
        return folders
            .Select(folder => new EmailFolder(folder.FullName, folder.Name))
            .Prepend(new EmailFolder(client.Inbox.FullName, client.Inbox.Name))
            .GroupBy(folder => folder.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(folder => folder.DisplayName)
            .ToList();
    }

    public async Task<IReadOnlyList<EmailMessageSummary>> GetMessagesAsync(EmailAccount account, string folderName, CancellationToken cancellationToken = default)
    {
        using var client = await ConnectImapAsync(account, cancellationToken);
        var folder = await GetFolderAsync(client, folderName, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        if (folder.Count == 0)
            return [];

        var start = Math.Max(0, folder.Count - MessagePageSize);
        var summaries = await folder.FetchAsync(start, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.BodyStructure, cancellationToken);
        return summaries
            .Reverse()
            .Select(summary => new EmailMessageSummary
            {
                UniqueId = summary.UniqueId.Id,
                Subject = summary.Envelope?.Subject ?? "(no subject)",
                From = summary.Envelope?.From?.ToString() ?? string.Empty,
                Date = summary.Envelope?.Date,
                HasAttachments = summary.Attachments?.Any() == true
            })
            .ToList();
    }

    public async Task<EmailMessageDetails> GetMessageAsync(EmailAccount account, string folderName, uint uniqueId, CancellationToken cancellationToken = default)
    {
        using var client = await ConnectImapAsync(account, cancellationToken);
        var folder = await GetFolderAsync(client, folderName, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var message = await folder.GetMessageAsync(new UniqueId(uniqueId), cancellationToken);

        return new EmailMessageDetails
        {
            Subject = message.Subject ?? "(no subject)",
            From = message.From.ToString(),
            To = message.To.ToString(),
            Date = message.Date,
            Body = message.TextBody ?? message.HtmlBody ?? string.Empty
        };
    }

    public async Task SendAsync(EmailAccount account, string recipients, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(account.DisplayName, account.EmailAddress));
        message.To.AddRange(InternetAddressList.Parse(recipients));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(account.OutgoingHost, account.OutgoingPort, GetSmtpSecureSocketOptions(account), cancellationToken);
        await AuthenticateAsync(client, account, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    /// <summary>
    /// Physically deletes the given messages from a folder on the server (adds the
    /// <c>\Deleted</c> flag and expunges them) in order to free up server-side storage.
    /// Each message is deleted individually so that a failure on one message does not
    /// prevent the others from being deleted.
    /// </summary>
    /// <returns>
    /// A map of UID to error message. A <c>null</c> value means the message was deleted
    /// successfully; a non-null value contains the error that prevented its deletion.
    /// </returns>
    public async Task<IReadOnlyDictionary<uint, string?>> DeleteMessagesFromServerAsync(
        EmailAccount account,
        string folderName,
        uint expectedUidValidity,
        IReadOnlyList<uint> uids,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<uint, string?>();
        if (uids.Count == 0)
            return results;

        using var client = await ConnectImapAsync(account, cancellationToken);
        var folder = await GetFolderAsync(client, folderName, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        // If the folder was recreated the stored UIDs belong to a previous generation and no
        // longer identify the intended messages, so refuse to delete anything by those UIDs.
        if ((uint)folder.UidValidity != expectedUidValidity)
        {
            var error = "The server folder changed (UIDVALIDITY mismatch); re-sync before deleting.";
            foreach (var uid in uids)
                results[uid] = error;
            return results;
        }

        foreach (var uid in uids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var uniqueIds = new[] { new UniqueId(uid) };
                await folder.AddFlagsAsync(uniqueIds, MessageFlags.Deleted, true, cancellationToken);
                await folder.ExpungeAsync(uniqueIds, cancellationToken);
                results[uid] = null;
            }
            catch (Exception ex)
            {
                results[uid] = ex.Message;
            }
        }

        await folder.CloseAsync(false, cancellationToken);
        return results;
    }

    /// <summary>
    /// Fetches a folder's current UIDVALIDITY together with all messages whose UID is greater than
    /// <paramref name="sinceUidExclusive"/>. Each returned record carries the full raw MIME message,
    /// its attachments and the metadata (UID, UIDVALIDITY, Message-Id and, for Gmail, the
    /// X-GM-MSGID/THRID identifiers) required to later reload or delete it from the server.
    ///
    /// When <paramref name="expectedUidValidity"/> is supplied and no longer matches the server, the
    /// stored UIDs belong to a previous generation, so the whole folder is re-fetched and
    /// <see cref="FolderFetchResult.UidValidityChanged"/> is set so the caller can drop stale rows.
    /// </summary>
    public async Task<FolderFetchResult> FetchFolderForSyncAsync(EmailAccount account, string folderName, uint sinceUidExclusive, uint? expectedUidValidity = null, CancellationToken cancellationToken = default)
    {
        using var client = await ConnectImapAsync(account, cancellationToken);
        var folder = await GetFolderAsync(client, folderName, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var uidValidity = (uint)folder.UidValidity;
        var uidValidityChanged = expectedUidValidity.HasValue && expectedUidValidity.Value != uidValidity;
        var effectiveSince = uidValidityChanged ? 0u : sinceUidExclusive;

        if (folder.Count == 0)
            return new FolderFetchResult { UidValidity = uidValidity, HighestUid = effectiveSince, UidValidityChanged = uidValidityChanged, Messages = [] };

        var items = MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.Size;
        if (account.Provider == EmailProvider.Gmail)
            items |= MessageSummaryItems.GMailMessageId | MessageSummaryItems.GMailThreadId;

        // Ask the server only for UIDs greater than the last one we stored (UID min is 1).
        var startUid = new UniqueId(effectiveSince == uint.MaxValue ? uint.MaxValue : effectiveSince + 1);
        var range = new UniqueIdRange(startUid, UniqueId.MaxValue);
        var summaries = await folder.FetchAsync(range, items, cancellationToken);
        var newSummaries = summaries.OrderBy(summary => summary.UniqueId.Id).ToList();

        var messages = new List<EmailMessageRecord>(newSummaries.Count);
        var highestUid = effectiveSince;

        foreach (var summary in newSummaries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await folder.GetMessageAsync(summary.UniqueId, cancellationToken);
            messages.Add(await BuildRecordAsync(summary, uidValidity, message, cancellationToken));
            highestUid = Math.Max(highestUid, summary.UniqueId.Id);
        }

        return new FolderFetchResult { UidValidity = uidValidity, HighestUid = highestUid, UidValidityChanged = uidValidityChanged, Messages = messages };
    }

    private static async Task<EmailMessageRecord> BuildRecordAsync(IMessageSummary summary, uint uidValidity, MimeMessage message, CancellationToken cancellationToken)
    {
        using var rawStream = new MemoryStream();
        await message.WriteToAsync(rawStream, cancellationToken);
        var raw = rawStream.ToArray();

        var attachments = new List<EmailAttachmentRecord>();
        foreach (var entity in message.Attachments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attachments.Add(BuildAttachment(entity));
        }

        var attachmentsSize = attachments.Sum(a => a.Size);
        var attachedFileNames = string.Join(", ", attachments
            .Select(a => a.FileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

        return new EmailMessageRecord
        {
            Uid = summary.UniqueId.Id,
            UidValidity = uidValidity,
            MessageId = message.MessageId,
            GmailMessageId = summary.GMailMessageId,
            GmailThreadId = summary.GMailThreadId,
            Flags = summary.Flags?.ToString() ?? string.Empty,
            Subject = message.Subject ?? "(no subject)",
            From = message.From?.ToString() ?? string.Empty,
            To = message.To?.ToString() ?? string.Empty,
            Cc = message.Cc?.ToString() ?? string.Empty,
            Date = message.Date,
            Size = summary.Size.HasValue ? (long)summary.Size.Value : raw.LongLength,
            TotalSize = raw.LongLength,
            AttachmentsSize = attachmentsSize,
            AttachmentsCount = attachments.Count,
            AttachedFileNames = attachedFileNames,
            BodyText = message.TextBody ?? string.Empty,
            BodyHtml = message.HtmlBody ?? string.Empty,
            RawMessage = raw,
            Attachments = attachments
        };
    }

    private static EmailAttachmentRecord BuildAttachment(MimeEntity entity)
    {
        using var stream = new MemoryStream();
        if (entity is MimePart part)
            part.Content?.DecodeTo(stream);
        else
            entity.WriteTo(stream);

        var content = stream.ToArray();
        var fileName = entity.ContentDisposition?.FileName
            ?? (entity as MimePart)?.FileName
            ?? entity.ContentType.Name
            ?? "attachment";

        return new EmailAttachmentRecord
        {
            FileName = fileName,
            ContentType = entity.ContentType.MimeType,
            ContentId = entity.ContentId,
            Disposition = entity.ContentDisposition?.Disposition,
            Size = content.LongLength,
            Content = content
        };
    }

    private async Task<ImapClient> ConnectImapAsync(EmailAccount account, CancellationToken cancellationToken)
    {
        var client = new ImapClient();
        await client.ConnectAsync(account.IncomingHost, account.IncomingPort, account.IncomingUseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
        await AuthenticateAsync(client, account, cancellationToken);
        return client;
    }

    private async Task AuthenticateAsync(IMailService client, EmailAccount account, CancellationToken cancellationToken)
    {
        // Gmail and Outlook no longer accept username/password and require OAuth2 bearer tokens
        // with the proper scopes; custom IMAP/SMTP servers keep using username/password.
        if (account.UsesOAuth)
        {
            var accessToken = await _oauthTokenService.GetAccessTokenAsync(account, cancellationToken);
            var oauth2 = new SaslMechanismOAuth2(account.Username, accessToken);
            await client.AuthenticateAsync(oauth2, cancellationToken);
        }
        else
        {
            await client.AuthenticateAsync(account.Username, account.Password, cancellationToken);
        }
    }

    private static SecureSocketOptions GetSmtpSecureSocketOptions(EmailAccount account)
    {
        if (account.OutgoingPort == 465)
            return SecureSocketOptions.SslOnConnect;

        return account.OutgoingUseStartTls ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.Auto;
    }

    private static async Task<IMailFolder> GetFolderAsync(ImapClient client, string folderName, CancellationToken cancellationToken)
    {
        if (string.Equals(folderName, client.Inbox.FullName, StringComparison.OrdinalIgnoreCase))
            return client.Inbox;

        return await client.GetFolderAsync(folderName, cancellationToken);
    }
}
