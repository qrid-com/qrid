using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Reputation
{
    public partial class CreateCocoResponse : Qrid.Common.Proto.ProtoBase
    {

        public static CreateCocoResponse Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = System.Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CreateCocoResponse Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CreateCocoResponse Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }


    }
}
