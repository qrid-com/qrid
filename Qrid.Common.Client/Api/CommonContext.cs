using Qrid.Common.Http;
using Qrid.Proto.Messaging;
using System;
using System.Diagnostics;
using System.Net;

namespace Qrid.Common.Client.Api
{
    public partial class CommonContext
    {
        // 
        public HttpResponseModel<T> ErrorResponse<T>(Exception e) where T : Qrid.Common.Proto.BaseData
        {
            if (e is WebException)
            {
                // Network Problem?
                return ErrorResponse<T>(e.Message, "Network Error");
            }
            else if (e is ApplicationException)
            {
                // ApplicationException?
                return ErrorResponse<T>(e.Message, "Application Error");
            }
            else
            {
                // other - unexpected
                return ErrorResponse<T>(e.Message, "Unexpected Error");
            }
        }

        public HttpResponseModel<T> ErrorResponse<T>(ResponseModelBase response) where T : Qrid.Common.Proto.BaseData
        {
            return ErrorResponse<T>(response.Message, response.FriendlyMessage);
        }

        public static HttpResponseModel<T> ErrorResponse<T>(string message) where T : Qrid.Common.Proto.BaseData
        {
            return ErrorResponse<T>(message, null);
        }
        public static HttpResponseModel<T> ErrorResponse<T>(string message, string friendlyMessage) where T : Qrid.Common.Proto.BaseData
        {
            var res = new HttpResponseModel<T>();
            res.Status = "ERROR";
            res.Message = message;
            res.FriendlyMessage = friendlyMessage;
            //
            //OpStatus = res.GetOpStatus();
            //
            return res;
        }

        public static HttpResponseModel<T> SuccessResponse<T>(T responseData) where T : Qrid.Common.Proto.BaseData
        {
            var res = new HttpResponseModel<T>();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            res.Data = responseData;
            return res;
        }

        public HttpResponseModel<T> SuccessResponse<T>() where T : Qrid.Common.Proto.BaseData, new()
        {
            var res = new HttpResponseModel<T>();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            res.Data = new T();
            return res;
        }

        public static StringResponse GetStringResponse(string value)
        {
            return new StringResponse() { StringValue = value };
        }

        public static HttpResponseModel<StringResponse> ErrorStringResponse(string error)
        {
            var res = ErrorResponse<StringResponse>(error);
            res.Data = GetStringResponse(error);
            return res;
        }

        public static HttpResponseModel<StringResponse> SuccessStringResponse(string response)
        {
            return SuccessResponse<StringResponse>(GetStringResponse(response));
        }

        public HttpResponseModel<StringResponse> SuccessStringResponse()
        {
            return SuccessStringResponse(null);
        }

        [Conditional("DEBUG")]
        public void LogDebug(string message)
        {
            
            Console.WriteLine($"{DateTime.Now.ToString()} - {message}");
            Debug.WriteLine($"{DateTime.Now.ToString()} - {message}");
            
        }

        // 

        #region Np (Non-Proto) methods
        
        public static ResponseModel<T> NpErrorResponse<T>(Exception e) //where T : Qrid.Common.Proto.BaseData
        {
            if (e is WebException)
            {
                // Network Problem?
                return NpErrorResponse<T>(e.Message, "Network Error");
            }
            else if (e is ApplicationException)
            {
                // ApplicationException?
                return NpErrorResponse<T>(e.Message, "Application Error");
            }
            else
            {
                // other - unexpected
                return NpErrorResponse<T>(e.Message, "Unexpected Error");
            }
        }

        public static ResponseModel<T> NpErrorResponse<T>(ResponseModelBase response) //where T : Qrid.Common.Proto.BaseData
        {
            return NpErrorResponse<T>(response.Message, response.FriendlyMessage);
        }

        public static ResponseModel<T> NpErrorResponse<T>(string message) //where T : Qrid.Common.Proto.BaseData
        {
            return NpErrorResponse<T>(message, null);
        }
        public static ResponseModel<T> NpErrorResponse<T>(string message, string friendlyMessage) //where T : Qrid.Common.Proto.BaseData
        {
            var res = new ResponseModel<T>();
            res.Status = "ERROR";
            res.Message = message;
            res.FriendlyMessage = friendlyMessage;
            //
            //OpStatus = res.GetOpStatus();
            //
            return res;
        }

        public static ResponseModel<T> NpSuccessResponse<T>(T responseData) //where T : Qrid.Common.Proto.BaseData
        {
            var res = new ResponseModel<T>();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            res.Data = responseData;
            return res;
        }

        public static ResponseModel<T> NpSuccessResponse<T>() //where T : Qrid.Common.Proto.BaseData, new()
        {
            var res = new ResponseModel<T>();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            //res.Data = new T();
            return res;
        }



        public static ResponseModel<string> NpErrorStringResponse(string error)
        {
            var res = NpErrorResponse<string>(error);
            res.Data = error;
            return res;
        }

        public static ResponseModel<string> NpSuccessStringResponse(string response)
        {
            return NpSuccessResponse<string>(response);
        }

        public static ResponseModel<string> NpSuccessStringResponse()
        {
            return NpSuccessStringResponse(null);
        }

        public static ResponseModel SuccessResponse2()
        {
            var res = new ResponseModel();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            return res;
        }

        public static ResponseModel ErrorResponse2(string message)
        {
            var res = new ResponseModel();
            res.Status = "ERROR";
            res.Message = message;
            res.FriendlyMessage = string.Empty;
            return res;
        }

        #endregion

        public ResponseModel SuccessResponse()
        {
            var res = new ResponseModel();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            return res;
        }

        public ResponseModel SuccessResponse(string message)
        {
            var res = new ResponseModel();
            res.Status = string.Empty;
            res.Message = message;
            res.FriendlyMessage = string.Empty;
            return res;
        }

        public ResponseModel ErrorResponse(string message)
        {
            var res = new ResponseModel();
            res.Status = "ERROR";
            res.Message = message;
            res.FriendlyMessage = string.Empty;
            return res;
        }

        public ResponseModel ErrorResponse(Exception e)
        {
            return ErrorResponse(e.Message);
        }

    }
}
