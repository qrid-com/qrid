using Google.Protobuf;
using System;
using GPB = Google.Protobuf;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CryptoHash : ProtoBase
    {
        public static CryptoHash Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoHash Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoHash Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }


        public static CryptoHash CreateHash(byte[] hashBytes, HashAlgo hashAlgo)
        {
            var res = new CryptoHash();
            res.HashAlgo = hashAlgo;
            res.HashBytes = GPB.ByteString.CopyFrom(hashBytes);
            return res;
        }

        public static CryptoHash CreateSha3x512(byte[] hashBytes)
        {
            var res = CreateHash(hashBytes, HashAlgo.Sha3X512);
            return res;
        }

        public static CryptoHash CreateSha3x256(byte[] hashBytes)
        {
            var res = CreateHash(hashBytes, HashAlgo.Sha3X256);
            return res;
        }

        public override DataType GetDataType()
        {
            return DataType.CryptoHash;
        }

        public string Explain()
        {
            return $"HashBytes.Hash:{HashBytes?.GetHashCode()}, HashAlgo:{HashAlgo}";
        }

        public bool IsEqualHash(CryptoHash other)
        {
            //    note that ByteString has defined .Equals and == operator that
            //    does byte-by-byte comparison via Span.SequenceEqual(..)

            return this.HashAlgo == other.HashAlgo && this.HashBytes.Equals(other.HashBytes);
        }

    }
}