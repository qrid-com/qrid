namespace Qrs.App.EmailTools.Avalonia.Models;

public sealed class EmailFolder
{
    public EmailFolder(string fullName, string displayName)
    {
        FullName = fullName;
        DisplayName = displayName;
    }

    public string FullName { get; }
    public string DisplayName { get; }
}
