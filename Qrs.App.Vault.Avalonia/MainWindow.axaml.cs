using Avalonia.Controls;
using Avalonia.Interactivity;
using Qrs.App.Vault.Avalonia.Controls;
using System;

namespace Qrs.App.Vault.Avalonia
{
    public partial class MainWindow : QrsScrollPage
    {

        public HomePanel HomePanel => homePanel;
        public QrPanel QrPanel => qrPanel;
        public NewVaultPanel NewVaultPanel => newVaultPanel;
        public VaultPanel VaultPanel => vaultPanel;
        public ExtractedPanel ExtractedPanel => extractedPanel;
        public DonePanel DonePanel => donePanel;

        public ProcessingPanel ProcessingPanel => processingPanel;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void HideAll()
        {
            homePanel.IsVisible = false;
            qrPanel.IsVisible = false;
            newVaultPanel.IsVisible = false;
            vaultPanel.IsVisible = false;
            extractedPanel.IsVisible = false;
            donePanel.IsVisible = false;
            processingPanel.IsVisible = false;
        }

        public bool IsAllHidden()
        {
            return !(
                homePanel.IsVisible
                || qrPanel.IsVisible
                || newVaultPanel.IsVisible
                || vaultPanel.IsVisible
                || extractedPanel.IsVisible
                || donePanel.IsVisible
                //|| processingPanel.IsVisible  ProcessingPanel is a special case, we want it to be commented out in this function based on how this f is used 
                );
        }

        private async void Menu_New_Vault_Clicked(object sender, RoutedEventArgs e)
        {
            await App.DoVaultNew();
        }

        private async void Menu_New_InMemory_Vault_Clicked(object sender, RoutedEventArgs e)
        {
            await App.DoVaultNewInMemory();
        }

        private async void Menu_Open_Vault_Clicked(object sender, RoutedEventArgs e)
        {
            await App.DoVaultOpen();
        }

        private void Menu_OpenRecent_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            var recentFiles = Context.Settings?.RecentFiles;
            OpenRecentMenuItem.Items.Clear();

            if (recentFiles == null || recentFiles.Count == 0)
            {
                OpenRecentMenuItem.Items.Add(new MenuItem { Header = "(no recent files)", IsEnabled = false });
                return;
            }

            foreach (var file in recentFiles)
            {
                var item = new MenuItem { Header = System.IO.Path.GetFileName(file) };
                ToolTip.SetTip(item, file);
                item.Click += async (s, args) => await App.DoVaultOpen(file);
                OpenRecentMenuItem.Items.Add(item);
            }
        }

        private async void Menu_Close_Vault_Clicked(object sender, RoutedEventArgs e)
        {
            await App.DoVaultClose();
        }

        private async void Menu_Edit_File_Clicked(object sender, RoutedEventArgs e)
        {
            await VaultPanel.EditSelectedFile();
        }

        private async void Menu_Export_Files_Clicked(object sender, RoutedEventArgs e)
        {
            await App.DoVaultExportSelectedFiles();
        }

        private async void Menu_Add_File_Clicked(object sender, RoutedEventArgs e)
        {
            await App.DoVaultAddNewFile();
        }

        private async void Menu_Extract_Vault_Clicked(object sender, RoutedEventArgs e)
        {
            await App.DoVaultOpenExtractView();
        }

        private void Menu_Goto_Main_Clicked(object sender, RoutedEventArgs e)
        {
            App.DoHome();
        }

        private async void Menu_Exit_Clicked(object sender, RoutedEventArgs e)
        {
            App.Shutdown();
        }

        private async void Menu_Help_About_Clicked(object sender, RoutedEventArgs e)
        {
            _ = Window.Launcher.LaunchUriAsync(new Uri(@"https://qrid.com"));
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await App.Initalize();
        }

        private async void Menu_Test_ProgressDialog_Clicked(object sender, RoutedEventArgs e)
        {
            // not used at the moment
            App.OnStartProcessing(null, null);
        }

        private async void Menu_Test_Clicked(object sender, RoutedEventArgs e)
        {
            // not used at the moment
            App.ShowProcessing();
        }


    }
}