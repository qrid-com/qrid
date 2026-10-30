using System;
using System.IO;
using System.Text.Json;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// OAuth2 client application registration details required to obtain access tokens
/// for Gmail (Google) and Outlook (Microsoft).
///
/// These identify the desktop application to the identity provider (they are not
/// end-user secrets). They are read from an optional <c>oauth.json</c> file placed
/// next to the accounts store, or from environment variables which take precedence.
/// </summary>
public sealed class OAuthSettings
{
    public const string GoogleClientIdVariable = "QRID_MAIL_GOOGLE_CLIENT_ID";
    public const string GoogleClientSecretVariable = "QRID_MAIL_GOOGLE_CLIENT_SECRET";
    public const string MicrosoftClientIdVariable = "QRID_MAIL_MICROSOFT_CLIENT_ID";
    public const string MicrosoftTenantVariable = "QRID_MAIL_MICROSOFT_TENANT";

    public string? GoogleClientId { get; set; }

    public string? GoogleClientSecret { get; set; }

    public string? MicrosoftClientId { get; set; }

    // "common" supports both personal Microsoft accounts and work/school (Office 365) accounts.
    public string MicrosoftTenant { get; set; } = "common";

    public static OAuthSettings Load()
    {
        var settings = LoadFromFile() ?? new OAuthSettings();

        settings.GoogleClientId = FirstNonEmpty(Environment.GetEnvironmentVariable(GoogleClientIdVariable), settings.GoogleClientId);
        settings.GoogleClientSecret = FirstNonEmpty(Environment.GetEnvironmentVariable(GoogleClientSecretVariable), settings.GoogleClientSecret);
        settings.MicrosoftClientId = FirstNonEmpty(Environment.GetEnvironmentVariable(MicrosoftClientIdVariable), settings.MicrosoftClientId);
        settings.MicrosoftTenant = FirstNonEmpty(Environment.GetEnvironmentVariable(MicrosoftTenantVariable), settings.MicrosoftTenant) ?? "common";

        return settings;
    }

    public static string GetSettingsPath()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QRID", "Mail");
        return Path.Combine(folder, "oauth.json");
    }

    private static OAuthSettings? LoadFromFile()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<OAuthSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception)
        {
            // A malformed settings file must not prevent the app from starting; fall back to
            // environment variables only.
            return null;
        }
    }

    private static string? FirstNonEmpty(string? primary, string? fallback)
    {
        return !string.IsNullOrWhiteSpace(primary) ? primary : fallback;
    }
}
