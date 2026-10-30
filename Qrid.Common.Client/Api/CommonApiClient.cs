using Newtonsoft.Json;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Http;
using Qrid.Common.Proto.Const;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CR = Qrid.Common.Proto.Crypto;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;
using QP = Qrid.Common.Proto;

namespace Qrid.Common.Client.Api
{
    public partial class CommonApiClient : CommonContext
    {
        protected readonly HttpClient httpClient = CreateHttpClient(); // new HttpClient();

        public string BaseUrl { get; set; }
        public string AppName { get; set; }

        // Optional API key sent as a cookie on outgoing requests. When set, it is appended to
        // the outgoing Cookie header so the QRS Tunnel Server can authorize new-session creation
        // (see Qrs.Tunnel.Server QrsTunnel:ApiKey). Null/empty means no api key cookie is sent.
        public string ApiKey { get; set; }

        public ICookieProvider CookieProvider { get; set; } // responsible for persisting InstanceIdt Cookie that we get from server

        public static HttpClient CreateCrossPlatformHttpClient(int handlerTimeout, int clientTimeout)
        {
            if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
            {
                return new HttpClient(new HttpClientHandler())
                {
                    Timeout = TimeSpan.FromSeconds(clientTimeout)
                };
            }

            var socketHandler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(handlerTimeout)
            };

            return new HttpClient(socketHandler)
            {
                Timeout = TimeSpan.FromSeconds(clientTimeout)
            };
        }

        private static HttpClient CreateHttpClient()
        {
            return CreateCrossPlatformHttpClient(5, 10);
            // return CreateXplatformHttpClient(5,1200);  // 1200 for debugging - 20 min - this is for DEBUGGING ONLY so I could spend a lot of time in a server-side breakpoint
        }
        

        public CommonApiClient()
        {
        }

        public CommonApiClient(string baseUrl, string appName, ICookieProvider cookieProvider)
        {
            this.BaseUrl = baseUrl;
            this.AppName = appName;
            this.CookieProvider = cookieProvider ?? new MemoryCookieProvider();
        }

        public virtual string DefaultBaseUrl()
        {
            string defaultPortString = DefaultPort();
            if (!string.IsNullOrWhiteSpace(defaultPortString))
                defaultPortString = ":" + defaultPortString;

            return $"{DefaultSchema()}://{DefaultDomain()}{defaultPortString}/{DefaultPath()}";
        }

        public virtual string DefaultSchema()
        {
            return "http";
        }
        public virtual string DefaultDomain()
        {
            return "lh.qrid.com";
        }

        public virtual string DefaultPort()
        {
            // may need to be overwritten in all child classes
            return string.Empty;
        }

        public virtual string DefaultPath()
        {
            // may need to be overwritten in all child classes
            return string.Empty;
        }
        
        public T HttpGet<T>(string url, HttpClient client = null) where T : Qrid.Common.Proto.BaseData
        {
            var fullUrl = $"{BaseUrl}{url}";
            LogDebug($"GET {fullUrl}");
            var request = GetRequest(fullUrl);
            var response = (client ?? httpClient).SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            // instance id cookie - save from Response
            ResponseCookieSave(response, request.RequestUri.Host);
            string responseBody = response.Content.ReadAsStringAsync().Result;
            var responseModel = Newtonsoft.Json.JsonConvert.DeserializeObject<HttpResponseModel<T>>(responseBody);
            return responseModel.Data;
        }

        public HttpRequestMessage GetRequest(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            // instance id cookie - add to Request
            RequestCookieAdd(request);
            return request;
        }

        public async Task<HttpResponseModel<T>> HttpGetAsync<T>(string url, HttpClient client = null) where T : Qrid.Common.Proto.BaseData
        {
            var fullUrl = $"{BaseUrl}{url}";
            LogDebug($"GET {fullUrl}");
            CancelTokenSource = new CancellationTokenSource();
            try
            {
                var request = GetRequest(fullUrl);
                var response = await (client ?? httpClient).SendAsync(request, CancelTokenSource.Token);
                response.EnsureSuccessStatusCode();

                // instance id cookie - save from Response
                ResponseCookieSave(response, request.RequestUri.Host);

                string responseBody = await response.Content.ReadAsStringAsync();
                var responseModel = Newtonsoft.Json.JsonConvert.DeserializeObject<HttpResponseModel<T>>(responseBody);
                return responseModel;
            }
            catch (Exception e)
            {
                return ExplainException<T>(e);
            }

        }

