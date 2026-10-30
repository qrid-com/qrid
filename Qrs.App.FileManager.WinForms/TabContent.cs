using Qrs.App.FileManager.Client.Payload;
using System.Windows.Forms;

namespace Qrs.App.FileManager.WinForms
{
    public partial class TabContent : UserControl
    {

        public MainForm MF; //=> (MainForm)ParentForm;
        public TabPage TP; // => (TabPage)Parent;

        //public QrsFileInfo QFI;

        // new
        public QrsFileWrapper QFW;


        public TabContent()
        {
            InitializeComponent();
        }

        public void LoadContentToQFI()
        {
            QFW.Content = txtContent.Text;
        }

        public void Init(QrsFileWrapper efi, MainForm mf)
        {
            MF = mf;
            QFW = efi;

            TP = new TabPage(QFW.FileName);
            TP.Controls.Add(this);
            MF.TabCtrl.TabPages.Add(TP);
            
            
            txtContent.Text = QFW.Content;
            Dock = DockStyle.Fill;

            MF.TabCtrl.SelectedTab = TP;
        }

        public void Save()
        {
            // // should be called only when QFI.File contains path already
            
            if (string.IsNullOrEmpty(QFW.File))
            {
                // show file dialog to pick the file name
            }

            MF.FmContext.FileSave(QFW);
        }

    }
}
