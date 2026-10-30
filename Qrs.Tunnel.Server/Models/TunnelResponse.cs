namespace Qrs.Tunnel.Server.Models
{
    // AOT-friendly response envelope that reproduces the wire format previously
    // produced by MyControllerBase.JsonSuccess/JsonError (a "Status"/"Data"/"Message"
    // JSON object). Clients (see Qrid.Common.Client.CommonApiClient + the generic
    // Qrid.Common.Http.HttpResponseModel<T>) read the "Status", "Data" and "Message"
    // properties case-insensitively, so this shape must be preserved.
    public sealed class TunnelResponse<T>
    {
        public string Status { get; set; } = "SUCCESS";
        public T? Data { get; set; }
        public string? Message { get; set; }

        // QRS Tunnel Server Protocol version (SemVer) - included into every response from the server
        public string ServerProtocolVersion { get; set; } = Qrs.Tunnel.Shared.QrsTunnelProtocol.ServerProtocolVersion;

        public static TunnelResponse<T> Success(T? data = default, string? message = null)
            => new TunnelResponse<T> { Status = "SUCCESS", Data = data, Message = message };

        public static TunnelResponse<T> Error(string error)
            => new TunnelResponse<T> { Status = "ERROR", Message = error };
    }
}
