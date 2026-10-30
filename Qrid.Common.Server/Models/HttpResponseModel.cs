namespace Qrid.Common.Server.Models
{
    sealed class HttpResponseModel
    {
        public string Status { get; set; }
        public object Data { get; set; }
        public string Message { get; set; }
    }
}