using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class AsymEncryptedBytesWithKeyId : ProtoBase
    {

        public static AsymEncryptedBytesWithKeyId Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AsymEncryptedBytesWithKeyId Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AsymEncryptedBytesWithKeyId Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static AsymEncryptedBytesWithKeyId Create(AsymEncryptedBytes asymBytes, string keyId)
        {
            var res = new AsymEncryptedBytesWithKeyId();
            res.AsymEncryptedBytes = asymBytes;
            res.KeyId = keyId;
            return res;
        }

    }
}