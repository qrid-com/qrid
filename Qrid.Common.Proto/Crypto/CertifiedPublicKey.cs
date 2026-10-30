using Google.Protobuf;
using System;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CertifiedPublicKey : ProtoBase
    {

        public static CertifiedPublicKey Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CertifiedPublicKey Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CertifiedPublicKey Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        // CR.CryptoKey.FromCanonicalKey(AccountAsym.PublicKey)

        public static CertifiedPublicKey Create(CC.CanonicalKey ccPubKey)
        {
            var pubKey = CR.CryptoKey.FromCanonicalKey(ccPubKey);
            return Create(pubKey);
        }

        public static CertifiedPublicKey Create(CC.CanonicalKey ccPubKey, DateTime notBefore, DateTime notAfter)
        {
            var pubKey = CR.CryptoKey.FromCanonicalKey(ccPubKey);
            return Create(pubKey, notBefore, notAfter);
        }

        public static CertifiedPublicKey Create(CryptoKey pubKey)
        {
            DateTime notBefore = DateTime.Now.AddDays(-1); // TODO - do we even need to init this data for the root non-certified PubKey ?
            DateTime notAfter = notBefore.AddYears(10);    // TODO - do we even need to init this data for the root non-certified PubKey ?
            return Create(pubKey, notBefore, notAfter);
        }
        public static CertifiedPublicKey Create(CryptoKey pubKey, DateTime notBefore, DateTime notAfter)
        {
            if (pubKey.KeyType != KeyType.Public)
                throw new ArgumentException("pubKey is not a public key!", "pubKey");

            var res = new CertifiedPublicKey();
            res.PublicKey = pubKey;
            res.NotBefore = res.GetTimestamp(notBefore);
            res.NotAfter = res.GetTimestamp(notAfter);
            return res;
        }

        public static CertifiedPublicKey Create(CryptoKey pubKey, string keyRole, string subjectName, string subjectType)
        {
            var res = Create(pubKey);
            res.KeyRole = keyRole;
            res.SubjectName = subjectName;
            res.SubjectType = subjectType;
            return res;
        }

        public static CertifiedPublicKey Create(CryptoKey pubKey, string keyRole, string subjectName, string subjectType, DateTime expirationDate)
        {
            var res = Create(pubKey);
            res.KeyRole = keyRole;
            res.SubjectName = subjectName;
            res.SubjectType = subjectType;
            res.NotBefore = res.GetTimestamp(DateTime.Now.AddMinutes(-5));
            res.NotAfter = res.GetTimestamp(expirationDate);
            return res;
        }

        public DateTime NotAfterOrMin => NotAfter?.ToDateTime() ?? DateTime.MinValue;
        public DateTime NotBeforeOrMax => NotBefore?.ToDateTime() ?? DateTime.MaxValue;

        public override DataType GetDataType()
        {
            return DataType.CertifiedPublicKey;
        }

        public string Explain()
        {
            return $"KeyRole:'{KeyRole}',SubjectName:'{SubjectName}', PublicKey:[{PublicKey?.Explain()}]";
        }
    }
}
