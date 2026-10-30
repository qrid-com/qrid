using Qrid.Common.Server.Models;

namespace Qrid.Common.Server.Extensions
{
    public static class HttpResponseExtensions
    {
        public static async Task<JsonCommonResponse<T>> GetJsonDataAsync<T>(this HttpResponseMessage response)
        {
            var stringData = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<JsonCommonResponse<T>>(stringData);
        }
    }
}