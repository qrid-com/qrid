using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class SigMlDsa : ProtoBase
    {

        public static SigMlDsa Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static SigMlDsa Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static SigMlDsa Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }



    }
}