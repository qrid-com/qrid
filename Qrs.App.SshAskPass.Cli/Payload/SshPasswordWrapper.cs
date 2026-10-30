using Qrid.Common.Proto;
using Qrs.Client.Context;
using Qrs.Proto;

namespace Qrs.App.SshAskPass.Cli.Payload
{
    /// <summary>
    /// Wraps the ssh key passphrase as a QRS payload.
    ///
    /// The payload itself is carried inside a <see cref="StringObject"/> (the QRS
    /// Tunnel/Mobile App never sees the payload, only the wrapped DEK and the
    /// Associated Data).
    ///
    /// The Host Name and the SSH Key File name are exposed as two separate Associated
    /// Data items so that the QRS Mobile App can present them to the user before
    /// confirming the wrap/unwrap operation, and so that a stored passphrase is
    /// cryptographically bound to the host and the ssh key file it was created for.
    /// </summary>
    public class SshPasswordWrapper : Wrapper<StringObject>
    {
        public const string PAYLOAD_TYPE = "ssh"; // ssh key passphrase

        public const string AD_KEY_HOST_NAME = "Host Name";
        public const string AD_KEY_SSH_KEY_FILE = "SSH Key File";

        public string HostName { get; set; }
        public string SshKeyFileName { get; set; }

        public SshPasswordWrapper(ClientContext context, string hostName, string sshKeyFileName) : base(context.AppName)
        {
            HostName = hostName;
            SshKeyFileName = sshKeyFileName;
        }

        public override int GetImmutableTopLevelKey() => 1;

        public override string GetPayloadType() => PAYLOAD_TYPE;

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            res.KekName = string.Empty;
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = AD_KEY_HOST_NAME, Value = HostName });
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = AD_KEY_SSH_KEY_FILE, Value = SshKeyFileName });
            return res;
        }

        /// <summary>Stores the supplied ssh key passphrase into the payload.</summary>
        public void SetPassword(string password)
        {
            Payload = new StringObject { Value = password };
        }

        /// <summary>Reads the ssh key passphrase back from the (unwrapped) payload.</summary>
        public string GetPassword()
        {
            return Payload.Value;
        }
    }
}
