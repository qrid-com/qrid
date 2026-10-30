using Google.Protobuf;
using System;

namespace Qrid.Common.Proto
{
    public partial class StringObject : ProtoBase
    {
        public static StringObject CreateWithString(string s)
        {
            var res = new StringObject();
            res.Value = s;
            return res;
        }

        public static StringObject Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static StringObject Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static StringObject Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }


    }
}
