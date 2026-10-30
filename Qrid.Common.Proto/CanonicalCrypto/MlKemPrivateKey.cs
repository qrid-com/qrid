using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class MlKemPrivateKey : ProtoBase
    {

        public static MlKemPrivateKey Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static MlKemPrivateKey Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static MlKemPrivateKey Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static MlKemPrivateKey CreateWith(byte[] encodedPrivateKey)
        {
            var res = new MlKemPrivateKey();
            res.PrivateBytes = ByteString.CopyFrom(encodedPrivateKey);
            return res;
        }



    }
}