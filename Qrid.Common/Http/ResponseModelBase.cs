using Qrid.Proto;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qrid.Common.Http
{
    public class ResponseModelBase
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string FriendlyMessage { get; set; } // user-friendly error message
        
        public bool IsSuccess()
        {
            return (Status != "ERROR"); // TODO - what about "TIMEOUT"???
        }

        public bool IsError()
        {
            return (Status == "ERROR" || Status == "TIMEOUT");
        }

        //public bool IsErrorOrTimeout()
        //{
        //    return (Status == "ERROR" || Status == "TIMEOUT");
        //}
        public void SetError()
        {
            Status = "ERROR";
        }

        public void SetError(string errorMessage)
        {
            Status = "ERROR";
            Message = errorMessage;
        }

        public void AddError(string errorMessage)
        {
            Status = "ERROR";
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                Message = errorMessage;
            }
            else
            {
                Message += "; " + errorMessage;
            }
        }

        public void ThrowIfError()
        {
            if (!IsSuccess())
                throw new ApplicationException($"Response Error: '{Message}'");
        }

        
    }

}
