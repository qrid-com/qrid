namespace Qrs.Mobile.App.iOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp()
                // must be registered after UseSharedMauiApp() (i.e. after UseBarcodeScanning()) to replace the default CameraView handler
                .ConfigureMauiHandlers(handlers => handlers.AddHandler<BarcodeScanning.CameraView, PortraitCameraViewHandler>());

            return builder.Build();
        }
    }
}
