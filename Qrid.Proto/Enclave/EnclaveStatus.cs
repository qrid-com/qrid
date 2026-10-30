using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using Qrid.Proto.DbConst;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Enclave
{
    public partial class EnclaveStatus : Qrid.Common.Proto.ProtoBase
    {

        public static EnclaveStatus Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EnclaveStatus Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EnclaveStatus Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public bool IsApproved()
        {
            return Status == EnclaveRequestStatus.APPROVED;
        }

        public bool IsPending()
        {
            return Status == EnclaveRequestStatus.PENDING;
        }

        public bool IsRejected()
        {
            return Status == EnclaveRequestStatus.REJECTED;
        }


    }
}
