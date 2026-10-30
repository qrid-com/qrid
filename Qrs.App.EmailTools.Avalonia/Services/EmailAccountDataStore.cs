using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Json;
using Google.Apis.Util.Store;
using Qrs.App.EmailTools.Avalonia.Models;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// A Google <see cref="IDataStore"/> that persists the single token for one
/// <see cref="EmailAccount"/> into <see cref="EmailAccount.GoogleOAuthToken"/>.
///
/// The Google auth flow uses this store both to look up an existing token (so the
/// user is not prompted again) and to write back a refreshed token when the access
/// token expires. Because a store instance is scoped to a single account, the
/// per-entry <c>key</c> is ignored.
/// </summary>
internal sealed class EmailAccountDataStore : IDataStore
{
    private readonly EmailAccount _account;
    private readonly Func<Task> _persistAsync;

    public EmailAccountDataStore(EmailAccount account, Func<Task> persistAsync)
    {
        _account = account;
        _persistAsync = persistAsync;
    }

    public Task StoreAsync<T>(string key, T value)
    {
        _account.GoogleOAuthToken = NewtonsoftJsonSerializer.Instance.Serialize(value);
        return _persistAsync();
    }

    public Task DeleteAsync<T>(string key)
    {
        _account.GoogleOAuthToken = null;
        return _persistAsync();
    }

    public Task<T> GetAsync<T>(string key)
    {
        var data = _account.GoogleOAuthToken;
        var result = string.IsNullOrEmpty(data)
            ? default!
            : NewtonsoftJsonSerializer.Instance.Deserialize<T>(data);
        return Task.FromResult(result);
    }

    public Task ClearAsync()
    {
        _account.GoogleOAuthToken = null;
        return _persistAsync();
    }
}
