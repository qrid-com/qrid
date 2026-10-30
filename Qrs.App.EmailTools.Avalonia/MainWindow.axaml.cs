using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Qrs.App.EmailTools.Avalonia.Models;
using Qrs.App.EmailTools.Avalonia.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace Qrs.App.EmailTools.Avalonia;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<EmailAccount> _accounts = [];
    private readonly ObservableCollection<EmailFolder> _folders = [];
    private readonly ObservableCollection<EmailMessageSummary> _messages = [];
    private readonly ObservableCollection<DeletionSummaryRow> _deletionSummary = [];
    private readonly ObservableCollection<DeletionMessageRow> _deletionMessages = [];
    private readonly AccountStore _accountStore = new();
    private readonly OAuthTokenService _oauthService;
    private readonly MailTransportService _mailTransport;
    private readonly MailSyncService _syncService;
    private readonly MailDeletionService _deletionService;
    private MailDatabase? _mailDatabase;
    private bool _isLoadingAccount;
    private string _deletionSortColumn = "Date";
    private bool _deletionSortAscending;

    public MainWindow()
    {
        InitializeComponent();

        _oauthService = new OAuthTokenService(OAuthSettings.Load())
        {
            // Persist refreshed OAuth tokens the same way account settings are stored.
            PersistAccountAsync = async (_, _) => await _accountStore.SaveAsync(_accounts)
        };
        _mailTransport = new MailTransportService(_oauthService);
        _syncService = new MailSyncService(_mailTransport);
        _deletionService = new MailDeletionService(_mailTransport);

        cmbProvider.ItemsSource = Enum.GetValues<EmailProvider>();
        lstAccounts.ItemsSource = _accounts;
        lstFolders.ItemsSource = _folders;
        gridMessages.ItemsSource = _messages;
        gridDeletionSummary.ItemsSource = _deletionSummary;
        gridDeletionMessages.ItemsSource = _deletionMessages;

        Closed += async (_, _) =>
        {
            if (_mailDatabase != null)
                await _mailDatabase.DisposeAsync();
        };
    }

    private async void MainWindow_Opened(object? sender, EventArgs e)
    {
        var accounts = await _accountStore.LoadAsync();
        foreach (var account in accounts)
            _accounts.Add(account);

        if (_accounts.Count == 0)
            _accounts.Add(CreateDefaultAccount());

        lstAccounts.SelectedIndex = 0;
    }

    private void lstAccounts_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        LoadAccountForm(SelectedAccount);
        _folders.Clear();
        _messages.Clear();
        txtMessageDetails.Text = string.Empty;
    }

    private void cmbProvider_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingAccount || cmbProvider.SelectedItem is not EmailProvider provider)
            return;

        var account = ReadAccountForm();
        account.Provider = provider;
        account.ApplyProviderDefaults();
        LoadAccountForm(account);
    }

    private void btnAddAccount_Click(object? sender, RoutedEventArgs e)
    {
        var account = CreateDefaultAccount();
        _accounts.Add(account);
        lstAccounts.SelectedItem = account;
    }

    private async void btnRemoveAccount_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectedAccount == null)
            return;

        _accounts.Remove(SelectedAccount);
        await _accountStore.SaveAsync(_accounts);
        if (_accounts.Count == 0)
            _accounts.Add(CreateDefaultAccount());
        lstAccounts.SelectedIndex = 0;
    }

    private async void btnSaveAccounts_Click(object? sender, RoutedEventArgs e)
    {
        ApplyAccountFormToSelected();
        await _accountStore.SaveAsync(_accounts);
        SetStatus("Account settings saved. Passwords are intentionally not saved.");
    }

    private async void btnSyncAll_Click(object? sender, RoutedEventArgs e)
    {
        await RunMailAction(async () =>
        {
            ApplyAccountFormToSelected();
            await _accountStore.SaveAsync(_accounts);

            var database = await GetDatabaseAsync();
            var progress = new Progress<string>(SetStatus);
            var saved = await _syncService.SyncAllAsync(_accounts, database, progress);
            SetStatus($"Sync complete. {saved} new message(s) stored in the encrypted local database.");
        });
    }

    // App data folder shared with AccountStore: %AppData%/QRID/Mail.
    private static string AppDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QRID", "Mail");

    private async Task<MailDatabase> GetDatabaseAsync()
    {
        if (_mailDatabase != null)
            return _mailDatabase;

        // Unlock (or, on first run, create and QRS-wrap) the random SQLCipher key via the QRS Mobile App.
        var keyStore = new MailDbKeyStore(AppDataFolder, new Qrs.UI.Avalonia.UIProvider());
        var key = await keyStore.LoadOrCreateKeyAsync();

        var database = new MailDatabase(Path.Combine(AppDataFolder, "mail.db"), key);
        await database.OpenAsync();
        _mailDatabase = database;
        return database;
    }

    private void btnApplyAccount_Click(object? sender, RoutedEventArgs e)
    {
        ApplyAccountFormToSelected();
        SetStatus("Account settings applied.");
    }

    private async void btnLoadFolders_Click(object? sender, RoutedEventArgs e)
    {
        await RunMailAction(async () =>
        {
            var account = RequireSelectedAccount();
            ApplyAccountFormToSelected();
            _folders.Clear();
            _messages.Clear();
            txtMessageDetails.Text = string.Empty;
            foreach (var folder in await _mailTransport.GetFoldersAsync(account))
                _folders.Add(folder);
            if (_folders.Count > 0)
                lstFolders.SelectedIndex = 0;
            SetStatus($"Loaded {_folders.Count} folders for {account.EmailAddress}.");
        });
    }

    private async void lstFolders_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        await LoadSelectedFolderAsync();
    }

    private async void gridMessages_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        await RunMailAction(async () =>
        {
            var account = RequireSelectedAccount();
            var folder = RequireSelectedFolder();
            if (gridMessages.SelectedItem is not EmailMessageSummary summary)
                return;

            var details = await _mailTransport.GetMessageAsync(account, folder.FullName, summary.UniqueId);
            txtMessageDetails.Text = FormatMessageDetails(details);
            SetStatus($"Loaded message: {details.Subject}");
        });
    }

    private async void btnSend_Click(object? sender, RoutedEventArgs e)
    {
        await RunMailAction(async () =>
        {
            var account = RequireSelectedAccount();
            ApplyAccountFormToSelected();
            await _mailTransport.SendAsync(account, txtComposeTo.Text ?? string.Empty, txtComposeSubject.Text ?? string.Empty, txtComposeBody.Text ?? string.Empty);
            txtComposeTo.Text = string.Empty;
            txtComposeSubject.Text = string.Empty;
            txtComposeBody.Text = string.Empty;
            SetStatus("Message sent.");
        });
    }

    // --- Mark messages for deletion (from the Mailbox view) ---

    private async void btnDeleteFromServer_Click(object? sender, RoutedEventArgs e)
    {
        await MarkSelectedForDeletionAsync(alsoDeleteFromClient: false);
    }

    private async void btnDelete_Click(object? sender, RoutedEventArgs e)
    {
        await MarkSelectedForDeletionAsync(alsoDeleteFromClient: true);
    }

    private async Task MarkSelectedForDeletionAsync(bool alsoDeleteFromClient)
    {
        await RunMailAction(async () =>
        {
            var account = RequireSelectedAccount();
            var folder = RequireSelectedFolder();
            var selected = gridMessages.SelectedItems?.OfType<EmailMessageSummary>().ToList() ?? [];
            if (selected.Count == 0)
            {
                SetStatus("Select one or more messages to delete.");
                return;
            }

            var database = await GetDatabaseAsync();
            var marked = 0;
            var missing = 0;

            foreach (var summary in selected)
            {
                var rowId = await database.FindMessageRowIdAsync(account.AccountId, folder.FullName, summary.UniqueId);
                if (rowId is null)
                {
                    missing++;
                    continue;
                }

                await database.AddToMessageListAsync(rowId.Value, MessageListType.DeleteFromServer);
                if (alsoDeleteFromClient)
                    await database.AddToMessageListAsync(rowId.Value, MessageListType.DeleteFromClient);
                marked++;
            }

            var missingHint = missing > 0 ? $" {missing} message(s) are not in the local database yet — sync first." : string.Empty;
            SetStatus($"Queued {marked} message(s) for deletion.{missingHint}");
            await RefreshDeletionAsync();
        });
    }

    // --- Review and Delete tab ---

    private async void btnRefreshDeletion_Click(object? sender, RoutedEventArgs e)
    {
        await RunMailAction(RefreshDeletionAsync);
    }

    private async Task RefreshDeletionAsync()
    {
        var database = await GetDatabaseAsync();

        _deletionSummary.Clear();
        foreach (var row in await database.GetDeletionSummaryAsync())
            _deletionSummary.Add(row);

        _deletionMessages.Clear();
        foreach (var row in await database.GetDeletionMessagesAsync(_deletionSortColumn, _deletionSortAscending, 1000))
            _deletionMessages.Add(row);
    }

    private async void gridDeletionMessages_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        // Sort the whole queued dataset in the database, then re-take the top 1000.
        e.Handled = true;
        var column = e.Column.Tag as string ?? "Date";
        if (_deletionSortColumn == column)
            _deletionSortAscending = !_deletionSortAscending;
        else
        {
            _deletionSortColumn = column;
            _deletionSortAscending = true;
        }

        await RunMailAction(RefreshDeletionAsync);
    }

    private async void btnRunDeletion_Click(object? sender, RoutedEventArgs e)
    {
        await RunMailAction(async () =>
        {
            var database = await GetDatabaseAsync();
            var accounts = _accounts.ToList();

            var progressWindow = new DeletionProgressWindow(progress =>
                _deletionService.DeleteAsync(accounts, database, progress));
            await progressWindow.ShowDialog(this);

            await RefreshDeletionAsync();
            var result = progressWindow.Result;
            if (result != null)
                SetStatus($"Deleted {result.ServerDeleted} from server, {result.ClientDeleted} from client. {result.Errors} error(s).");
        });
    }

    private async Task LoadSelectedFolderAsync()
    {
        await RunMailAction(async () =>
        {
            var account = RequireSelectedAccount();
            var folder = RequireSelectedFolder();
            _messages.Clear();
            txtMessageDetails.Text = string.Empty;
            foreach (var message in await _mailTransport.GetMessagesAsync(account, folder.FullName))
                _messages.Add(message);
            SetStatus($"Loaded {_messages.Count} messages from {folder.DisplayName}.");
        });
    }

    private EmailAccount? SelectedAccount => lstAccounts.SelectedItem as EmailAccount;

    private EmailFolder? SelectedFolder => lstFolders.SelectedItem as EmailFolder;

    private EmailAccount RequireSelectedAccount()
    {
        return SelectedAccount ?? throw new InvalidOperationException("Select an email account first.");
    }

    private EmailFolder RequireSelectedFolder()
    {
        return SelectedFolder ?? throw new InvalidOperationException("Select a mail folder first.");
    }

    private void ApplyAccountFormToSelected()
    {
        var account = SelectedAccount;
        if (account == null)
            return;

        account.CopyFrom(ReadAccountForm());
        account.ApplyProviderDefaults();
        lstAccounts.ItemsSource = null;
        lstAccounts.ItemsSource = _accounts;
        lstAccounts.SelectedItem = account;
    }

    private EmailAccount ReadAccountForm()
    {
        return new EmailAccount
        {
            Provider = cmbProvider.SelectedItem is EmailProvider provider ? provider : EmailProvider.Custom,
            DisplayName = txtDisplayName.Text?.Trim() ?? string.Empty,
            EmailAddress = txtEmailAddress.Text?.Trim() ?? string.Empty,
            Username = txtUsername.Text?.Trim() ?? string.Empty,
            Password = txtPassword.Text ?? string.Empty,
            IncomingHost = txtIncomingHost.Text?.Trim() ?? string.Empty,
            IncomingPort = Convert.ToInt32(numIncomingPort.Value),
            IncomingUseSsl = chkIncomingSsl.IsChecked == true,
            OutgoingHost = txtOutgoingHost.Text?.Trim() ?? string.Empty,
            OutgoingPort = Convert.ToInt32(numOutgoingPort.Value),
            OutgoingUseStartTls = chkOutgoingStartTls.IsChecked == true
        };
    }

    private void LoadAccountForm(EmailAccount? account)
    {
        if (account == null)
            return;

        _isLoadingAccount = true;
        cmbProvider.SelectedItem = account.Provider;
        txtDisplayName.Text = account.DisplayName;
        txtEmailAddress.Text = account.EmailAddress;
        txtUsername.Text = account.Username;
        txtPassword.Text = account.Password;
        txtIncomingHost.Text = account.IncomingHost;
        numIncomingPort.Value = account.IncomingPort;
        chkIncomingSsl.IsChecked = account.IncomingUseSsl;
        txtOutgoingHost.Text = account.OutgoingHost;
        numOutgoingPort.Value = account.OutgoingPort;
        chkOutgoingStartTls.IsChecked = account.OutgoingUseStartTls;
        _isLoadingAccount = false;
    }

    private static EmailAccount CreateDefaultAccount()
    {
        var account = new EmailAccount
        {
            DisplayName = "New account",
            Provider = EmailProvider.Gmail,
            IncomingPort = 993,
            IncomingUseSsl = true,
            OutgoingPort = 587,
            OutgoingUseStartTls = true
        };
        account.ApplyProviderDefaults();
        return account;
    }

    private async Task RunMailAction(Func<Task> action)
    {
        try
        {
            SetStatus("Working...");
            await action();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            await ShowError("Mail error", ex.Message);
        }
    }

    private void SetStatus(string message)
    {
        txtStatus.Text = message;
    }

    private async Task ShowError(string title, string message)
    {
        var msgBox = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
            title,
            message,
            MsBox.Avalonia.Enums.ButtonEnum.Ok,
            MsBox.Avalonia.Enums.Icon.Error);
        await msgBox.ShowWindowDialogAsync(this);
    }

    private static string FormatMessageDetails(EmailMessageDetails details)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"From: {details.From}");
        builder.AppendLine($"To: {details.To}");
        builder.AppendLine($"Date: {details.Date}");
        builder.AppendLine($"Subject: {details.Subject}");
        builder.AppendLine();
        builder.AppendLine(details.Body);
        return builder.ToString();
    }
}
