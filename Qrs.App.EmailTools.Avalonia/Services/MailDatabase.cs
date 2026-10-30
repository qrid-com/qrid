using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Qrs.App.EmailTools.Avalonia.Models;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// Local, whole-file encrypted (SQLCipher) SQLite database that mirrors every
/// message and attachment from every account and folder. It stores enough
/// metadata (account id, folder, UIDVALIDITY, UID, RFC Message-Id and, for
/// Gmail, the X-GM-MSGID/THRID identifiers) to later reload or delete a specific
/// message directly on the mail server.
/// </summary>
public sealed class MailDatabase : IAsyncDisposable
{
    static MailDatabase()
    {
        // Register the SQLCipher-enabled native SQLite provider.
        SQLitePCL.Batteries_V2.Init();
    }

    private readonly string _dbPath;
    private readonly string _keyHex;
    private SqliteConnection? _connection;

    public MailDatabase(string dbPath, byte[] key)
    {
        _dbPath = dbPath;
        _keyHex = Convert.ToHexString(key);
    }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_connection != null)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync(cancellationToken);

        // Supply the raw 256-bit key (as a SQLCipher blob literal) before any other statement.
        await ExecuteAsync($"PRAGMA key = \"x'{_keyHex}'\";", cancellationToken);
        await ExecuteAsync("PRAGMA foreign_keys = ON;", cancellationToken);

        await EnsureSchemaAsync(cancellationToken);
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        const string schema = """
            CREATE TABLE IF NOT EXISTS accounts (
                AccountId    TEXT PRIMARY KEY,
                EmailAddress TEXT,
                DisplayName  TEXT,
                Provider     TEXT,
                UpdatedUtc   TEXT
            );

            CREATE TABLE IF NOT EXISTS folders (
                AccountId      TEXT NOT NULL,
                FolderFullName TEXT NOT NULL,
                DisplayName    TEXT,
                UidValidity    INTEGER NOT NULL DEFAULT 0,
                LastSeenUid    INTEGER NOT NULL DEFAULT 0,
                LastSyncUtc    TEXT,
                PRIMARY KEY (AccountId, FolderFullName)
            );

            CREATE TABLE IF NOT EXISTS messages (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId      TEXT NOT NULL,
                FolderFullName TEXT NOT NULL,
                UidValidity    INTEGER NOT NULL,
                Uid            INTEGER NOT NULL,
                MessageId      TEXT,
                GmailMessageId INTEGER,
                GmailThreadId  INTEGER,
                Flags          TEXT,
                Subject        TEXT,
                FromAddr       TEXT,
                ToAddr         TEXT,
                CcAddr         TEXT,
                DateUtc        TEXT,
                Size           INTEGER,
                TotalSize      INTEGER NOT NULL DEFAULT 0,
                AttachmentsSize INTEGER NOT NULL DEFAULT 0,
                AttachmentsCount INTEGER NOT NULL DEFAULT 0,
                AttachedFileNames TEXT NOT NULL DEFAULT '',
                HasAttachments INTEGER NOT NULL DEFAULT 0,
                BodyText       TEXT,
                BodyHtml       TEXT,
                RawMessage     BLOB,
                FirstSyncedUtc TEXT,
                LastSeenUtc    TEXT,
                ServerDeletedUtc TEXT,
                UNIQUE (AccountId, FolderFullName, UidValidity, Uid)
            );

            CREATE INDEX IF NOT EXISTS ix_messages_lookup ON messages (AccountId, FolderFullName, UidValidity, Uid);
            CREATE INDEX IF NOT EXISTS ix_messages_msgid ON messages (MessageId);

            CREATE TABLE IF NOT EXISTS attachments (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                MessageRowId INTEGER NOT NULL,
                FileName     TEXT,
                ContentType  TEXT,
                ContentId    TEXT,
                Disposition  TEXT,
                Size         INTEGER,
                Content      BLOB,
                FOREIGN KEY (MessageRowId) REFERENCES messages(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_attachments_msg ON attachments (MessageRowId);

            CREATE TABLE IF NOT EXISTS messagelist (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                ListType     TEXT NOT NULL,
                MessageRowId INTEGER NOT NULL,
                ErrorMessage TEXT,
                CreatedUtc   TEXT,
                UNIQUE (ListType, MessageRowId),
                FOREIGN KEY (MessageRowId) REFERENCES messages(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_messagelist_msg ON messagelist (MessageRowId);
            """;

        await ExecuteAsync(schema, cancellationToken);

        // Migrate existing databases that pre-date these columns.
        await AddColumnIfMissingAsync("messages", "TotalSize INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync("messages", "AttachmentsSize INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync("messages", "AttachmentsCount INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await AddColumnIfMissingAsync("messages", "AttachedFileNames TEXT NOT NULL DEFAULT ''", cancellationToken);
    }

    /// <summary>
    /// Adds a column to an existing table if it does not already exist.
    /// Both <paramref name="table"/> and <paramref name="columnDef"/> must be
    /// compile-time constant strings from within this class; they are never
    /// derived from external or user-controlled input.
    /// </summary>
    private async Task AddColumnIfMissingAsync(string table, string columnDef, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync($"ALTER TABLE {table} ADD COLUMN {columnDef};", cancellationToken);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("duplicate column name"))
        {
            // Column already exists — nothing to do.
        }
    }

    public async Task UpsertAccountAsync(string accountId, string emailAddress, string displayName, string provider, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO accounts (AccountId, EmailAddress, DisplayName, Provider, UpdatedUtc)
            VALUES ($id, $email, $name, $provider, $updated)
            ON CONFLICT(AccountId) DO UPDATE SET
                EmailAddress = excluded.EmailAddress,
                DisplayName  = excluded.DisplayName,
                Provider     = excluded.Provider,
                UpdatedUtc   = excluded.UpdatedUtc;
            """;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", accountId);
        command.Parameters.AddWithValue("$email", emailAddress);
        command.Parameters.AddWithValue("$name", displayName);
        command.Parameters.AddWithValue("$provider", provider);
        command.Parameters.AddWithValue("$updated", UtcNow());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<FolderSyncState?> GetFolderSyncStateAsync(string accountId, string folderFullName, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT UidValidity, LastSeenUid FROM folders WHERE AccountId = $id AND FolderFullName = $folder;";
        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", accountId);
        command.Parameters.AddWithValue("$folder", folderFullName);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new FolderSyncState
        {
            UidValidity = (uint)reader.GetInt64(0),
            LastSeenUid = (uint)reader.GetInt64(1)
        };
    }

    public async Task UpsertFolderAsync(string accountId, string folderFullName, string displayName, uint uidValidity, uint lastSeenUid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO folders (AccountId, FolderFullName, DisplayName, UidValidity, LastSeenUid, LastSyncUtc)
            VALUES ($id, $folder, $name, $uidv, $lastuid, $sync)
            ON CONFLICT(AccountId, FolderFullName) DO UPDATE SET
                DisplayName = excluded.DisplayName,
                UidValidity = excluded.UidValidity,
                LastSeenUid = excluded.LastSeenUid,
                LastSyncUtc = excluded.LastSyncUtc;
            """;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", accountId);
        command.Parameters.AddWithValue("$folder", folderFullName);
        command.Parameters.AddWithValue("$name", displayName);
        command.Parameters.AddWithValue("$uidv", uidValidity);
        command.Parameters.AddWithValue("$lastuid", lastSeenUid);
        command.Parameters.AddWithValue("$sync", UtcNow());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Removes all locally stored messages for a folder. Used when the server reports a
    /// new UIDVALIDITY, which invalidates every previously stored UID for that folder.
    /// </summary>
    public async Task ClearFolderMessagesAsync(string accountId, string folderFullName, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM messages WHERE AccountId = $id AND FolderFullName = $folder;";
        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", accountId);
        command.Parameters.AddWithValue("$folder", folderFullName);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveMessageAsync(string accountId, string folderFullName, EmailMessageRecord record, CancellationToken cancellationToken = default)
    {
        using var transaction = (SqliteTransaction)await Connection.BeginTransactionAsync(cancellationToken);

        var messageRowId = await UpsertMessageAsync(accountId, folderFullName, record, transaction, cancellationToken);
        await ReplaceAttachmentsAsync(messageRowId, record, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<long> UpsertMessageAsync(string accountId, string folderFullName, EmailMessageRecord record, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO messages
                (AccountId, FolderFullName, UidValidity, Uid, MessageId, GmailMessageId, GmailThreadId,
                 Flags, Subject, FromAddr, ToAddr, CcAddr, DateUtc, Size, TotalSize, AttachmentsSize, AttachmentsCount, AttachedFileNames, HasAttachments,
                 BodyText, BodyHtml, RawMessage, FirstSyncedUtc, LastSeenUtc, ServerDeletedUtc)
            VALUES
                ($account, $folder, $uidv, $uid, $msgid, $gmid, $gthid,
                 $flags, $subject, $from, $to, $cc, $date, $size, $totalsize, $attsize, $attcount, $attnames, $hasatt,
                 $bodytext, $bodyhtml, $raw, $now, $now, NULL)
            ON CONFLICT(AccountId, FolderFullName, UidValidity, Uid) DO UPDATE SET
                Flags = excluded.Flags,
                LastSeenUtc = excluded.LastSeenUtc,
                ServerDeletedUtc = NULL
            RETURNING Id;
            """;

        using var command = Connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        var now = UtcNow();
        command.Parameters.AddWithValue("$account", accountId);
        command.Parameters.AddWithValue("$folder", folderFullName);
        command.Parameters.AddWithValue("$uidv", record.UidValidity);
        command.Parameters.AddWithValue("$uid", record.Uid);
        command.Parameters.AddWithValue("$msgid", (object?)record.MessageId ?? DBNull.Value);
        command.Parameters.AddWithValue("$gmid", (object?)(long?)record.GmailMessageId ?? DBNull.Value);
        command.Parameters.AddWithValue("$gthid", (object?)(long?)record.GmailThreadId ?? DBNull.Value);
        command.Parameters.AddWithValue("$flags", record.Flags);
        command.Parameters.AddWithValue("$subject", record.Subject);
        command.Parameters.AddWithValue("$from", record.From);
        command.Parameters.AddWithValue("$to", record.To);
        command.Parameters.AddWithValue("$cc", record.Cc);
        command.Parameters.AddWithValue("$date", (object?)record.Date?.UtcDateTime.ToString("o") ?? DBNull.Value);
        command.Parameters.AddWithValue("$size", record.Size);
        command.Parameters.AddWithValue("$totalsize", record.TotalSize);
        command.Parameters.AddWithValue("$attsize", record.AttachmentsSize);
        command.Parameters.AddWithValue("$attcount", record.AttachmentsCount);
        command.Parameters.AddWithValue("$attnames", record.AttachedFileNames);
        command.Parameters.AddWithValue("$hasatt", record.Attachments.Count > 0 ? 1 : 0);
        command.Parameters.AddWithValue("$bodytext", record.BodyText);
        command.Parameters.AddWithValue("$bodyhtml", record.BodyHtml);
        command.Parameters.AddWithValue("$raw", record.RawMessage);
        command.Parameters.AddWithValue("$now", now);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private async Task ReplaceAttachmentsAsync(long messageRowId, EmailMessageRecord record, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        using (var delete = Connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM attachments WHERE MessageRowId = $mid;";
            delete.Parameters.AddWithValue("$mid", messageRowId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var attachment in record.Attachments)
        {
            using var insert = Connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO attachments (MessageRowId, FileName, ContentType, ContentId, Disposition, Size, Content)
                VALUES ($mid, $name, $type, $cid, $disp, $size, $content);
                """;
            insert.Parameters.AddWithValue("$mid", messageRowId);
            insert.Parameters.AddWithValue("$name", attachment.FileName);
            insert.Parameters.AddWithValue("$type", attachment.ContentType);
            insert.Parameters.AddWithValue("$cid", (object?)attachment.ContentId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$disp", (object?)attachment.Disposition ?? DBNull.Value);
            insert.Parameters.AddWithValue("$size", attachment.Size);
            insert.Parameters.AddWithValue("$content", attachment.Content);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Adds the message identified by <paramref name="messageRowId"/> to the given
    /// deletion list. Idempotent: re-adding an existing entry simply clears any prior
    /// error so the message can be retried.
    /// </summary>
    public async Task AddToMessageListAsync(long messageRowId, string listType, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO messagelist (ListType, MessageRowId, ErrorMessage, CreatedUtc)
            VALUES ($type, $mid, NULL, $now)
            ON CONFLICT(ListType, MessageRowId) DO UPDATE SET ErrorMessage = NULL;
            """;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$type", listType);
        command.Parameters.AddWithValue("$mid", messageRowId);
        command.Parameters.AddWithValue("$now", UtcNow());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves the local message row id for an account/folder/UID. When the same UID
    /// exists across UIDVALIDITY generations, the most recent generation wins.
    /// Returns <c>null</c> when the message is not present in the local database.
    /// </summary>
    public async Task<long?> FindMessageRowIdAsync(string accountId, string folderFullName, uint uid, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id FROM messages
            WHERE AccountId = $id AND FolderFullName = $folder AND Uid = $uid
            ORDER BY UidValidity DESC
            LIMIT 1;
            """;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", accountId);
        command.Parameters.AddWithValue("$folder", folderFullName);
        command.Parameters.AddWithValue("$uid", uid);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToInt64(result);
    }

    /// <summary>
    /// Builds the "Review and Delete" summary grouped by account, folder and from-address,
    /// driven entirely by the <c>messagelist</c> table.
    /// </summary>
    public async Task<IReadOnlyList<DeletionSummaryRow>> GetDeletionSummaryAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                COALESCE(a.EmailAddress, m.AccountId) AS Account,
                m.FolderFullName AS Folder,
                m.FromAddr AS FromAddr,
                COUNT(CASE WHEN ml.ListType = $server THEN 1 END) AS ServerCount,
                COALESCE(SUM(CASE WHEN ml.ListType = $server THEN m.AttachmentsCount ELSE 0 END), 0) AS ServerAtt,
                COALESCE(SUM(CASE WHEN ml.ListType = $server THEN m.TotalSize ELSE 0 END), 0) AS ServerSize,
                COUNT(CASE WHEN ml.ListType = $client THEN 1 END) AS ClientCount,
                COALESCE(SUM(CASE WHEN ml.ListType = $client THEN m.TotalSize ELSE 0 END), 0) AS ClientSize,
                MIN(m.DateUtc) AS MinDate,
                MAX(m.DateUtc) AS MaxDate
            FROM messagelist ml
            JOIN messages m ON m.Id = ml.MessageRowId
            LEFT JOIN accounts a ON a.AccountId = m.AccountId
            GROUP BY Account, Folder, FromAddr
            ORDER BY Account, Folder, FromAddr;
            """;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$server", MessageListType.DeleteFromServer);
        command.Parameters.AddWithValue("$client", MessageListType.DeleteFromClient);

        var rows = new List<DeletionSummaryRow>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DeletionSummaryRow
            {
                Account = reader.GetString(0),
                Folder = reader.GetString(1),
                FromAddress = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                ServerMessageCount = reader.GetInt32(3),
                ServerAttachmentsCount = reader.GetInt32(4),
                ServerTotalSize = reader.GetInt64(5),
                ClientMessageCount = reader.GetInt32(6),
                ClientTotalSize = reader.GetInt64(7),
                MinDate = ParseDate(reader, 8),
                MaxDate = ParseDate(reader, 9)
            });
        }

        return rows;
    }

    /// <summary>
    /// Returns the top <paramref name="limit"/> messages queued for deletion after
    /// sorting the entire queued dataset by the requested column and direction.
    /// </summary>
    public async Task<IReadOnlyList<DeletionMessageRow>> GetDeletionMessagesAsync(string sortColumn, bool ascending, int limit, CancellationToken cancellationToken = default)
    {
        // Whitelist the sortable columns; the value is never taken directly from user input.
        var orderBy = sortColumn switch
        {
            "Address" => "m.FromAddr",
            "Subject" => "m.Subject",
            "AttachmentCount" => "m.AttachmentsCount",
            "BodyPreview" => "BodyPreview",
            "ErrorMessage" => "ErrorMessage",
            _ => "m.DateUtc"
        };
        var direction = ascending ? "ASC" : "DESC";

        var sql = $"""
            SELECT
                m.DateUtc AS DateUtc,
                m.FromAddr AS FromAddr,
                m.Subject AS Subject,
                m.AttachmentsCount AS AttachmentsCount,
                substr(m.BodyText, 1, 100) AS BodyPreview,
                (SELECT ml2.ErrorMessage FROM messagelist ml2
                 WHERE ml2.MessageRowId = m.Id AND ml2.ErrorMessage IS NOT NULL LIMIT 1) AS ErrorMessage
            FROM messages m
            WHERE m.Id IN (SELECT DISTINCT MessageRowId FROM messagelist)
            ORDER BY {orderBy} {direction}
            LIMIT $limit;
            """;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$limit", limit);

        var rows = new List<DeletionMessageRow>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DeletionMessageRow
            {
                Date = ParseDate(reader, 0),
                Address = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Subject = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                AttachmentCount = reader.GetInt32(3),
                BodyPreview = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                ErrorMessage = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
            });
        }

        return rows;
    }

    /// <summary>
    /// Returns every message currently queued for deletion together with the lists it
    /// belongs to and the metadata needed to delete it from the server.
    /// </summary>
    public async Task<IReadOnlyList<DeletionTarget>> GetDeletionTargetsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                m.Id, m.AccountId, m.FolderFullName, m.UidValidity, m.Uid, m.Subject,
                MAX(CASE WHEN ml.ListType = $server THEN 1 ELSE 0 END) AS HasServer,
                MAX(CASE WHEN ml.ListType = $client THEN 1 ELSE 0 END) AS HasClient
            FROM messagelist ml
            JOIN messages m ON m.Id = ml.MessageRowId
            GROUP BY m.Id
            ORDER BY m.AccountId, m.FolderFullName, m.Uid;
            """;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$server", MessageListType.DeleteFromServer);
        command.Parameters.AddWithValue("$client", MessageListType.DeleteFromClient);

        var targets = new List<DeletionTarget>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            targets.Add(new DeletionTarget
            {
                MessageRowId = reader.GetInt64(0),
                AccountId = reader.GetString(1),
                FolderFullName = reader.GetString(2),
                UidValidity = (uint)reader.GetInt64(3),
                Uid = (uint)reader.GetInt64(4),
                Subject = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                DeleteFromServer = reader.GetInt32(6) == 1,
                DeleteFromClient = reader.GetInt32(7) == 1
            });
        }

        return targets;
    }

    /// <summary>Removes a single message-list entry (e.g. after a successful delete).</summary>
    public async Task RemoveFromMessageListAsync(long messageRowId, string listType, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM messagelist WHERE MessageRowId = $mid AND ListType = $type;";
        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$mid", messageRowId);
        command.Parameters.AddWithValue("$type", listType);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Records a server error against a message-list entry so it can be inspected later.</summary>
    public async Task SetMessageListErrorAsync(long messageRowId, string listType, string error, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE messagelist SET ErrorMessage = $error WHERE MessageRowId = $mid AND ListType = $type;";
        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$mid", messageRowId);
        command.Parameters.AddWithValue("$type", listType);
        command.Parameters.AddWithValue("$error", error);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a message row (and, via cascade, its attachments and any message-list
    /// entries) from the local database.
    /// </summary>
    public async Task DeleteMessageRowAsync(long messageRowId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM messages WHERE Id = $mid;";
        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$mid", messageRowId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DateTimeOffset? ParseDate(Microsoft.Data.Sqlite.SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var raw = reader.GetString(ordinal);
        return DateTimeOffset.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var value)
            ? value
            : null;
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection Connection =>
        _connection ?? throw new InvalidOperationException("The mail database has not been opened. Call OpenAsync first.");

    private static string UtcNow() => DateTime.UtcNow.ToString("o");

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
