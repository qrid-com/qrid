using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Qrs.App.PasswordManager.Components.Code.ViewModels;
using Qrs.App.PasswordManager.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class QridComponentBase : ComponentBase 
    {
        [Inject] 
        public ApplicationService App { get; set; }

        [Inject]
        public ILogger<QridComponentBase> Log { get; set; }

        [Parameter]
        public bool Visible { get; set; } = true;
        public string VisibleClass => Visible ? string.Empty : " d-none "; // MudBlazor's version of display:none

        public PwmContext Context => App.Context;
        public ApplicationState State => App.State;

        public MudTheme Theme => App.AppRoot.MudThemeProvider.Theme;
        public Palette Palette => App.AppRoot.MudThemeProvider.IsDarkMode ? Theme.PaletteDark : Theme.PaletteLight;

        private static int TagCount = 0;
        private static Object lockObj = new object();
        private static string GetTag()
        {
            lock(lockObj)
            {
                TagCount++;
                //  {obj.GetType().Name}
                return $"Tag--{TagCount}--{DateTime.Now.ToLongTimeString()}";
            }
        }

        private string GetInstanceTag()
        {
           return $"{this.GetType().Name}--{DateTime.Now.ToLongTimeString()}";
        }

        public string Tag = GetTag();

        public void LogInfo(string message)
        {
            App.LogInfo(message);
        }

        public void StateChanged()
        {
            LogInfo($"StateChanged({GetInstanceTag()})");
            this.StateHasChanged();
        }

        public async Task StateChangedAsync()
        {
            LogInfo($"StateChangedAsync({GetInstanceTag()})");
            // https://learn.microsoft.com/en-us/aspnet/core/blazor/components/rendering?view=aspnetcore-5.0#when-to-call-statehaschanged
            await InvokeAsync(this.StateHasChanged);
        }

        public virtual void StateChangedCascade()
        {
            StateChanged();
        }

        [Parameter]
        public ViewModelBase ViewModel {  get; set; }
        public void RegisterInViewModel()
        {
            if (ViewModel == null) { return; }

            if (ViewModel.View == this) { return; }

            if (ViewModel.View != null)
            {
                // unregister the old .View
                // may need to do something with the previous NON-NULL .View
                // for now, do nothing here
            }

            // register new .View
            ViewModel.View = this;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            RegisterInViewModel();
        }


    }

    public class QridComponent<P> : QridComponentBase // where P : QridComponentBase
    {
       

        [Parameter]
        public P Parent { get; set; }

        public QridComponentBase ParentQc => Parent as QridComponentBase;

        public string GetParentTag()
        {
            if (Parent == null)
                return "Parent is null";
            else
                return ParentQc.Tag;
        }

        // https://www.thinktecture.com/en/blazor/blazor-components-lifecycle-is-not-always-straightforward/
        // COMPONENT LIFECYCLE - IN ORDER OF EXECUTION (TOP TO BOTTOM)
        // Constructor
        // SetParametersAsync
        // - base.SetParametersAsync MUST be called, if overriding
        // - do NOT create Components that write to their own [Parameter] properties
        // OnInitialized/OnInitializedAsync - called after the initial(!) setting of the parameters
        // OnParametersSet/OnParametersSetAsync
        // - OnParametersSet should NOT be preferred to OnInitialized/OnInitializedAsync because otherwise the component will be rendered without initialization.
        // OnAfterRender(firstRender)/OnAfterRenderAsync(firstRender)
        // - method pair OnAfterRender/OnAfterRenderAsync may be called (again) although the previous execution of OnAfterRenderAsync is not finished yet
        // IDisposable.Dispose() (if we navigate to another page)
        // -  should be used for all kinds of cleanup, like unsubscribing from events and for stopping asynchronous calls
        // - All asynchronous calls should have CancellationToken support and be canceled in Dispose

        // region Lifecycle overrrides - IN ORDER OF EXECUTION (TOP TO BOTTOM)

    }
}
