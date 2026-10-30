using Google.Protobuf;
using System;

namespace Qrid.Common.Proto
{
    public partial class TypedDataList : ProtoBase
    {

        public static TypedDataList Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static TypedDataList Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static TypedDataList Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public static TypedDataList Create(TypedData firstItem)
        {
            var res = new TypedDataList();
            res.Items.Add(firstItem);
            return res;
        }

    }
}
