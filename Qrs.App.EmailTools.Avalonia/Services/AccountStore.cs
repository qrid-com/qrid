using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System;
using Qrs.App.EmailTools.Avalonia.Models;
using System.Text.Json;

namespace Qrs.App.EmailTools.Avalonia.Services;

public sealed class AccountStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public AccountStore()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QRID", "Mail");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "accounts.json");
    }

    public async Task<List<EmailAccount>> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<EmailAccount>>(stream, _jsonOptions) ?? [];
    }

    public async Task SaveAsync(IEnumerable<EmailAccount> accounts)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, accounts, _jsonOptions);
    }
}
