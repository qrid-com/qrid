using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Enclave
{
    public partial class NewUserCertifyResponse : EnclaveResponseBase
    {

        public static NewUserCertifyResponse Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static NewUserCertifyResponse Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static NewUserCertifyResponse Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public override EnclaveStatus GetEnclaveStatus()
        {
            return this.EnclaveStatus;
        }

        public override void SetEnclaveStatus(EnclaveStatus response)
        {
            this.EnclaveStatus = response;
        }



    }
}
