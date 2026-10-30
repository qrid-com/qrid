using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.Client.Context;
using Qrs.Proto;
using Qrs.Tunnel.Client;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// QRS <see cref="ClientContext"/> used only to wrap/unwrap the local database key
/// against the QRS Mobile App. Mirrors the pattern used by <c>VaultContext</c>.
/// </summary>
public sealed class MailContext : ClientContext
{
    public const string APP_NAME = "QRS MAIL";

    public sealed class Settings
    {
        public string TunnelServerDomain { get; set; } = string.Empty;
        public string TunnelServerUri { get; set; } = "https://qrid.qrid.com/qrs-tunnel-api/";

        // Optional API key required by some Tunnel Servers to create new sessions
        // (see Qrs.Tunnel.Server QrsTunnel:ApiKey). Null/empty means no api key is sent.
        public string ApiKey { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string AppRootPath { get; }
    public Settings ContextSettings { get; private set; } = new();

    private MailContext(string appRootPath, IUserInteraction ui, IAsymKeyItemProvider asymKeyItemProvider, ICookieProvider cookieProvider)
        : base(APP_NAME, ui, asymKeyItemProvider, cookieProvider)
    {
        AppRootPath = appRootPath;
    }

    private string SettingsFilePath => Path.Combine(AppRootPath, "MailContextSettings.json");

    public static async Task<ResponseModel<MailContext>> CreateInitialized(string appRootPath, IUserInteraction ui)
    {
        Directory.CreateDirectory(appRootPath);
        var asymKeyItemProvider = new FileAsymKeyItemProvider(appRootPath);
        var cookieProvider = new FileCookieProvider(appRootPath);

        var context = new MailContext(appRootPath, ui, asymKeyItemProvider, cookieProvider);
        await context.LoadSettings();

        var response = await context.InitFromConfigAsync(context.ContextSettings.TunnelServerDomain, context.ContextSettings.TunnelServerUri, context.ContextSettings.ApiKey);
        if (response.IsError())
            return NpErrorResponse<MailContext>(response);

        return NpSuccessResponse(context);
    }

    private async Task LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            ContextSettings = new Settings();
            await SaveSettings();
            return;
        }

        var json = await File.ReadAllTextAsync(SettingsFilePath);
        ContextSettings = JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
    }

    private async Task SaveSettings()
    {
        Directory.CreateDirectory(AppRootPath);
        var json = JsonSerializer.Serialize(ContextSettings, JsonOptions);
        await File.WriteAllTextAsync(SettingsFilePath, json);
    }
}
