namespace Qrs.App.Vault.Client
{
    // Output format chosen in the export dialog.
    public enum ExportFormat
    {
        // A single password-encrypted (AES-256) standard ZIP archive containing all selected files.
        Zip,

        // One OpenPGP passphrase-encrypted (.gpg) file per selected file.
        Gpg
    }
}