        public HttpResponseModel<T> HttpPost<T>(string url, object data = null, HttpClient client = null) where T : Qrid.Common.Proto.BaseData
        {
            HttpResponseModel<T> httpResponseModel;
            var request = JsonConvert.SerializeObject(data);
            CancelTokenSource = new CancellationTokenSource();
            try
            {
                var fullUrl = $"{BaseUrl}{url}";
                var uri = new Uri(fullUrl);
                LogDebug($"POST {fullUrl}");

                var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                // instance id cookie - add to Request
                RequestCookieAdd(requestContent, uri.Host);

                var response = (client ?? httpClient).PostAsync(
                    fullUrl,
                    requestContent,
                    CancelTokenSource.Token
                ).Result;

                response.EnsureSuccessStatusCode();

                // instance id cookie - save from Response
                ResponseCookieSave(response, uri.Host);

                string responseBody = response.Content.ReadAsStringAsync().Result;
                httpResponseModel = JsonConvert.DeserializeObject<HttpResponseModel<T>>(responseBody);
                return httpResponseModel;
            }
            catch (Exception e)
            {
                return ExplainException<T>(e);
            }
        }

        public bool CancelRequest()
        {
            if (CancelTokenSource == null)
            {
                LogDebug("WARNING - CancelTokenSource == null");
                return false;
            }
            if (!CancelTokenSource.Token.CanBeCanceled)
            {
                LogDebug("WARNING - CancelTokenSource.Token cannot be cancelled");
                return false;
            }
            CancelTokenSource?.Cancel();
            return true;
        }

        private CancellationTokenSource CancelTokenSource { get; set; }

        public void RequestCookieAdd(HttpContent requestContent, string host)
        {
            if (CookieProvider == null)
                throw new ApplicationException("CookieProvider is null!");

            var cookie = BuildCookieHeader(host);
            if (cookie != null)
                requestContent.Headers.Add("Cookie", cookie);
        }
        public void RequestCookieAdd(HttpRequestMessage request)
        {
            if (CookieProvider == null)
                throw new ApplicationException("CookieProvider is null!");

            var cookie = BuildCookieHeader(request.RequestUri.Host);
            if (cookie != null)
                request.Headers.Add("Cookie", cookie);
        }

        // Builds the outgoing Cookie header value, combining the persisted server cookie
        // (e.g. instance_idt) with the optional api key cookie when ApiKey is set.
        private string BuildCookieHeader(string host)
        {
            var cookie = CookieProvider.GetCookie(host);

            if (string.IsNullOrWhiteSpace(ApiKey))
                return cookie;

            var apiKeyCookie = $"{Qrid.Common.Const.CookieName.QrsTunnelApiKey}={ApiKey}";
            return string.IsNullOrWhiteSpace(cookie) ? apiKeyCookie : $"{cookie}; {apiKeyCookie}";
        }

        public void ResponseCookieSave(HttpResponseMessage response, string host)
        {
            if (response.Headers.Contains("Set-Cookie"))
            {
                // instance_idt=ProtoObjectInBase64; Domain=<domain-value>; Secure; HttpOnly
                var rawCookie = response.Headers.GetValues("Set-Cookie").ToList()[0];
                // instance_idt=ProtoObjectInBase64
                var nameAndValue = rawCookie.Split(';')[0];
                CookieProvider.SetCookie(host, nameAndValue);
            }
        }

        public async Task<HttpResponseModel<T>> HttpPostAsync<T>(string url, object data = null, HttpClient client = null) where T : Qrid.Common.Proto.BaseData
        {
            HttpResponseModel<T> httpResponseModel;
            var request = JsonConvert.SerializeObject(data);

            CancelTokenSource = new CancellationTokenSource();

            // TODO - move non-network-call stuff out of try-catch
            try
            {
                var fullUrl = $"{BaseUrl}{url}";
                var uri = new Uri(fullUrl);
                LogDebug($"POST {fullUrl}");
                var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                // instance id cookie - add to Request
                RequestCookieAdd(requestContent, uri.Host);

                var response = await (client ?? httpClient).PostAsync(
                    fullUrl,
                    requestContent,
                    CancelTokenSource.Token
                );
                response.EnsureSuccessStatusCode();

                // instance id cookie - save from Response
                ResponseCookieSave(response, uri.Host);

                string responseBody = await response.Content.ReadAsStringAsync();
                httpResponseModel = JsonConvert.DeserializeObject<HttpResponseModel<T>>(responseBody);
                return httpResponseModel;
            }
            catch (Exception e)
            {
                return ExplainException<T>(e);
            }
        }

