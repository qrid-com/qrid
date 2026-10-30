using Google.Protobuf;
using System;
using GPB = Google.Protobuf;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CryptoHmac : ProtoBase
    {
        public static CryptoHmac Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoHmac Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoHmac Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }


        public static CryptoHmac CreateHmac(byte[] hmacBytes, HashAlgo hashAlgo)
        {
            var res = new CryptoHmac();
            res.HashAlgo = hashAlgo;
            res.HmacBytes = GPB.ByteString.CopyFrom(hmacBytes);
            return res;
        }

        public static CryptoHmac CreateSha3x512(byte[] hmacBytes)
        {
            var res = CreateHmac(hmacBytes, HashAlgo.Sha3X512);
            return res;
        }

        public static CryptoHmac CreateSha3x256(byte[] hmacBytes)
        {
            var res = CreateHmac(hmacBytes, HashAlgo.Sha3X256);
            return res;
        }

        public override DataType GetDataType()
        {
            return DataType.CryptoHmac;
        }

        public string Explain()
        {
            return $"HmacBytes.Hash:{HmacBytes?.GetHashCode()}, HashAlgo:{HashAlgo}";
        }

        public bool IsEqualHash(CryptoHmac other)
        {
            return this.HashAlgo == other.HashAlgo && this.HmacBytes.Equals(other.HmacBytes);
        }

    }
}