using Qrid.Common.Proto;
using Qrs.Client.Context;
using Qrs.Proto;

namespace Qrs.App.EmailTools.Avalonia.Services;

/// <summary>
/// QRS payload wrapper that carries the raw SQLCipher database key so it can be
/// wrapped/unwrapped by the QRS Mobile App (KEK holder) into a <c>QrsEnvelope</c>.
/// The key itself never leaves the device in plaintext: it is encrypted with a
/// per-file DEK, and the DEK is wrapped by the Master Key held on the phone.
/// </summary>
public sealed class MailDbKeyWrapper : Wrapper<PaddedData>
{
    // Immutable, stable identifier so the same envelope is always addressed the same way.
    private const int DbKeyTopLevelKey = 1;

    public string Title { get; set; } = "QRS Mail local database key";

    public MailDbKeyWrapper(string appName) : base(appName)
    {
    }

    public override string GetPayloadType() => "mime:application/vnd.qrs.mail.dbkey";

    public override int GetImmutableTopLevelKey() => DbKeyTopLevelKey;

    public override AssociatedData2 GetAssociatedData2()
    {
        var res = new AssociatedData2();
        // KekName is filled in by the QRS Mobile app.
        res.KekName = string.Empty;
        res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "Title", Value = Title });
        return res;
    }
}
