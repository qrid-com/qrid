using Qrid.Proto;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qrid.Common.Http
{
    public class ResponseModel : ResponseModelBase
    {

        #region Np (Non-Proto) methods

        public static ResponseModel<T> ErrorResponse<T>(Exception e) //where T : Qrid.Common.Proto.BaseData
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

        public static ResponseModel<T> ErrorResponse<T>(ResponseModelBase response) //where T : Qrid.Common.Proto.BaseData
        {
            return ErrorResponse<T>(response.Message, response.FriendlyMessage);
        }

        public static ResponseModel<T> ErrorResponse<T>(string message) //where T : Qrid.Common.Proto.BaseData
        {
            return ErrorResponse<T>(message, null);
        }
        public static ResponseModel<T> ErrorResponse<T>(string message, string friendlyMessage) //where T : Qrid.Common.Proto.BaseData
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

        public static ResponseModel<T> SuccessResponse<T>(T responseData) //where T : Qrid.Common.Proto.BaseData
        {
            var res = new ResponseModel<T>();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            res.Data = responseData;
            return res;
        }

        public static ResponseModel<T> SuccessResponse<T>() //where T : Qrid.Common.Proto.BaseData, new()
        {
            var res = new ResponseModel<T>();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            //res.Data = new T();
            return res;
        }



        public static ResponseModel<string> ErrorStringResponse(string error)
        {
            var res = ErrorResponse<string>(error);
            res.Data = error;
            return res;
        }

        public static ResponseModel<string> SuccessStringResponse(string response)
        {
            return SuccessResponse<string>(response);
        }

        public static ResponseModel<string> SuccessStringResponse()
        {
            return SuccessStringResponse(null);
        }

        public static ResponseModel SuccessResponse()
        {
            var res = new ResponseModel();
            res.Status = string.Empty;
            res.Message = string.Empty;
            res.FriendlyMessage = string.Empty;
            return res;
        }

        public static ResponseModel ErrorResponse(string message)
        {
            var res = new ResponseModel();
            res.Status = "ERROR";
            res.Message = message;
            res.FriendlyMessage = string.Empty;
            return res;
        }

        #endregion
    }

    public class ResponseModel<T> : ResponseModel // where T : Qrid.Common.Proto.BaseData
    {
        public T Data { get; set; }

        
        
    }

}
