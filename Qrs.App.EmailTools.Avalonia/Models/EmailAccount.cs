using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Qrs.App.EmailTools.Avalonia.Models;

public sealed class EmailAccount : INotifyPropertyChanged
{
    private string _accountId = System.Guid.NewGuid().ToString("N");
    private string _displayName = string.Empty;
    private string _emailAddress = string.Empty;
    private EmailProvider _provider;
    private string _incomingHost = string.Empty;
    private int _incomingPort = 993;
    private bool _incomingUseSsl = true;
    private string _outgoingHost = string.Empty;
    private int _outgoingPort = 587;
    private bool _outgoingUseStartTls = true;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string? _googleOAuthToken;
    private string? _microsoftOAuthCache;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Stable local identifier that survives edits to the email address, username or provider.
    // Used as the primary key that links locally-stored messages/attachments to this account.
    public string AccountId { get => _accountId; set => SetField(ref _accountId, value); }

    public string DisplayName { get => _displayName; set => SetField(ref _displayName, value); }
    public string EmailAddress { get => _emailAddress; set => SetField(ref _emailAddress, value); }
    public EmailProvider Provider { get => _provider; set => SetField(ref _provider, value); }
    public string IncomingHost { get => _incomingHost; set => SetField(ref _incomingHost, value); }
    public int IncomingPort { get => _incomingPort; set => SetField(ref _incomingPort, value); }
    public bool IncomingUseSsl { get => _incomingUseSsl; set => SetField(ref _incomingUseSsl, value); }
    public string OutgoingHost { get => _outgoingHost; set => SetField(ref _outgoingHost, value); }
    public int OutgoingPort { get => _outgoingPort; set => SetField(ref _outgoingPort, value); }
    public bool OutgoingUseStartTls { get => _outgoingUseStartTls; set => SetField(ref _outgoingUseStartTls, value); }
    public string Username { get => _username; set => SetField(ref _username, value); }

    [JsonIgnore]
    public string Password { get => _password; set => SetField(ref _password, value); }

    // OAuth2 token material. Persisted alongside the account (unencrypted) for now,
    // exactly like the account username; a later change will move this to encrypted storage.
    // Gmail stores a serialized Google TokenResponse (access + refresh token).
    public string? GoogleOAuthToken { get => _googleOAuthToken; set => SetField(ref _googleOAuthToken, value); }

    // Outlook stores a serialized MSAL token cache (base64) that holds the refresh token.
    public string? MicrosoftOAuthCache { get => _microsoftOAuthCache; set => SetField(ref _microsoftOAuthCache, value); }

    [JsonIgnore]
    public bool UsesOAuth => Provider is EmailProvider.Gmail or EmailProvider.Outlook;

    [JsonIgnore]
    public string Label => string.IsNullOrWhiteSpace(DisplayName) ? EmailAddress : $"{DisplayName} <{EmailAddress}>";

    public void ApplyProviderDefaults()
    {
        switch (Provider)
        {
            case EmailProvider.Gmail:
                IncomingHost = "imap.gmail.com";
                IncomingPort = 993;
                IncomingUseSsl = true;
                OutgoingHost = "smtp.gmail.com";
                OutgoingPort = 587;
                OutgoingUseStartTls = true;
                break;
            case EmailProvider.Outlook:
                IncomingHost = "outlook.office365.com";
                IncomingPort = 993;
                IncomingUseSsl = true;
                OutgoingHost = "smtp.office365.com";
                OutgoingPort = 587;
                OutgoingUseStartTls = true;
                break;
        }

        if (string.IsNullOrWhiteSpace(Username))
            Username = EmailAddress;
    }

    public EmailAccount Clone()
    {
        return new EmailAccount
        {
            AccountId = AccountId,
            DisplayName = DisplayName,
            EmailAddress = EmailAddress,
            Provider = Provider,
            IncomingHost = IncomingHost,
            IncomingPort = IncomingPort,
            IncomingUseSsl = IncomingUseSsl,
            OutgoingHost = OutgoingHost,
            OutgoingPort = OutgoingPort,
            OutgoingUseStartTls = OutgoingUseStartTls,
            Username = Username,
            Password = Password,
            GoogleOAuthToken = GoogleOAuthToken,
            MicrosoftOAuthCache = MicrosoftOAuthCache
        };
    }

    public void CopyFrom(EmailAccount account)
    {
        // Preserve any existing OAuth tokens unless the identity the tokens were issued
        // for has changed (provider, email address or username), in which case they are
        // no longer valid and must be re-acquired.
        var keepTokens = Provider == account.Provider
            && string.Equals(EmailAddress, account.EmailAddress, System.StringComparison.OrdinalIgnoreCase)
            && string.Equals(Username, account.Username, System.StringComparison.OrdinalIgnoreCase);

        DisplayName = account.DisplayName;
        EmailAddress = account.EmailAddress;
        Provider = account.Provider;
        IncomingHost = account.IncomingHost;
        IncomingPort = account.IncomingPort;
        IncomingUseSsl = account.IncomingUseSsl;
        OutgoingHost = account.OutgoingHost;
        OutgoingPort = account.OutgoingPort;
        OutgoingUseStartTls = account.OutgoingUseStartTls;
        Username = account.Username;
        Password = account.Password;

        if (!keepTokens)
        {
            GoogleOAuthToken = null;
            MicrosoftOAuthCache = null;
        }
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName is nameof(DisplayName) or nameof(EmailAddress))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
    }
}
