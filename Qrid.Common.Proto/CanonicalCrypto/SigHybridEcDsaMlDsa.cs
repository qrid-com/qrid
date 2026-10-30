using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class SigHybridEcDsaMlDsa : ProtoBase
    {

        public static SigHybridEcDsaMlDsa Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static SigHybridEcDsaMlDsa Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static SigHybridEcDsaMlDsa Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static SigHybridEcDsaMlDsa CreateWith(SigEcDsa sigEcDsa, SigMlDsa sigMlDsa)
        {
            var res = new SigHybridEcDsaMlDsa();
            res.SigMlDsa = sigMlDsa;
            res.SigEcDsa = sigEcDsa;
            return res;
        }


    }
}