
using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrid.Common.Utils;
using Qrs.App.PasswordManager.Components.Code.ViewModels;
using Qrs.App.PasswordManager.Components.Components;
using Qrs.App.PasswordManager.Client.Models;
using Qrs.App.PasswordManager.Client.Proto;
using Qrs.Tunnel.Client;
using System.Diagnostics;
using System.Text;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class ApplicationService : IUserInteraction
    {
        public Qrs.App.PasswordManager.Client.PwmContext Context { get; set; }
        public ApplicationVm Vm { get; set; }
        public ApplicationState State { get; set; }
        public FileAsymKeyItemProvider AsymKeyItemProvider { get; set; }
        public FileCookieProvider CookieProvider { get; set; }

        public IJSRuntime JS { get; set; }
        public IJSInProcessRuntime JSIP => (IJSInProcessRuntime)JS;

        public ILogger<ApplicationService> Log { get; set; }

        public ApplicationService(IDialogService dialogService, NavigationManager navigationManager, IJSRuntime jsRuntime, ILogger<ApplicationService> logger, ApplicationSingleton appSingleton)
        {
            appSingleton.RegisterAppSvc(this); // it may throw

            DialogService = dialogService;
            NavigationManager = navigationManager;
            Log = logger;
            // JavaScript Runtime
            JS = jsRuntime;

            Vm = new ApplicationVm();
            State = new ApplicationState();

            AsymKeyItemProvider = new FileAsymKeyItemProvider();
            CookieProvider = new FileCookieProvider(CookieFolderPath);

            Context = new Qrs.App.PasswordManager.Client.PwmContext(this, AsymKeyItemProvider, CookieProvider);

            Vm.MainScreenVm = new MainScreenVm();

            AppSvcRef = DotNetObjectReference.Create(this); // too early to call it here?

        }

        #region Test, Experimental

        public int Count_OnParametersSet = 0;
        public int Count_OnParametersSetAsync = 0;
        public string CurrentTestTime;
        public void PerformTest()
        {
            CurrentTestTime = DateTime.Now.ToLongTimeString();
            AppRoot.StateChanged();
        }
        #endregion


        #region JavaScript calls

        private DotNetObjectReference<ApplicationService>? AppSvcRef;

        public void JsMeasureHeight()
        {
            LogInfo($"[C#] App.JsMeasureHeight() - before JS measureHeight invoke ");
            //JS.InvokeAsync<string>("measureHeight", AppSvcRef);
            JSIP.Invoke<string>("measureHeight", AppSvcRef);
            LogInfo($"[C#] App.JsMeasureHeight() - after JS measureHeight invoke ");
        }

        public void JsMeasureWindowSize()
        {
            LogInfo($"[C#] App.JsMeasureWindowSize() - before JS measureWindowSize() invoke ");
            //JS.InvokeAsync<string>("measureHeight", AppSvcRef);
            JSIP.Invoke<string>("measureWindowSize", AppSvcRef);
            LogInfo($"[C#] App.JsMeasureWindowSize() - after JS measureWindowSize() invoke ");
        }

        public void JsMarkdownResize()
        {
            JSIP.Invoke<string>("MarkdownResize");
        }

        public void JsDelayedMarkdownResize()
        {
            JSIP.Invoke<string>("DelayedMarkdownResize");
        }
        public void JsAddWindowSizeListener()
        {
            LogInfo($"[C#] App.JsAddWindowSizeListener() - before JS invoke ");
            JSIP.Invoke<string>("AddWindowSizeListener", AppSvcRef);
            LogInfo($"[C#] App.JsAddWindowSizeListener() - after JS invoke ");
        }

        private DateTime lastResize;

        [JSInvokable]
        public void UpdateWindowSize()
        {
            // called by JS on Window Resize event

            // debounce - do not want to call it too often
            if (lastResize.AddMilliseconds(100).CompareTo(DateTime.Now) > 0)
                // debounce interval has not yet passed, so skip this resize event without handling
                return;

            lastResize = DateTime.Now;


        }

        public void Dispose()
        {
            AppSvcRef?.Dispose();
        }



        #endregion

        #region Cookies

        public async Task SetCookie(string key, string value, int? days = null)
        {
            var curExp = (days != null) ? (days > 0 ? DateToUTC(days.Value) : "") : "";
            await SetCookieJs($"{key}={value}; expires={curExp}; path=/");
        }

        public async Task<string> GetCookie(string key, string def = "")
        {
            var cValue = await GetCookieJs();
            if (string.IsNullOrEmpty(cValue)) return def;

            var vals = cValue.Split(';');
            foreach (var val in vals)
                if (!string.IsNullOrEmpty(val) && val.IndexOf('=') > 0)
                    if (val.Substring(0, val.IndexOf('=')).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                        return val.Substring(val.IndexOf('=') + 1);
            return def;
        }

        private async Task SetCookieJs(string value)
        {
            await JS.InvokeVoidAsync("eval", $"document.cookie = \"{value}\"");
        }

        private async Task<string> GetCookieJs()
        {
            return await JS.InvokeAsync<string>("eval", $"document.cookie");
        }

        private static string DateToUTC(int days) => DateTime.Now.AddDays(days).ToUniversalTime().ToString("R");

        // copies the given text into the OS clipboard, returns false when the browser/webview refused to do it
        public async Task<bool> CopyToClipboardAsync(string text)
        {
            text ??= string.Empty;

            try
            {
                await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
                return true;
            }
            catch (Exception e)
            {
                // clipboard API is not available (e.g. insecure context) - fall back to the legacy approach below
                LogInfo($"CopyToClipboardAsync - navigator.clipboard.writeText failed - {e.Message}");
            }

            try
            {
                // text is passed as a JSON literal, so it cannot break out of the script
                var jsonText = System.Text.Json.JsonSerializer.Serialize(text);
                var script =
                    "(function(t){var a=document.createElement('textarea');a.value=t;a.setAttribute('readonly','');" +
                    "a.style.position='fixed';a.style.opacity='0';document.body.appendChild(a);a.select();" +
                    "var ok=false;try{ok=document.execCommand('copy');}catch(e){ok=false;}" +
                    $"document.body.removeChild(a);return ok;}})({jsonText})";

                return await JS.InvokeAsync<bool>("eval", script);
            }
            catch (Exception e)
            {
                LogInfo($"CopyToClipboardAsync - fallback copy failed - {e.Message}");
                return false;
            }
        }

        #endregion

        #region Popup - dialog window (QR Code dialog, etc)

        public IDialogService DialogService; // used to show QrCode window, etc
        private IDialogReference _dialogReference;

        public Components.QrCommandDialog QrCommandDialog
        {
            get
            {
                if (_dialogReference == null)
                {
                    var parameters = new DialogParameters { };
                    _dialogReference = DialogService.Show<Components.QrCommandDialog>("", parameters);
                }
                return _dialogReference?.Dialog as Components.QrCommandDialog;
            }
        }

        #endregion

        #region Popover - status message window, etc

        public Popover Popover { get; set; } // used to show status messages in a Popover element

        public void RegisterPopover(Popover popover)
        {
            // this is called from Popover.razor after it has initialized
            Popover = popover;
        }

        // see Show/HideMessage(Window) functions instead of PopoverShow/Hide

        //public void PopoverShow()
        //{
        //    if (Popover != null)
        //        Popover.IsVisible = true;
        //}
        //public void PopoverHide()
        //{
        //    if (Popover != null)
        //        Popover.IsVisible = false;

        //}

        #endregion

        #region Auth State
        public AuthStateProvider AuthStateProvider { get; set; }
        public void RegisterAuthStateProvider(AuthStateProvider provider)
        {
            AuthStateProvider = provider;
        }
        #endregion

        #region Log

        public void LogInfo(string message)
        {
            //Log.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} - {message}");

            Console.WriteLine($"{DateTime.Now.ToString()} - {message}");
            Debug.WriteLine($"{DateTime.Now.ToString()} - {message}");
        }

        #endregion

        #region AppRoot - reference to App.razor component
        public Qrs.App.PasswordManager.Components.App AppRoot { get; set; }
        public void RegisterAppRoot(Qrs.App.PasswordManager.Components.App appRoot)
        {
            // this is called from App.razor after it has initialized
            AppRoot = appRoot;

            //AppRoot.MudThemeProvider.Theme.LayoutProperties.DrawerWidthLeft = "160px";

        }
        #endregion

        #region ResourceBoxVm

        //public MainScreenVm Vm.MainScreenVm { get; set; }
        public async Task RegisterMainScreenView(MainScreenView view, string folderPath)
        {
            // todo - log the call and params
            Vm.MainScreenVm.View = view;

        }

        public void SetSelectedItem(SearchedItem si)
        {

            var ri = si?.ResourceItem;
            Vm.MainScreenVm.ListVm.ListView?.SetSelectedItem(si);

            if (ri == null)
            {
                // this is supported and should clear the form
                Vm.MainScreenVm.DetailsVm.DataModelSet(null);
                LogInfo("called SetSelectedItem( null )");
                return;
            }

            if (Vm.IsView) // ???
            {
                Vm.MainScreenVm.DetailsVm.DataModelSet(ri);
            }
        }

        public void SelectFirstItem()
        {
            var si = Context.Folder?.Searched.Items.FirstOrDefault();

            Vm.SetMode(EditMode.View);

            SetSelectedItem(si);


            //ModList ml = Vm.MainScreenVm?.View?.MessageList?.MudList;
            //if (ml != null)
            //{
            //    ml.SetSelectedValueForced(Vm.MainScreenVm.ViewItem);
            //}

        }

        #endregion

        #region Navigation - to URL, etc

        public NavigationManager NavigationManager;
        private string _returnUrl;

        public string CurrentUrl()
        {
            return NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        }

        public void Navigate(string uri)
        {
            NavigationManager.NavigateTo(uri);
        }

        public void NavigateToLogin()
        {
            _returnUrl = CurrentUrl();
            Navigate("/qrlogin"); // ?returnUrl=" + HttpUtility.UrlEncode(CurrentUrl())
        }

        public void NavigateToInbox()
        {
            _returnUrl = CurrentUrl();
            Navigate("/inbox"); // ?returnUrl=" + HttpUtility.UrlEncode(CurrentUrl())
        }

        public void NavigateToInit()
        {
            _returnUrl = CurrentUrl();
            Navigate("/init"); // ?returnUrl=" + HttpUtility.UrlEncode(CurrentUrl())
        }

        public void NavigateToConfirmSettings()
        {
            Navigate("/confirmsettings"); // ?returnUrl=" + HttpUtility.UrlEncode(CurrentUrl())
        }

        public bool IsLoginPage()
        {
            var currentUrl = CurrentUrl().ToLower();
            return currentUrl.StartsWith("qrlogin") || currentUrl.StartsWith("/qrlogin");
        }

        public bool IsInboxOrSentPage()
        {
            var currentUrl = CurrentUrl().ToLower();
            var res = currentUrl.StartsWith("inbox") || currentUrl.StartsWith("/inbox")
                || currentUrl.StartsWith("sentbox") || currentUrl.StartsWith("/sentbox");
            return res;
        }

        public bool IsInboxPage()
        {
            var currentUrl = CurrentUrl().ToLower();
            var res = currentUrl.StartsWith("inbox") || currentUrl.StartsWith("/inbox");
            return res;
        }

        public bool IsSentboxPage()
        {
            var currentUrl = CurrentUrl().ToLower();
            var res = currentUrl.StartsWith("sentbox") || currentUrl.StartsWith("/sentbox");
            return res;
        }

        public bool IsEditingNewMessage => Vm.Mode == Code.ViewModels.EditMode.New;



        #endregion

        #region QR UI implementation

        public void ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        public void ShowQrCodeWindow(string sessionId, string qrString)
        {
            var parameters = new DialogParameters { { "QrCodeString", qrString } };
            _dialogReference = DialogService.Show<Components.QrCommandDialog>("", parameters);

            //QrCommandDialog.SetQrCodeString(qrString);
        }

        public async Task ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            var parameters = new DialogParameters { { "QrCodeString", qrString } };
            _dialogReference = await DialogService.ShowAsync<Components.QrCommandDialog>("", parameters);

            //QrCommandDialog.SetQrCodeString(qrString);
        }

        public void HideQrCodeWindow(string sessionId)
        {
            _dialogReference.Close(DialogResult.Ok(true));
            //QrCommandDialog.SetQrCodeString($"called HideQrCodeWindow(sessionId='{sessionId}')");

            //AuthStateProvider.AuthChanged(); here it is too early to do this call - the IsLoggedIn is not yet true
        }

        public void ShowMessageWindow(string title)
        {
            if (Popover != null)
            {
                Popover.Title = title;
                Popover.IsVisible = true;
                Popover.StateChanged();
            }

        }
        public void HideMessageWindow()
        {
            if (Popover != null)
            {
                Popover.IsVisible = false;
                Popover.StateChanged();
            }

        }

        public void ShowMessage(string message)
        {
            if (Popover != null)
            {
                Popover.Message = message;
                Popover.StateChanged();
            }

            //QrCommandDialog?.SetQrCodeString($"called ShowMessage(message='{message}')");
        }

        public void ShowError(string message)
        {
            ShowMessageWindow("ERROR !!!");
            ShowMessage(message);

            //QrCommandDialog?.SetQrCodeString($"called ShowError(message='{message}')");
        }

        public async Task ShowErrorAsync(string message)
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall };

            bool? result = await DialogService.ShowMessageBox("ERROR", message, yesText: "OK", options: options);
        }

        public async Task<bool> ShowConfirmationAsync(string message)
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall };
            bool? result = await DialogService.ShowMessageBox("Confirm", message, yesText: "Yes", noText: "No", options:options);
            return result ?? false;
        }

        public async Task<bool> ShowMessageBoxAsync(string title, string message)
        {
            var markupMessage = new MarkupString(message);
            return await ShowMessageBoxAsync(title, markupMessage);
        }

        public async Task<bool> ShowMessageBoxAsync(string title, MarkupString markupMessage)
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium };
            bool? result = await DialogService.ShowMessageBox(title, markupMessage, yesText:"OK", options: options);
            return result ?? false;
        }

        public void SetDescription(string title, string subtitle)
        {
            // TODO - implement properly
            LogInfo("SetDescription(title='{title}', subtitle='{subtitle}')");
        }

        public void SetTunnelServiceUri(string tunnelServiceUri)
        {
            // TODO - implement properly
            LogInfo($"SetTunnelServiceUri(tunnelServiceUri='{tunnelServiceUri}')");
        }

        #endregion


        public string GetSortText(SortBy sortBy)
        {
            switch (sortBy)
            {
                case SortBy.CreatedAsc: { return "Date Asc"; }
                case SortBy.CreatedDesc: { return "Date Desc"; }
                case SortBy.NameAsc: { return "Name Asc"; }
                case SortBy.NameDesc: { return "Name Desc"; }
                case SortBy.TypeAsc: { return "Type Asc"; }
                case SortBy.TypeDesc: { return "Type Desc"; }
                default: { throw new ApplicationException($"Unsupported value in param sortBy={sortBy}"); }
            }
        }
        public string GetSortIcon(SortBy sortBy)
        {
            switch (sortBy)
            {
                case SortBy.CreatedAsc: { return Icons.Material.Filled.ArrowDownward; }
                case SortBy.CreatedDesc: { return Icons.Material.Filled.ArrowUpward; }
                case SortBy.NameAsc: { return Icons.Material.Filled.ArrowDownward; }
                case SortBy.NameDesc: { return Icons.Material.Filled.ArrowUpward; }
                case SortBy.TypeAsc: { return Icons.Material.Filled.ArrowDownward; }
                case SortBy.TypeDesc: { return Icons.Material.Filled.ArrowUpward; }
                default: { throw new ApplicationException($"Unsupported value in param sortBy={sortBy}"); }
            }
        }


        public void ShowProcessing()
        {
            LogInfo("ShowProcessing");
            ShowMessageWindow(String.Empty);
            ShowMessage("Processing ...");
        }

        public void HideProcessing()
        {
            HideMessageWindow();
            LogInfo("HideProcessing");
        }

        public bool PerformCancelQrCodePolling()
        {
            return Context.CancelQrCodePolling();
        }

        public async Task PerformDataMigration(string folderPath)
        {
            AsymKeyItemProvider.Folder = folderPath;
            await ShowErrorAsync("Deprecated - Use Export/Import instead of Migration");
        }

        public async Task PerformReloadAsync(string folderPath)
        {
            AsymKeyItemProvider.Folder = folderPath;

            ShowMessageWindow(String.Empty);
            ShowMessage("reloading ...");

            // init with values from previous user session (saved in the file on app exit)
            var keywords = State?.PersistentState?.SearchKeywords ?? string.Empty;
            var sortBy = (SortBy)(State?.PersistentState?.SortOrder ?? (int)SortBy.NameAsc);
            
            Context.Folder = await Context.FolderOpen(folderPath, keywords, sortBy, AppRootPath);

            if (Context.Folder.IsError)
            {
                var errorToDisplay = Context.Folder.ErrorMessage;
                Context.Folder.ErrorMessage = string.Empty;
                await ShowErrorAsync(errorToDisplay);
                //Context.UI.ShowError(Context.Folder.ErrorMessage);
            }


            SelectFirstItem();



            HideMessageWindow();

            AppRoot.StateChanged(); // this is not enough b/c we do NOT use [Parameter]
            Vm.MainScreenVm.StateChangedCascade();

        }


        public async Task PerformNewItemShowClick()
        {
            var rit = new ResourceItem();
            rit.PrepareObjects(); 

            rit.ResourceInfo.IsNew = true;
            rit.ResourceInfo.Id = System.Guid.NewGuid().ToString();
            rit.ResourceInfo.Type = "web";

            Vm.MainScreenVm.DetailsVm.DataModelSet(rit, EditMode.New);

        }

        public async Task PerformItemSaveClick()
        {
            LogDebug("============= PerformItemSaveClick() - START ======================================================");
            var oldMode = Vm.MainScreenVm.DetailsVm.Mode;
            LogDebug($"oldMode={oldMode}");
            Vm.MainScreenVm.DetailsVm.SaveIntoDataModel();

            var saveResult = await Context.ResourceSave(Context.Folder, Vm.MainScreenVm.DetailsVm.DataModel.ResourceInfo);

            if (saveResult.IsError())
            {
                await ShowErrorAsync(saveResult.Message);
                Vm.MainScreenVm.DetailsVm.Mode = oldMode;
                return;
            }

            var insertedIndex = Context.Folder.Searched.SaveResourceItem(Vm.MainScreenVm.DetailsVm.DataModel);

            Vm.MainScreenVm.DetailsVm.Mode = ViewModels.EditMode.View;
            //Vm.DetailsVm.View.MessageDetails.StateChangedCascade();

            if (insertedIndex >= 0)
            {
                var insertedSi = Context.Folder.Searched.Items[insertedIndex];
                SetSelectedItem(insertedSi);
            }

            Vm.MainScreenVm.StateChangedCascade();

        }

        public async Task PerformItemCancelClick()
        {
            Vm.MainScreenVm.DetailsVm.Cancel();

            Vm.MainScreenVm.DetailsVm.Mode = ViewModels.EditMode.View;
            Vm.MainScreenVm.DetailsVm.StateChangedCascade();
        }

        public async Task PerformItemDeleteClick(ResourceInfo ri)
        {
            // ask user for confirmation
            var resourceName = ri?.Name;
            var confirmation = await ShowConfirmationAsync($"Delete '{resourceName}'?");
            if (!confirmation) { return; }

            // find next item to select after deleting this item
            var nextSi = Context.Folder.Searched.NextOrPreviousSearchedItem(ri?.Id);

            // delete
            await Context.ResourceDelete(Context.Folder, ri);

            // set next item as current
            // setting to null is also supported, when we have no more items (just deleted last visible item)
            if (nextSi?.ResourceItem != null)
                Vm.MainScreenVm.DetailsVm.DataModelSet(nextSi.ResourceItem);
            else
                // null - make it explicit
                Vm.MainScreenVm.DetailsVm.DataModelSet(null);


            Vm.MainScreenVm.StateChangedCascade();
        }


        public async Task PerformItemEditClick()
        {

            Vm.MainScreenVm.DetailsVm.Edit();
            Vm.MainScreenVm.DetailsVm.SecretVm.Edit();

            Vm.MainScreenVm.DetailsVm.StateChangedCascade();
        }

        public async Task DoSortClick(SortBy sortBy)
        {
            Context.Folder.Searched.Criteria.SortBy = sortBy;
            SortRefreshLayout();
        }

        public void SortRefreshLayout()
        {
            Context.Folder.Searched.Sort();
            SelectFirstItem();
            Vm.MainScreenVm.View.StateChangedCascade();

        }

        public async Task DoSearchClick()
        {
            //Context.Folder.Searched.Criteria.Keywords = searchStr;
            SearchSortRefreshLayout();
        }
        public async Task DoSearchCancelClick()
        {
            //Context.Folder.Searched.Criteria.Keywords = String.Empty;
            SearchSortRefreshLayout();
        }

        public void SearchSortRefreshLayout()
        {
            Context.Folder.Searched.SearchAndSort();
            SelectFirstItem();
            Vm.MainScreenVm.View.StateChangedCascade();
        }

        public /*async Task*/ void DoSetMessageListWidth(float newWidth)
        {
            if (newWidth == State.LeftMenuWidth) return;

            State.LeftMenuWidth = newWidth;

            // todo: DO we do any UNECESSARY RENDERINGS here ????
            // todo: Need to put counts of calls to events / methods to identify unnecessary renderings if any

            Vm.MainScreenVm.View.StateChanged();

            LogInfo($"DoSetMessageListWidth - after calling ResourceBoxVm.View.StateChangedCascade()");

            // the logical execution flow continues at ColumnsHidden.OnAfterRender() <-- check that method now
        }

        public async Task<Attachment> GetAttachmentAsync(IBrowserFile file)
        {
            var a = new Attachment();
            a.Header = new AttachmentHeader();
            a.Body = new AttachmentBody();

            a.Header.FileName = file.Name;
            a.Header.FileSize = Convert.ToInt32(file.Size);

            using (var s = file.OpenReadStream(1024000))
            {
                a.Body.Content = await ByteString.FromStreamAsync(s);
            };

            return a;
        }

        public Attachment GetAttachmentWithEmptyBody(IBrowserFile file)
        {
            var a = new Attachment();
            a.Header = new AttachmentHeader();
            a.Body = new AttachmentBody();

            a.Header.FileName = file.Name;
            a.Header.FileSize = Convert.ToInt32(file.Size);

            //using (var s = file.OpenReadStream(1024000))
            //{
            //    a.Body.Content = await ByteString.FromStreamAsync(s);
            //};

            return a;
        }

        #region Persistent State
        public string PersistentStateFilePath => Path.Combine(AppRootPath, "PersistentState.json");
        public async Task SavePersistentState()
        {
            // before saving into file, init with values from the screen
            if (Context?.Folder?.Searched?.Criteria != null)
            {
                State.PersistentState.SearchKeywords = Context.Folder.Searched.Criteria.Keywords;
                State.PersistentState.SortOrder = (int)Context.Folder.Searched.Criteria.SortBy;
            }
            
            // save into file
            var json = System.Text.Json.JsonSerializer.Serialize(State.PersistentState);
            await File.WriteAllTextAsync(PersistentStateFilePath, json);
        }

        public async Task LoadPersistentState()
        {
            if (!File.Exists(PersistentStateFilePath)) { return; }

            var json = await File.ReadAllTextAsync(PersistentStateFilePath);
            State.PersistentState = System.Text.Json.JsonSerializer.Deserialize<PersistentState>(json);
        }

        #endregion

        #region Settings

        public static string ENVIRONMENT_NAME = "Dev4"; //"Dev4"; //"Prod"; //"DEMO"
        public Settings Settings { get; set; }
        public string AppRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QrsPasswordManager", ENVIRONMENT_NAME);
        public string SettingsFilePath => Path.Combine(AppRootPath,"Settings.json");
        public string DefaultDataPath => Path.Combine(AppRootPath, "main");
        public string CookieFolderPath => AppRootPath;

        public async Task SaveSettings()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Settings);
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }

        public async Task LoadSettings()
        {
            if (!File.Exists(SettingsFilePath)) 
            {
                Settings = null;
                return; 
            }

            var json = await File.ReadAllTextAsync(SettingsFilePath);
            Settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
            Settings.IsConfirmed = true;
        }

        public async Task LoadDefaultSettings()
        {
            Settings = new Settings();
            Settings.DataPath = DefaultDataPath;
            Settings.TunnelServerDomain = ""; // todo: change to "qrid.com"
            Settings.TunnelServerUri = "https://qrid.qrid.com/qrs-tunnel-api/"; // todo: change to ""
            Settings.IsConfirmed = false;
            
        }

        #endregion

        #region Backup / Restore

        public List<BackupOption> GetBackupOptions()
        {
            var all = Settings?.BackupFolders?.Split(';') ?? new string[] { };
            var selected = Settings?.SelectedBackupFolders?.Split(';') ?? new string[] { };
            var res = new List<BackupOption>();

            foreach(var backup in all)
            {
                var bo = new BackupOption();
                bo.BackupFolder = backup;
                bo.IsSelected = selected.Contains(backup); // should work with "string"
                res.Add(bo);
            }
            return res;
        }

        public List<BackupOption> GetRestoreOptions()
        {
            var res = new List<BackupOption>();
            var folders = GetBackupOptions();

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder.BackupFolder)) 
                    continue;

                // get all dirs in a folder
                var backupFolders = Directory.GetDirectories(folder.BackupFolder);

                foreach (var backupFolder in backupFolders)
                {
                    var bo = new BackupOption();
                    bo.BackupFolder = backupFolder;
                    bo.IsSelected = false;
                    res.Add(bo);
                }
            }
            // sort DESCENDING by BackupFolder string (it ends with the properly sortable datetime string)
            res.Sort((x, y) => y.BackupFolder.CompareTo(y.BackupFolder)); 
            return res;
        }

        public void SetBackupOptions(List<BackupOption> backupOptions)
        {
            Settings.BackupFolders = string.Join(';', backupOptions.Select(s => s.BackupFolder).ToArray());
            Settings.SelectedBackupFolders = string.Join(';', backupOptions.Where(s => s.IsSelected).Select(s => s.BackupFolder).ToArray());
        }

        public void PerformBackupToAllDestinations(List<BackupOption> backupOptions)
        {
            backupOptions.ForEach(bo => bo.ResultMessage = string.Empty);

            var folderName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var selected = backupOptions.Where(s => s.IsSelected).ToList();
            var res = true;
            foreach(var bo in selected)
            {
                PerformBackup(bo, folderName);
            }
        }

        public void PerformBackup(BackupOption bo, string folderName)
        {
            bo.IsError = true;
            if (!Directory.Exists(bo.BackupFolder))
            {
                // if BackupOption includes path to USB drive then it is a legit case when the USB drive is unplugged
                bo.ResultMessage = $"Skipped this backup destination b/c folder is not available: '{bo.BackupFolder}' ";
                return;
            }
            var backupFolder = Path.Combine(bo.BackupFolder, folderName);
            if (Directory.Exists(backupFolder)) 
            {
                // should never happen b/c we use time up to the second to generate folder name
                bo.ResultMessage = $"Skipped this backup destination to avoid overwriting already existing folder: '{backupFolder}'";
                return;
            }

            // COPY DATA DIRECTORY into BACKUP FOLDER
            // recursively with subfolders but for now we don't have any subfolders just files

            try
            {
                // !!! it will also CREATE backupFolder if required
                FileUtils.CopyDirectory(Settings.DataPath, backupFolder, true);
            }
            catch (Exception ex)
            {
                bo.ResultMessage = $"ERROR while backing up to folder '{backupFolder}' - Error Details:  " + ex.Message;
            }

            bo.IsError = false;
            bo.ResultMessage = $"SUCCESSFULLY backed up to folder '{backupFolder}'";
            return;
        }

        public void GetBackupResults(List<BackupOption> backupOptions, out string result, out string resultDetails )
        {
            var selected = backupOptions.Where(s => s.IsSelected).ToList();
            var successCount = 0;
            var errorCount = 0;
            var totalCount = 0;
            var sb = new StringBuilder();
            sb.AppendLine("<ul>");
            foreach (var bo in selected)
            {
                successCount += bo.IsError ? 0 : 1;
                errorCount += bo.IsError ? 1 : 0;
                totalCount += 1;
                //
                sb.Append("<li>");
                sb.Append(bo.IsError ? "ERROR - " : "SUCCESS - ");
                sb.Append(bo.ResultMessage);
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul>");
            resultDetails = sb.ToString();
            if (successCount > 0)
            {
                result = errorCount > 0 ? "PS" : "S";
            }
            else
            {
                result = "E";
            }
        }


        public string PerformRestore(string backupPath)
        {
            // this is scary

            if (!Directory.Exists(backupPath))
            {
                return $"Cannot restore from non-existing folder: '{backupPath}'";
            }

            var fileCount = Directory.EnumerateFiles(backupPath, "*.qrs?", SearchOption.TopDirectoryOnly).Count();
            
            if (fileCount <= 0)
            {
                return $"Cannot restore from a folder that does NOT contain any *.qrs1 or *.qrs2 files: '{backupPath}'";
            }

            // RESTORE ...
            
            // delete first - AAAAAAAAAAAAAAAAAH
            Directory.Delete(Settings.DataPath, true);

            // re-create by copying from Backup
            try
            {
                // !!! this will also CREATE Settings.DataPath Folder 
                FileUtils.CopyDirectory(backupPath, Settings.DataPath, true);
            }
            catch (Exception ex)
            {
                return "Backup Error - " + ex.Message;
            }

            return string.Empty; // if string.Empty then OK
        }

        #endregion

        public async Task DoOpenAllSecrets()
        {
            var response = await Context.ResourceOpenAllSecrets(Context.Folder);
            if (response.IsError())
                await ShowMessageBoxAsync("Error", response.Message);
            else
                // TODO - make sure that special characters in password are not getting altered b/c of html while being displayed:
                await ShowMessageBoxAsync("Success", $"Successfully opened all secrets");
            // TODO - refresh the current views?
        }


        #region Get Input

        private TaskCompletionSource<string> continueWithUserInputTask;

        public void ContinueWithUserInput(string userInput)
        {
            continueWithUserInputTask.TrySetResult(userInput);
        }

        public void LogDebug(string message)
        {
            Context.LogDebug(message);
        }

        public async Task<HttpResponseModel<StringResponse>> GetUserInputAsync(string inputContext)
        {
            LogDebug("GetUserInputAsync - start");
            continueWithUserInputTask = new TaskCompletionSource<string>();

            // show User Input part of the screen
            if (QrCommandDialog == null)
                throw new ApplicationException("GetUserInputAsync is called when QrCommandDialog is not open! This is a bug");
            QrCommandDialog.StartUserInputMode();

            LogDebug("GetUserInputAsync - after changing DisplayMode");

            // 1.1. wait until callback with Username
            await continueWithUserInputTask.Task;

            LogDebug("GetUserInputAsync - await completed");

            if (continueWithUserInputTask.Task.IsCompletedSuccessfully)
                return CommonContext.SuccessStringResponse(continueWithUserInputTask.Task.Result);
            else
                return CommonContext.ErrorStringResponse("Error while waiting for User Input to be provided");
        }



        #endregion
    }
}
