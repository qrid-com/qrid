using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.Proto.Api
{
    public partial class PairingResponse : Qrid.Common.Proto.ProtoBase
    {

        public static PairingResponse Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static PairingResponse Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static PairingResponse Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static PairingResponse Create()
        {
            var res = new PairingResponse();
            return res;
        }

        public static PairingResponse Create(CR.CryptoKey pubKey, string keyId, string status, string message)
        {
            var res = new PairingResponse() { PubKey = pubKey, KeyId = keyId, Status = status, Message = message };
            return res;
        }


    }
}