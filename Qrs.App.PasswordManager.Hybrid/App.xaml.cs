using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Components.Code;

namespace Qrs.App.PasswordManager.Hybrid
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; }
        public ApplicationSingleton ApplicationSingleton { get; private set; }
        public ApplicationService Apps => ApplicationSingleton?.AppSvc;

        public App(IServiceProvider provider)
        {
            InitializeComponent();
            Services = provider;
            ApplicationSingleton = Services.GetService<ApplicationSingleton>();
            //Apps = Services.GetService<ApplicationService>(); << does not work, getting a different separate instance. Cannot make AppSvc as Singleton b/c then having NavigationManager error (fixed in .NET7?)
            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            Window window = base.CreateWindow(activationState);

            window.Destroying += async (s, e) =>
            {
                await Apps?.SavePersistentState();
            };

            return window;
        }

    }
}