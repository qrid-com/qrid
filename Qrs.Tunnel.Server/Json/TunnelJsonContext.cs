using System.Collections.Generic;
using System.Text.Json.Serialization;
using Qrid.Common.Analytics;
using Qrs.Tunnel.Server.Models;
using Model = Qrs.Tunnel.Shared.Models;

namespace Qrs.Tunnel.Server.Json
{
    // System.Text.Json source-generated serializer metadata. Using a compile-time
    // JsonSerializerContext (instead of runtime reflection) is what makes the
    // Minimal API endpoints safe to publish with Native AOT (PublishAot=true).
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(Model.MessageSendPollRequest))]
    [JsonSerializable(typeof(Model.MessageSendPollResponse))]
    [JsonSerializable(typeof(TunnelResponse<Model.MessageSendPollResponse>))]
    [JsonSerializable(typeof(TunnelResponse<Stats>))]
    [JsonSerializable(typeof(TunnelResponse<List<SessionLogItem>>))]
    [JsonSerializable(typeof(TunnelResponse<string>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Model.VersionResponse))]
    public partial class TunnelJsonContext : JsonSerializerContext
    {
    }
}
