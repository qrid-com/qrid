using System.Text.RegularExpressions;

namespace Qrs.App.SshAskPass.Cli.Context
{
    /// <summary>
    /// Parses the single prompt argument that ssh / ssh-add pass to an SSH_ASKPASS
    /// helper, in order to extract the ssh key file name it is asking the passphrase for.
    ///
    /// Known prompt shapes (the exact wording varies between OpenSSH versions/tools):
    ///   ssh-add : "Enter passphrase for /home/alice/.ssh/id_rsa: "
    ///   ssh-add : "Enter passphrase for /home/alice/.ssh/id_rsa (will confirm each use): "
    ///   ssh     : "Enter passphrase for key '/home/alice/.ssh/id_ed25519': "
    ///
    /// The extracted key file name is exposed to the user (and bound as Associated Data)
    /// so they can verify exactly which key the password is being released for.
    /// </summary>
    public static class SshPromptParser
    {
        public const string UnknownKeyFile = "(unknown ssh key)";

        public static string ExtractKeyFileName(string? prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return UnknownKeyFile;

            var text = prompt.Trim();

            // 1) Prefer an explicitly quoted value, e.g. ...for key '/path/to/key':
            var quoted = Regex.Match(text, "['\"]([^'\"]+)['\"]");
            if (quoted.Success)
            {
                var value = quoted.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            // 2) Otherwise take whatever follows "passphrase for" (optionally "for key").
            var match = Regex.Match(
                text,
                @"passphrase\s+for\s+(?:key\s+)?(.+)$",
                RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var value = match.Groups[1].Value;

                // Drop a trailing parenthetical note such as "(will confirm each use)".
                var paren = value.IndexOf('(');
                if (paren >= 0)
                    value = value.Substring(0, paren);

                value = value.Trim().TrimEnd(':').Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            // 3) Could not recognise the prompt - fall back to the raw prompt text.
            return text;
        }
    }
}
