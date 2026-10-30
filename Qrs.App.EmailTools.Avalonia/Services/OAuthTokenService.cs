using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.Identity.Client;
using Qrs.App.EmailTools.Avalonia.Models;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// Acquires and renews OAuth2 access tokens for Gmail (Google) and Outlook (Microsoft)
/// mail accounts. Both providers reject plain username/password authentication and
/// require OAuth2 bearer tokens with provider-specific scopes.
///
/// Tokens (including the refresh token material used for periodic renewal) are stored on
/// the <see cref="EmailAccount"/> and persisted through <see cref="PersistAccountAsync"/>,
/// in the same unencrypted store used for account usernames. This is a deliberate interim
/// step; a later change will move token storage to encrypted storage.
/// </summary>
public sealed class OAuthTokenService
{
    // Full IMAP + SMTP access to the Gmail mailbox.
    private static readonly string[] GoogleScopes = { "https://mail.google.com/" };

    // Delegated IMAP + SMTP access to the Outlook mailbox.
    private static readonly string[] MicrosoftScopes =
    {
        "https://outlook.office.com/IMAP.AccessAsUser.All",
        "https://outlook.office.com/SMTP.Send"
    };

    private readonly OAuthSettings _settings;

    public OAuthTokenService(OAuthSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Invoked whenever token material changes so the caller can persist the updated account.
    /// </summary>
    public Func<EmailAccount, CancellationToken, Task>? PersistAccountAsync { get; set; }

    /// <summary>
    /// Returns a valid access token for the account, acquiring it interactively on first use
    /// and transparently refreshing it when it has expired.
    /// </summary>
    public Task<string> GetAccessTokenAsync(EmailAccount account, CancellationToken cancellationToken = default)
    {
        return account.Provider switch
        {
            EmailProvider.Gmail => GetGoogleAccessTokenAsync(account, cancellationToken),
            EmailProvider.Outlook => GetMicrosoftAccessTokenAsync(account, cancellationToken),
            _ => throw new InvalidOperationException("OAuth2 authentication is only supported for Gmail and Outlook accounts.")
        };
    }

    private async Task<string> GetGoogleAccessTokenAsync(EmailAccount account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.GoogleClientId) || string.IsNullOrWhiteSpace(_settings.GoogleClientSecret))
        {
            throw new InvalidOperationException(
                $"Google OAuth2 client credentials are not configured. Set the {OAuthSettings.GoogleClientIdVariable} and " +
                $"{OAuthSettings.GoogleClientSecretVariable} environment variables or add them to {OAuthSettings.GetSettingsPath()}.");
        }

        var dataStore = new EmailAccountDataStore(account, () => PersistAsync(account, cancellationToken));
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _settings.GoogleClientId,
                ClientSecret = _settings.GoogleClientSecret
            },
            Scopes = GoogleScopes,
            DataStore = dataStore
        });

        // Loads the stored token if present; otherwise opens the system browser for consent.
        var userKey = string.IsNullOrWhiteSpace(account.EmailAddress) ? account.Username : account.EmailAddress;
        var installedApp = new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver());
        var credential = await installedApp.AuthorizeAsync(userKey, cancellationToken).ConfigureAwait(false);

        // Returns the current access token, refreshing (and re-persisting via the data store) if it has expired.
        return await credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetMicrosoftAccessTokenAsync(EmailAccount account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.MicrosoftClientId))
        {
            throw new InvalidOperationException(
                $"Microsoft OAuth2 client id is not configured. Set the {OAuthSettings.MicrosoftClientIdVariable} environment " +
                $"variable or add it to {OAuthSettings.GetSettingsPath()}.");
        }

        var app = PublicClientApplicationBuilder
            .Create(_settings.MicrosoftClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, _settings.MicrosoftTenant)
            .WithRedirectUri("http://localhost")
            .Build();

        // Back the MSAL token cache with the account's persisted cache blob so that refresh
        // tokens survive restarts and are renewed automatically by AcquireTokenSilent.
        app.UserTokenCache.SetBeforeAccess(args =>
        {
            var data = account.MicrosoftOAuthCache;
            args.TokenCache.DeserializeMsalV3(string.IsNullOrEmpty(data) ? null : Convert.FromBase64String(data));
        });
        app.UserTokenCache.SetAfterAccess(args =>
        {
            if (!args.HasStateChanged)
                return;

            var bytes = args.TokenCache.SerializeMsalV3();
            account.MicrosoftOAuthCache = bytes is { Length: > 0 } ? Convert.ToBase64String(bytes) : null;
        });

        AuthenticationResult result;
        var cachedAccounts = await app.GetAccountsAsync().ConfigureAwait(false);
        var cachedAccount = cachedAccounts.FirstOrDefault();

        try
        {
            if (cachedAccount == null)
                throw new MsalUiRequiredException("no_account", "No cached Microsoft account.");

            result = await app.AcquireTokenSilent(MicrosoftScopes, cachedAccount)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            var interactive = app.AcquireTokenInteractive(MicrosoftScopes);
            if (!string.IsNullOrWhiteSpace(account.EmailAddress))
                interactive = interactive.WithLoginHint(account.EmailAddress);

            result = await interactive.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }

        // The cache callback updated MicrosoftOAuthCache synchronously; persist it to disk.
        await PersistAsync(account, cancellationToken).ConfigureAwait(false);
        return result.AccessToken;
    }

    private Task PersistAsync(EmailAccount account, CancellationToken cancellationToken)
    {
        return PersistAccountAsync?.Invoke(account, cancellationToken) ?? Task.CompletedTask;
    }
}
