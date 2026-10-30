using System;
using System.IO;
using System.Threading.Tasks;
using Google.Protobuf;
using Qrid.Common.Client.UI;
using Qrid.Common.Proto;
using Qrs.Proto;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// Manages the SQLCipher master key for the local mail database.
///
/// On first use a random 256-bit key is generated and stored inside a
/// QRS-encrypted envelope file (<c>mail-db.key.qrs</c>): the key is encrypted
/// with a per-file DEK that is wrapped by the Master Key held on the QRS Mobile
/// App. Unlocking the database therefore requires a QR-code round-trip with the
/// phone, and the raw key is never persisted in plaintext.
/// </summary>
public sealed class MailDbKeyStore
{
    private const int KeyLengthBytes = 32;

    private readonly string _appRootPath;
    private readonly IUserInteraction _ui;
    private readonly string _keyFilePath;

    private MailContext? _context;

    public MailDbKeyStore(string appRootPath, IUserInteraction ui)
    {
        _appRootPath = appRootPath;
        _ui = ui;
        _keyFilePath = Path.Combine(appRootPath, "mail-db.key.qrs");
    }

    /// <summary>
    /// Returns the database key, creating and QRS-wrapping a new random key on first run.
    /// </summary>
    public async Task<byte[]> LoadOrCreateKeyAsync()
    {
        var context = await GetContextAsync();

        return File.Exists(_keyFilePath)
            ? await UnwrapKeyAsync(context)
            : await CreateAndWrapKeyAsync(context);
    }

    private async Task<MailContext> GetContextAsync()
    {
        if (_context != null)
            return _context;

        var response = await MailContext.CreateInitialized(_appRootPath, _ui);
        if (response.IsError())
            throw new InvalidOperationException($"Cannot initialize QRS context for the local database key: {response.Message}");

        _context = response.Data;
        return _context;
    }

    private async Task<byte[]> CreateAndWrapKeyAsync(MailContext context)
    {
        var key = CryptoUtils.GetRandomBytes(KeyLengthBytes);

        var wrapper = new MailDbKeyWrapper(MailContext.APP_NAME)
        {
            Payload = new PaddedData
            {
                Data = ByteString.CopyFrom(key),
                Padding = string.Empty
            }
        };

        var response = await context.WrapNewPayload(wrapper);
        if (response.IsError())
            throw new InvalidOperationException($"Failed to protect the local database key with the QRS Mobile App: {response.Message}");

        var tempPath = _keyFilePath + ".tmp";
        await File.WriteAllBytesAsync(tempPath, wrapper.QrsEnvelope.GetByteArray());
        File.Move(tempPath, _keyFilePath, overwrite: true);

        return key;
    }

    private async Task<byte[]> UnwrapKeyAsync(MailContext context)
    {
        var envelopeBytes = await File.ReadAllBytesAsync(_keyFilePath);

        var wrapper = new MailDbKeyWrapper(MailContext.APP_NAME)
        {
            QrsEnvelope = QrsEnvelope.Create(envelopeBytes)
        };

        var response = await context.UnwrapPayload(wrapper);
        if (response.IsError())
            throw new InvalidOperationException($"Failed to unlock the local database key with the QRS Mobile App: {response.Message}");

        return wrapper.Payload.Data.ToByteArray();
    }
}
