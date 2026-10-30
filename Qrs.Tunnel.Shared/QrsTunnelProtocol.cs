using NuGet.Versioning;

namespace Qrs.Tunnel.Shared
{
    /// <summary>
    /// QRS Tunnel protocol versions (Semantic Versioning - MAJOR.MINOR.PATCH).
    ///
    /// There are 2 independent (orthogonal) protocol versions:
    ///  - QRS Tunnel Server Protocol - REST protocol between clients (CA / MA) and Qrs.Tunnel.Server,
    ///    defined here (<see cref="ServerProtocolVersion"/>) and returned by the server in every response.
    ///  - QRS Tunnel Client Protocol - end-to-end protocol between CA and MA,
    ///    hardcoded in Qrs.Tunnel.Client (see TunnelClient.ClientProtocolVersion).
    ///
    /// Versions are manually incremented by QRS developers following Semantic Versioning rules:
    /// increment MAJOR only for breaking (backwards-incompatible) changes.
    /// A peer is supported when its MAJOR version is not greater than our own MAJOR version.
    /// Client Protocol version is mandatory (it is bound to every tunnel message via AEAD).
    /// A missing (empty) Server Protocol version means a legacy server created before protocol versioning
    /// was introduced and it is treated as supported.
    /// </summary>
    public static class QrsTunnelProtocol
    {
        /// <summary>
        /// Server protocol version implemented by Qrs.Tunnel.Server and supported by Qrs.Tunnel.Client.
        /// </summary>
        public const string ServerProtocolVersion = "1.0.0";

        public const string ClientProtocolName = "QRS Client Protocol";
        public const string ServerProtocolName = "QRS Server Protocol";

        private const int MaxVersionLength = 64;

        public static bool TryParse(string version, out SemanticVersion semanticVersion)
        {
            semanticVersion = null;
            if (string.IsNullOrWhiteSpace(version) || version.Length > MaxVersionLength)
                return false;
            return SemanticVersion.TryParse(version.Trim(), out semanticVersion);
        }

        /// <summary>
        /// Checks if a protocol version received from the other side (QR code, tunnel message or server response)
        /// is supported by our own protocol version.
        /// Returns null when supported; returns an error message ("please upgrade") when not supported.
        /// </summary>
        public static string ValidateVersion(string protocolName, string receivedVersion, string ownVersion, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(receivedVersion))
                return required
                    ? $"Missing {protocolName} version, please upgrade your software"
                    : null; // legacy software (before protocol versioning was introduced)

            if (!TryParse(receivedVersion, out var received))
                return $"Invalid {protocolName} version '{Truncate(receivedVersion)}' - expected MAJOR.MINOR.PATCH";

            if (!TryParse(ownVersion, out var own))
                return $"Invalid own {protocolName} version '{Truncate(ownVersion)}' - expected MAJOR.MINOR.PATCH";

            if (received.Major > own.Major)
                return $"Cannot support {protocolName} {received.ToNormalizedString()}, please upgrade your software (this software supports {protocolName} {own.ToNormalizedString()})";

            return null;
        }

        public static string ValidateServerProtocolVersion(string receivedVersion)
            => ValidateVersion(ServerProtocolName, receivedVersion, ServerProtocolVersion);

        private static string Truncate(string value)
            => value.Length > MaxVersionLength ? value.Substring(0, MaxVersionLength) + "..." : value;
    }
}
