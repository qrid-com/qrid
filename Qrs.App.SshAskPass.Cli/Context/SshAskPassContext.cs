using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.App.SshAskPass.Cli.Payload;
using Qrs.Client.Context;
using Qrs.Proto;
using Qrs.Tunnel.Client;

namespace Qrs.App.SshAskPass.Cli.Context
{
    /// <summary>
    /// QRS client context for the qrs-ssh-askpass helper.
    ///
    /// It exposes two operations that mirror the two flows of the helper:
    ///   * <see cref="EncryptPasswordAsync"/> - the init flow (wrap a new passphrase).
    ///   * <see cref="DecryptPasswordAsync"/> - the askpass flow (unwrap the passphrase).
    ///
    /// Both flows bind the passphrase to the Host Name and the SSH Key File name through
    /// the QRS Associated Data, so the QRS Mobile App can present them for confirmation.
    /// </summary>
    public partial class SshAskPassContext : ClientContext
    {
        public const string APP_NAME = "SSH ASKPASS";

        public string TunnelServiceUri { get; set; }
        public string TunnelServiceDomain { get; set; }

        public SshAskPassContext(
            IUserInteraction ui,
            string tunnelServiceDomain,
            string tunnelServiceUri,
            IAsymKeyItemProvider asymKeyItemProvider,
            ICookieProvider cookieProvider,
            string? apiKey = null) :
            base(APP_NAME, ui, asymKeyItemProvider, cookieProvider)
        {
            TunnelServiceDomain = tunnelServiceDomain;
            TunnelServiceUri = tunnelServiceUri;
            ApiKey = apiKey;
        }

        public async Task<HttpResponseModel<StringResponse>> InitFromConfigAsync()
        {
            return await InitFromConfigAsync(TunnelServiceDomain, TunnelServiceUri, ApiKey);
        }

        /// <summary>
        /// init flow: qrs-encrypt the supplied passphrase (the QRS Mobile App wraps the
        /// DEK after scanning the QR code) and store the resulting envelope into the well
        /// known password file, bound to the host name and ssh key file name.
        /// </summary>
        public async Task<ResponseModel> EncryptPasswordAsync(string passwordFilePath, string sshKeyFileName, string password)
        {
            var hostName = SshAskPassPaths.GetHostName();

            var wrapper = new SshPasswordWrapper(this, hostName, sshKeyFileName);
            wrapper.SetPassword(password);

            var initResponse = await InitFromConfigAsync();
            if (initResponse.IsError())
                return ErrorResponse(initResponse.Message);

            var response = await WrapNewPayload(wrapper);
            if (response.IsError())
                return ErrorResponse(response.Message);

            // persist the encrypted envelope into the well known file
            Directory.CreateDirectory(Path.GetDirectoryName(passwordFilePath)!);
            File.WriteAllText(passwordFilePath, wrapper.QrsEnvelope.GetBase64String());
            SshAskPassPaths.RestrictToOwner(passwordFilePath);

            return SuccessResponse();
        }

        /// <summary>
        /// askpass flow: read the encrypted envelope from the well known password file and
        /// qrs-decrypt it (the QRS Mobile App unwraps the DEK after scanning the QR code).
        /// The decrypted passphrase is returned in the response; the host name and ssh key
        /// file name must match the Associated Data stored in the envelope.
        /// </summary>
        public async Task<HttpResponseModel<StringResponse>> DecryptPasswordAsync(string passwordFilePath, string sshKeyFileName)
        {
            if (!File.Exists(passwordFilePath))
                return ErrorStringResponse($"Password file '{passwordFilePath}' not found. Run 'qrs-ssh-askpass init' first.");

            var hostName = SshAskPassPaths.GetHostName();

            QrsEnvelope envelope;
            try
            {
                var base64 = File.ReadAllText(passwordFilePath).Trim();
                envelope = QrsEnvelope.Create(base64);
            }
            catch (Exception ex)
            {
                return ErrorStringResponse($"Password file '{passwordFilePath}' is not a valid qrs envelope: {ex.Message}");
            }
            if (envelope == null)
                return ErrorStringResponse($"Password file '{passwordFilePath}' is empty or invalid.");

            // the host name and ssh key file name must match the Associated Data stored in
            // the envelope, otherwise the unwrap operation is rejected
            var wrapper = new SshPasswordWrapper(this, hostName, sshKeyFileName)
            {
                QrsEnvelope = envelope
            };

            var initResponse = await InitFromConfigAsync();
            if (initResponse.IsError())
                return ErrorStringResponse(initResponse.Message);

            var response = await UnwrapPayload(wrapper);
            if (response.IsError())
                return ErrorStringResponse(response.Message);

            return SuccessStringResponse(wrapper.GetPassword());
        }
    }
}
