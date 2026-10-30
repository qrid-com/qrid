using Google.Protobuf;
using System;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Proto
{
    public partial class TypedData : ProtoBase
    {

        public CR.CertifiedPublicKey AsCertifiedPublicKey
        {
            get
            {
                if (DataType != DataType.CertifiedPublicKey)
                    throw new ApplicationException($"Invalid operation - TypedData.DataType='{DataType}' cannot be converted into CertifiedPublicKey");

                var res = CR.CertifiedPublicKey.Create(this.DataBytes);
                return res;
            }
        }

        public CR.CryptoKey AsCryptoKey
        {
            get
            {
                if (DataType != DataType.CryptoKey)
                    throw new ApplicationException($"Invalid operation - TypedData.DataType='{DataType}' cannot be converted to CryptoKey");

                var res = CR.CryptoKey.Create(this.DataBytes);
                return res;
            }
        }

        public CR.CryptoKeyPair AsCryptoKeyPair
        {
            get
            {
                if (DataType != DataType.CryptoKeyPair)
                    throw new ApplicationException($"Invalid operation - TypedData.DataType='{DataType}' cannot be converted into CryptoKeyPair");

                var res = CR.CryptoKeyPair.Create(this.DataBytes);
                return res;
            }
        }

        public CR.CertifiedKeyPair AsCertifiedKeyPair
        {
            get
            {
                if (DataType != DataType.CertifiedKeyPair)
                    throw new ApplicationException($"Invalid operation - TypedData.DataType='{DataType}' cannot be converted into CertifiedKeyPair");

                var res = CR.CertifiedKeyPair.Create(this.DataBytes);
                return res;
            }
        }

        public static TypedData Create(ProtoBase protoObject)
        {
            var res = new TypedData();
            res.DataType = protoObject.GetDataType();
            res.DataBytes = protoObject.GetByteString();
            return res;
        }

        public static TypedData Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static TypedData Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static TypedData Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

    }
}
