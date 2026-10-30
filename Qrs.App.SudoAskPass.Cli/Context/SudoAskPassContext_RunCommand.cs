using Qrs.App.SudoAskPass.Cli.Payload;
using Qrs.Proto;

namespace Qrs.App.SudoAskPass.Cli.Context
{
    public partial class SudoAskPassContext
    {
        /// <summary>
        /// askpass flow (the default flow invoked by sudo via SUDO_ASKPASS):
        /// read the encrypted envelope from the well known password file, qrs-decrypt it
        /// (the QRS Mobile App unwraps the DEK after scanning the QR code) and write the
        /// decrypted sudo password to stdout as required by the sudo_askpass contract.
        ///
        /// 'prompt' is the single argument sudo passes to the askpass helper (e.g.
        /// "[sudo] password for user:"); it is only shown on the terminal for context.
        /// </summary>
        public async Task<int> RunCommandAsync(string passwordFilePath, string? prompt)
        {
            if (!File.Exists(passwordFilePath))
            {
                WriteError($"Password file '{passwordFilePath}' not found. Run 'qrs-sudo-askpass init' first.");
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(prompt))
                Terminal.WriteLine(prompt);

            var hostName = SudoAskPassPaths.GetHostName();
            var fileName = Path.GetFileName(passwordFilePath);

            // load the encrypted envelope
            var base64 = File.ReadAllText(passwordFilePath).Trim();
            QrsEnvelope envelope;
            try
            {
                envelope = QrsEnvelope.Create(base64);
            }
            catch (Exception ex)
            {
                WriteError($"Password file '{passwordFilePath}' is not a valid qrs envelope: {ex.Message}");
                return 1;
            }
            if (envelope == null)
            {
                WriteError($"Password file '{passwordFilePath}' is empty or invalid.");
                return 1;
            }

            // build wrapper - the host name and file name must match the Associated Data
            // stored in the envelope, otherwise the unwrap operation is rejected
            var wrapper = new SudoPasswordWrapper(this, hostName, fileName)
            {
                QrsEnvelope = envelope
            };

            // initialize QRS client (tunnel discovery)
            var initResponse = await InitFromConfigAsync();
            if (initResponse.IsError())
            {
                WriteError(initResponse.Message);
                return 1;
            }

            // unwrap payload via QRS Mobile App (QR shown on the terminal/stderr)
            Terminal.WriteLine("Scan the QR code below with your QRS app to decrypt the sudo password ...");
            var response = await UnwrapPayload(wrapper);
            if (response.IsError())
            {
                WriteError(response.Message);
                return 1;
            }

            var password = wrapper.GetPassword();

            // ONLY the password is written to stdout (this is what sudo reads)
            Console.Out.Write(password);
            Console.Out.Write('\n');
            Console.Out.Flush();

            return 0;
        }
    }
}
