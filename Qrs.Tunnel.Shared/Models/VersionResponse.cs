namespace Qrs.Tunnel.Shared.Models
{
    public sealed class VersionResponse
    {
        public string ServerProtocolVersion { get; set; } = string.Empty;
        public string BuildVersion { get; set; } = string.Empty;
        public string BuildTime { get; set; } = string.Empty;
    }
}
