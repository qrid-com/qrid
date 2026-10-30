namespace Qrs.App.EmailTools.Avalonia.Models;

/// <summary>
/// The pre-defined lists a message can be placed on before it is deleted.
/// The values are hard-coded and persisted verbatim in the <c>messagelist</c> table.
/// </summary>
public static class MessageListType
{
    /// <summary>Message should be physically deleted from the mail server.</summary>
    public const string DeleteFromServer = "dfs";

    /// <summary>Message should be deleted from the local (client) database.</summary>
    public const string DeleteFromClient = "dfc";
}
