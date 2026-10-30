using Google.Protobuf;
using Qrid.Proto.DbConst;
using System;
using System.Collections.Generic;
using System.Text;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Enclave
{
    public partial class EnclaveResponseBase : Qrid.Common.Proto.ProtoBase
    {

        public virtual EnclaveStatus GetEnclaveStatus()
        {
            throw new NotImplementedException("not implemented here - this needs to be overwritten in a deriving class");
        }

        public virtual void SetEnclaveStatus(EnclaveStatus response)
        {
            throw new NotImplementedException("not implemented here - this needs to be overwritten in a deriving class");
        }

        public void SetEnclaveStatus(string enclaveRequestId, int fastResponse, string status = null, string message = null)
        {
            var response = new EnclaveStatus();
            response.EnclaveRequestId = enclaveRequestId;
            response.FastResponse = fastResponse;
            response.Status = status ?? string.Empty;
            response.Message = message ?? string.Empty;
            SetEnclaveStatus(response);
        }

        public bool IsDelayedResponse()
        {
            return !string.IsNullOrWhiteSpace(GetEnclaveStatus()?.EnclaveRequestId);
        }

        public bool IsFastResponse()
        {
            return IsDelayedResponse() && GetEnclaveStatus().FastResponse > 0;
        }

        public bool IsApproved()
        {
            return GetEnclaveStatus()?.IsApproved() ?? false;
        }

        public bool IsPending()
        {
            return GetEnclaveStatus()?.IsPending() ?? false;
        }

        public bool IsRejected()
        {
            return GetEnclaveStatus()?.IsRejected() ?? false;
        }

    }
}
