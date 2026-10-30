using Qrs.App.SudoAskPass.Cli.Payload;
using Qrs.Proto;

namespace Qrs.App.SudoAskPass.Cli.Context
{
    public partial class SudoAskPassContext
    {
        /// <summary>
        /// init flow: ask the user for the sudo password, qrs-encrypt it (the QRS Mobile
        /// App wraps the DEK after scanning the QR code) and store the resulting envelope
        /// into the well known password file to be used later by the askpass flow.
        /// </summary>
        public async Task<int> InitCommandAsync(string passwordFilePath, bool force)
        {
            var hostName = SudoAskPassPaths.GetHostName();
            var fileName = Path.GetFileName(passwordFilePath);

            Terminal.WriteLine($"Initializing sudo password file for host '{hostName}'");
            Terminal.WriteLine($"Password file: {passwordFilePath}");

            if (File.Exists(passwordFilePath) && !force)
            {
                WriteError($"Password file '{passwordFilePath}' already exists. Re-run with --force to overwrite it.");
                return 1;
            }

            // read the sudo password (twice, without echo)
            Terminal.Write("Enter the sudo password to store: ");
            var password = Terminal.ReadHidden();
            Terminal.Write("Confirm the sudo password: ");
            var confirm = Terminal.ReadHidden();

            if (string.IsNullOrEmpty(password))
            {
                WriteError("Empty password - initialization cancelled.");
                return 1;
            }
            if (password != confirm)
            {
                WriteError("The two passwords do not match - initialization cancelled.");
                return 1;
            }

            // build wrapper and payload
            var wrapper = new SudoPasswordWrapper(this, hostName, fileName);
            wrapper.SetPassword(password);

            // initialize QRS client (tunnel discovery)
            var initResponse = await InitFromConfigAsync();
            if (initResponse.IsError())
            {
                WriteError(initResponse.Message);
                return 1;
            }

            // wrap payload via QRS Mobile App (QR shown on the terminal/stderr)
            Terminal.WriteLine("Scan the QR code below with your QRS app to encrypt the sudo password ...");
            var response = await WrapNewPayload(wrapper);
            if (response.IsError())
            {
                WriteError(response.Message);
                return 1;
            }

            // persist the encrypted envelope into the well known file
            Directory.CreateDirectory(Path.GetDirectoryName(passwordFilePath)!);
            File.WriteAllText(passwordFilePath, wrapper.QrsEnvelope.GetBase64String());
            SudoAskPassPaths.RestrictToOwner(passwordFilePath);

            Terminal.WriteLine($"SUCCESS - encrypted sudo password stored in '{passwordFilePath}'.");
            return 0;
        }
    }
}
