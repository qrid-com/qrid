using Google.Protobuf;
using System;

namespace Qrid.Common.Proto
{
    public partial class TypedMessage : ProtoBase
    {

        //public CR.CryptoKey AsCryptoKey
        //{
        //    get
        //    {
        //        if (DataType != DataType.CryptoKey)
        //            throw new ApplicationException($"Invalid operation - TypedData.DataType='{DataType}' cannot be converted to CryptoKey");

        //        var res = CR.CryptoKey.Create(this.DataBytes);
        //        return res;
        //    }
        //}

        public static TypedMessage Create(string messageType, byte[] messageBytes)
        {
            var res = new TypedMessage();
            res.MessageType = messageType;
            res.MessageBytes = ByteString.CopyFrom(messageBytes);
            return res;
        }

        public static TypedMessage Create(string messageType, ProtoBase protoObject)
        {
            var res = new TypedMessage();
            res.MessageType = messageType;
            res.MessageBytes = protoObject.GetByteString();
            return res;
        }

        public static TypedMessage Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static TypedMessage Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static TypedMessage Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public T Create<T>() where T : ProtoBase, new()
        {
            return ProtoBase.Create<T>(MessageBytes);
        }

    }
}
