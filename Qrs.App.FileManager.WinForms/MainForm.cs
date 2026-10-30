using Google.Protobuf.WellKnownTypes;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.Tunnel.Client;
using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Qrs.App.FileManager.WinForms
{
    public partial class MainForm : QrsFormBase, IUserInteraction
    {
        public string AppRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QrsFileManager", ""); // ENV_NAME
        public string CookieFolderPath => AppRootPath;


        QrCodeWindow qrWindow = new QrCodeWindow();
        public string AppName = "FILE MANAGER";
        public TabControl TabCtrl => this.tabControlMain;
        public TabContent CurrentTab => (TabContent)tabControlMain.SelectedTab.Controls[0];
        private int nextNew = 1;

        public MainForm()
        {
            InitializeComponent();
        }

        private async Task<HttpResponseModel<StringResponse>> InitFromConfigAsync()
        {
            var tunnelServiceDomain = ConfigurationManager.AppSettings["TunnelServiceDomain"];
            var tunnelServiceUriTemplate = ConfigurationManager.AppSettings["TunnelServiceUriTemplate"];

            // QrsContext - init using Config Params 
            var initResponse = await QrsContext.Instance.InitFromConfigAsync(tunnelServiceDomain, tunnelServiceUriTemplate);
            return initResponse;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // create app root folder, but only if it does not exist yet
            Directory.CreateDirectory(AppRootPath);

            var asymKeyItemProvider = new FileAsymKeyItemProvider(CookieFolderPath);
            var cookieProvider = new FileCookieProvider(CookieFolderPath);

            QrsContext.Instance = new Qrs.Client.Context.ClientContext(AppName, this, asymKeyItemProvider, cookieProvider);
            var initResponse = await InitFromConfigAsync(); // domain discovery or URI template
            if (!initResponse.IsSuccess())
            {
                // discovery failed, fatal error, cannot continue, exit
                MessageBox.Show($"ERROR - {initResponse.Message}", "ERROR");
                Application.Exit();
            }
            QrsContext.FmInstance = new Qrs.App.FileManager.Client.FmContext(QrsContext.Instance);

            slblTunnelServiceUri.Text = "End-To-End Encrypted Tunnel Server: " + (QrsContext.Instance.TunnelClient?.BaseUrl ?? "not initialized");
            tabControlMain.TabPages.Clear();
            FileNew_Click(null, null);
        }

        #region IUserInteraction
        public void ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.HideQrCodeWindow(string sessionId)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_HideQrCodeWindow(sessionId);
                }
            );

        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.BeginInvoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowQrCodeWindow(sessionId, qrString);
                }
            );

        }

        async Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.BeginInvoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowQrCodeWindow(sessionId, qrString);
                }
            );

        }

        void IUserInteraction.ShowError(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowMessage(message, true);
                }
            );
        }

        void IUserInteraction.ShowMessage(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowMessage(message, false);
                }
            );
        }

        public void Invoked_ShowMessage(string message, bool isError)
        {
            LogDebug($"Start - Invoked_ShowMessage({message})");
            // invoked from UI thread
            MessageBox.Show(
                message,
                isError ? "ERROR" : "Message",
                MessageBoxButtons.OK,
                isError ? MessageBoxIcon.Error : MessageBoxIcon.None
                );
            LogDebug($"End   - Invoked_ShowMessage");
        }



        public void Invoked_ShowQrCodeWindow(string sessionId, string qrString)
        {
            // invoked from UI thread

            //var res = qrWindow.ShowDialog(this);

            LogDebug($"Start - Invoked_ShowQrCodeWindow({qrString})");
            if (qrWindow == null)
                qrWindow = new QrCodeWindow();

            qrWindow.ShowQrCodeWindow(qrString);
            LogDebug($"End   - Invoked_ShowQrCodeWindow");
        }

        public void Invoked_HideQrCodeWindow(string sessionId)
        {
            // invoked from UI thread

            LogDebug($"Start - Invoked_HideQrCodeWindow");
            qrWindow.Hide();
            LogDebug($"End   - Invoked_HideQrCodeWindow");
        }

        void IUserInteraction.SetDescription(string title, string description)
        {
            throw new ApplicationException($"Unexpected call to UI.SetDescription(title='{title}', description='{description}')");
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            LogDebug($"called UI.SetTunnelServiceUri(tunnelServiceUri='{tunnelServiceUri}')");
        }

        #endregion




        private bool ContainsFileName(string fileName)
        {
            return tabControlMain.TabPages.Cast<TabPage>().Where(tp => (tp.Controls.Count > 0) && ((TabContent)tp.Controls[0]).QFW.FileName == fileName).Any();
        }
        private string GetNewFileName()
        {
            while (ContainsFileName($"new{nextNew}.qrs"))
            {
                nextNew++;
            }
            return $"new{nextNew}.qrs";
        }
        private void FileNew_Click(object sender, EventArgs e)
        {
            var efi = FmContext.FileNew(GetNewFileName());

            var ucTab = new TabContent();
            ucTab.Init(efi, this); // this will also add this as new tab:
        }

        private async void FileOpen_Click(object sender, EventArgs e)
        {

            openFileDialog1.Filter = "Encrypted QRS files (*.qrs)|*.qrs";

            // show file dialog to pick the file name
            if (DialogResult.OK != openFileDialog1.ShowDialog())
                // user decided to Cancel, do nothing
                return;

            var efi = await FmContext.FileOpen(openFileDialog1.FileName);

            var ucTab = new TabContent();
            ucTab.Init(efi, this); // this will also add this as new tab:
        }

        private void FileClose_Click(object sender, EventArgs e)
        {

        }

        private async Task FileSave(bool selectFileName = false)
        {
            if (selectFileName)
            {
                // show file dialog to pick the file name
                saveFileDialog1.FileName = CurrentTab.QFW.FileName;
                if (DialogResult.OK != saveFileDialog1.ShowDialog())
                    // user decided to Cancel, do nothing
                    return;

                CurrentTab.QFW.File = saveFileDialog1.FileName;
                var fi = new System.IO.FileInfo(CurrentTab.QFW.File);
                CurrentTab.QFW.FileName = fi.Name;
            }

            CurrentTab.LoadContentToQFI();
            await FmContext.FileSave(CurrentTab.QFW);
            CurrentTab.TP.Text = CurrentTab.QFW.FileName;
        }
        private async void FileSave_Click(object sender, EventArgs e)
        {
            var needFileName = string.IsNullOrEmpty(CurrentTab.QFW.File);
            await FileSave(needFileName);
        }

        private async void FileSaveAs_Click(object sender, EventArgs e)
        {
            await FileSave(true);
        }

        private void Exit_Click(object sender, EventArgs e)
        {

        }

        private TabPage GetClickedTab(MouseEventArgs e)
        {
            for (int i = 0; i < tabControlMain.TabCount; ++i)
            {
                if (tabControlMain.GetTabRect(i).Contains(e.Location))
                {
                    return tabControlMain.TabPages[i];
                }
            }
            return null;
        }

        private void tabControlMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var tp = GetClickedTab(e);

            if (tp != null && tp != tabControlMain.SelectedTab)
            {
                tabControlMain.SelectedTab = tp;
            }

        }

        private void closeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // closing the tab
            LogDebug($"CurrentTab: {tabControlMain.SelectedTab.Text}");
            TabPage next = null;
            if (tabControlMain.TabPages.Count > 1)
            {
                if (tabControlMain.SelectedIndex + 1 == tabControlMain.TabPages.Count)
                    next = tabControlMain.TabPages[tabControlMain.SelectedIndex - 1];
                else
                    next = tabControlMain.TabPages[tabControlMain.SelectedIndex + 1];
            }

            tabControlMain.TabPages.RemoveAt(tabControlMain.SelectedIndex);
            if (next != null)
            {
                tabControlMain.SelectedTab = next;
            }
            
        }

        public void ShowMessageWindow(string title)
        {
            throw new NotImplementedException();
        }

        public void HideMessageWindow()
        {
            throw new NotImplementedException();
        }

        private void slblFile_Click(object sender, EventArgs e)
        {

        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            var pairingCode = InputDialog.Show("Enter the 3 characters Pairing Code from mobile app:", "Pairing");
            if (string.IsNullOrWhiteSpace(pairingCode))
                return CommonContext.ErrorStringResponse("Pairing has been cancelled by user");
            else
                return CommonContext.SuccessStringResponse(pairingCode);
        }

        // How to draw [X] on a Tab and close Tab by clicking on [X]
        // https://stackoverflow.com/questions/3183352/close-button-in-tabcontrol/36070187#36070187
        //
    }
}
