using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using BarcodeScanning;

namespace Qrs.Mobile.App;

public static class MauiProgramExtensions
{
	public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
	{
		builder
            .UseMauiApp<App>()
			
			.UseMauiCommunityToolkit()
            .UseBarcodeScanning()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

        // This line ensures your app uses the icon in Resources/Images/appicon.png
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            Microsoft.Maui.Handlers.ImageHandler.Mapper.AppendToMapping("AppIcon", (handler, view) => {
                handler.PlatformView.SetClipToOutline(true);
            });
#endif
        });

        // TODO: Add the entry points to your Apps here.
        // See also: https://learn.microsoft.com/dotnet/maui/fundamentals/app-lifecycle
        // builder.Services.AddTransient<AppShell, AppShell>();



        return builder;
	}
}
