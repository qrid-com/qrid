using Qrs.App.Analytics.Client;
using Qrs.App.Analytics.Client.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Qrs.App.Analytics.WinForms
{
    public partial class TabContent : UserControl, IAnalyticLogger
    {
        public MainForm MF; 
        public TabPage TP; 
        public ContextSettings Settings;

        public TabContent()
        {
            InitializeComponent();
        }

        public void Init(ContextSettings settings, MainForm mf)
        {
            MF = mf;
            Settings = settings;

            TP = new TabPage(Settings.Name);
            TP.Controls.Add(this);
            MF.TabCtrl.TabPages.Add(TP);
            
            txtContent.Text = string.Empty;
            Dock = DockStyle.Fill;

            MF.TabCtrl.SelectedTab = TP;
        }

        void IAnalyticLogger.Log(string message)
        {
            txtContent.AppendText($"{DateTime.Now.ToString("HH:mm:ss")} - ");
            txtContent.AppendText(message);
            txtContent.AppendText(Environment.NewLine);
        }
    }
}
