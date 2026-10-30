using Qrid.Common.Proto;
using Qrs.Client.Context;
using Qrs.Proto;

namespace Qrs.App.SudoAskPass.Cli.Payload
{
    /// <summary>
    /// Wraps the sudo password as a QRS payload.
    ///
    /// The payload itself is carried inside a <see cref="StringObject"/> (the QRS
    /// Tunnel/Mobile App never sees the payload, only the wrapped DEK and the
    /// Associated Data).
    ///
    /// The Host Name and the sudo password File Name are exposed as two separate
    /// Associated Data items so that the QRS Mobile App can present them to the user
    /// before confirming the wrap/unwrap operation, and so that a password file is
    /// cryptographically bound to the host and file name it was created for.
    /// </summary>
    public class SudoPasswordWrapper : Wrapper<StringObject>
    {
        public const string PAYLOAD_TYPE = "sp"; // sudo password

        public const string AD_KEY_HOST_NAME = "Host Name";
        public const string AD_KEY_PASSWORD_FILE = "Sudo Password File";

        public string HostName { get; set; }
        public string PasswordFileName { get; set; }

        public SudoPasswordWrapper(ClientContext context, string hostName, string passwordFileName) : base(context.AppName)
        {
            HostName = hostName;
            PasswordFileName = passwordFileName;
        }

        public override int GetImmutableTopLevelKey() => 1;

        public override string GetPayloadType() => PAYLOAD_TYPE;

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            res.KekName = string.Empty;
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = AD_KEY_HOST_NAME, Value = HostName });
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = AD_KEY_PASSWORD_FILE, Value = PasswordFileName });
            return res;
        }

        /// <summary>Stores the supplied sudo password into the payload.</summary>
        public void SetPassword(string password)
        {
            Payload = new StringObject { Value = password };
        }

        /// <summary>Reads the sudo password back from the (unwrapped) payload.</summary>
        public string GetPassword()
        {
            return Payload.Value;
        }
    }
}
