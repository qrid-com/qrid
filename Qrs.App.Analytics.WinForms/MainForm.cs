using Qrid.Common.Client.Cookie;
using Qrs.App.Analytics.Client;
using Qrs.App.Analytics.Client.Models;
using System.ComponentModel;

namespace Qrs.App.Analytics.WinForms
{
    public partial class MainForm : Form
    {

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AnalyticsService Svc { get; set; }

        public TabControl TabCtrl => this.tabControlMain;
        public TabContent CurrentTab => (TabContent)tabControlMain.SelectedTab.Controls[0];

        public MainForm()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            if (Svc == null)
                await InitSvc();
        }

        private async Task InitSvc()
        {
            timer1.Enabled = false;

            Svc = new AnalyticsService();
            await Svc.InitSettingsAsync();

            txtSettingsFile.Text = Svc.SettingsFilePath;
            tabControlMain.TabPages.Clear();


            var cookieProvider = new FileCookieProvider(Svc.CookieFolderPath);

            foreach (var ctxSettings in Svc.Settings.ContextSettings)
            {
                // TabContent - UserControl implementing TabPage and IAnalyticLogger interface
                var tabContent = AddNewTab(ctxSettings);
                var ctx = new AnalyticsContext(ctxSettings, cookieProvider, tabContent);
                ctx.Init(); // if db file is missing, init sqlite db and create table 
                Svc.Contexts.Add(ctx);
                // do we need to expose ctx to TabContent?
            }

            timer1.Interval = Svc.Settings.TimerPeriod * 1000;
            timer1.Enabled = true;
        }


        private TabContent AddNewTab(ContextSettings ctxSettings)
        {
            var ucTab = new TabContent();
            ucTab.Init(ctxSettings, this); // this will also add this as new tab
            return ucTab;
        }


        public async void Timer_Tick(object sender, EventArgs e)
        {
            await Svc.UpdateAllContexts();
        }

        public void AddLog(string message)
        {
            rtbLog.AppendText($"{DateTime.Now.ToString("HH:mm:ss")} - {message}\n");
        }

        private void chkTimerEnabled_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = chkTimerEnabled.Checked;
        }

        private async void btnUpdateLogs_Click(object sender, EventArgs e)
        {
            await Svc.UpdateAllContexts();
        }
    }
}
