using Google.Protobuf;
using System;

namespace Qrid.Common.Proto
{
    public partial class PaddedData : ProtoBase
    {


        public static PaddedData CreateWith(string data, string padding)
        {
            var so = new StringObject();
            so.Value = data;
            var res = new PaddedData();
            res.Data = so.ToByteString();
            res.Padding = padding;
            return res;
        }

        public string AsString
        {
            get
            {
                var res = StringObject.Create(this.Data)?.Value;
                return res;
            }
        }

        public static PaddedData Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static PaddedData Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static PaddedData Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

    }
}
