using Microsoft.AspNetCore.Components.WebView.Maui;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Qrs.App.PasswordManager.Components.Code;
using Microsoft.Extensions.Logging;


namespace Qrs.App.PasswordManager.Hybrid
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            //builder.Services.AddLogging(logging =>
            //{
            //    logging.AddDebug();
            //    logging.AddConsole();
            //    //logging.AddSimpleConsole();
            //    //logging.AddSimpleConsole(o => o.ColorBehavior = LoggerColorBehavior.Disabled);
            //});

            builder.Services.AddScoped(sp => new HttpClient() );
            //{ BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }); ; ; // ;

            builder.Services.AddMudServices();
            builder.Services.AddMudBlazorDialog();

            //builder.Services.AddMudExtensions();

            builder.Services.AddAuthorizationCore();

            builder.Services.AddSingleton<ApplicationSingleton>();

            // ApplicationService is defined in a separate Components project
            // .AddSingleton<AppSvc> - does not work - WTF??? - throws error when trying to use NavigationManager.
            // So have to use .AddScoped<AppSvc>
            builder.Services.AddScoped<ApplicationService>();
            builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

            return builder.Build();
        }
    }
}