        // uses compiled json serializers that are AOT-compatible and do not use reflection
        public async Task<HttpResponseModel<T>> JsonPostAsync<T>(JsonSerializerOptions jso, string url, object data = null, HttpClient client = null) where T : Qrid.Common.Proto.BaseData
        {
            HttpResponseModel<T> httpResponseModel;
            //var request = JsonConvert.SerializeObject(data);
            var request = System.Text.Json.JsonSerializer.Serialize(data, jso);

            CancelTokenSource = new CancellationTokenSource();

            // TODO - move non-network-call stuff out of try-catch
            try
            {
                var fullUrl = $"{BaseUrl}{url}";
                var uri = new Uri(fullUrl);
                LogDebug($"POST {fullUrl}");
                var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                // instance id cookie - add to Request
                RequestCookieAdd(requestContent, uri.Host);

                var response = await (client ?? httpClient).PostAsync(
                    fullUrl,
                    requestContent,
                    CancelTokenSource.Token
                );
                response.EnsureSuccessStatusCode();

                // instance id cookie - save from Response
                ResponseCookieSave(response, uri.Host);

                string responseBody = await response.Content.ReadAsStringAsync();
                //httpResponseModel = JsonConvert.DeserializeObject<HttpResponseModel<T>>(responseBody);
                httpResponseModel = System.Text.Json.JsonSerializer.Deserialize<HttpResponseModel<T>>(responseBody, jso);
                return httpResponseModel;
            }
            catch (Exception e)
            {
                return ExplainException<T>(e);
            }
        }

        public HttpResponseModel<T> ExplainException<T>(Exception e) where T : Qrid.Common.Proto.BaseData
        {
            if (e.InnerException != null)
            {
                if (e.InnerException is System.TimeoutException)
                    if (e.InnerException.Message.Contains("connection could not be established"))
                        return ErrorResponse<T>($"Cannot connect to server at '{BaseUrl}'. Please try again later");

                return ErrorResponse<T>($"Cannot connect to server at '{BaseUrl}'. The InnerException error is '{e.InnerException.Message}'");
            }
            return ErrorResponse<T>($"Cannot connect to server at '{BaseUrl}'. The error is '{e.Message}'");
        }

        public ResponseModel<T> NpExplainException<T>(Exception e) 
        {
            if (e.InnerException != null)
            {
                if (e.InnerException is System.TimeoutException)
                    if (e.InnerException.Message.Contains("connection could not be established"))
                        return NpErrorResponse<T>($"Cannot connect to server at '{BaseUrl}'. Please try again later");

                return NpErrorResponse<T>(e.InnerException.Message);
            }
            return NpErrorResponse<T>(e.Message);
        }

        // "Np" means Non-Proto - where T is NOT required to inherit from ProtoBase

        public async Task<ResponseModel<T>> NpGetAsync<T>(string url, HttpClient client = null)
        {
            var fullUrl = $"{BaseUrl}{url}";
            LogDebug($"GET {fullUrl}");
            CancelTokenSource = new CancellationTokenSource();
            try
            {
                var request = GetRequest(fullUrl);
                var response = await (client ?? httpClient).SendAsync(request, CancelTokenSource.Token);
                response.EnsureSuccessStatusCode();

                // instance id cookie - save from Response
                ResponseCookieSave(response, request.RequestUri.Host);

                string responseBody = await response.Content.ReadAsStringAsync();
                var responseModel = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseModel<T>>(responseBody);
                return responseModel;
            }
            catch (Exception e)
            {
                return NpExplainException<T>(e);
            }

        }

        public async Task<ResponseModel<T>> NpPostAsync<T>(string url, object data = null, HttpClient client = null) // where T : Qrid.Common.Proto.BaseData
        {
            var request = JsonConvert.SerializeObject(data);

            CancelTokenSource = new CancellationTokenSource();

            // TODO - move non-network-call stuff out of try-catch
            try
            {
                var fullUrl = $"{BaseUrl}{url}";
                var uri = new Uri(fullUrl);
                LogDebug($"POST {fullUrl}");
                var requestContent = new StringContent(request, Encoding.UTF8, "application/json");

                // instance id cookie - add to Request
                RequestCookieAdd(requestContent, uri.Host);

                var response = await (client ?? httpClient).PostAsync(
                    fullUrl,
                    requestContent,
                    CancelTokenSource.Token
                );
                response.EnsureSuccessStatusCode();

                // instance id cookie - save from Response
                ResponseCookieSave(response, uri.Host);

                string responseBody = await response.Content.ReadAsStringAsync();
                var responseModel = JsonConvert.DeserializeObject<ResponseModel<T>>(responseBody);
                return responseModel;
            }
            catch (Exception e)
            {
                return NpExplainException<T>(e);
            }
        }


    }
}
