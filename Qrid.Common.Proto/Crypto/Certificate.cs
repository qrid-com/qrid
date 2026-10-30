using Google.Protobuf;
using Qrid.Common.Proto.Const;
using System;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Proto.Crypto
{
    public partial class Certificate : ProtoBase
    {
        //public static CertifiedPublicKey GetCertifiedPublicKeyForRootOfTrust()
        //{
        //    // for Root, since it is unsigned, we cannot trust CPK deserialized from whatever was in unsigned CertificateData (do we need to self-sign it with Root PrivKey?)
        //    // instead, we only compare hardcoded PubKey, and the use this function to create the rest of CPK in canonical form
        //    // so this function creates hard-coded canonical version of CPK for Root, with KeyRole that we want for CertHierarchy class to work properly
        //    return CertifiedPublicKey.Create(RootOfTrustPubKey, KeyRole.CA.RootOfTrust, string.Empty, string.Empty);
        //}

        public bool IsRootOfChain
        {
            get
            {
                // CertifyingSig.SigCertificate is NULL -> there is no next level of certs
                // 
                return IssuerCertificate == null;
            }
        }

        public Certificate IssuerCertificate
        {
            get
            {
                return CertifyingSig?.SigCertificate;
            }
        }

        public CR.CryptoKey PublicKey
        {
            get
            {
                return AsCertifiedPublicKey.PublicKey;
            }
        }

        public CR.CertifiedPublicKey AsCertifiedPublicKey
        {
            get
            {
                return TypedData.Create(CertificateData).AsCertifiedPublicKey;
            }
        }

        public Int32 GetCertificateDataHash()
        {
            return Utils.GetHashCode(CertificateData.ToByteArray());
        }

        public static Certificate Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Certificate Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Certificate Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static Certificate Create(CC.CanonicalKey ccPubKey)
        {
            var cpk = CR.CertifiedPublicKey.Create(ccPubKey);
            return Create(cpk);
        }

        public static Certificate Create(CC.CanonicalKey ccPubKey, DateTime notBefore, DateTime notAfter)
        {
            var cpk = CR.CertifiedPublicKey.Create(ccPubKey, notBefore, notAfter);
            return Create(cpk);
        }

        public static Certificate Create(CryptoKey pubKey)
        {
            var cpk = CR.CertifiedPublicKey.Create(pubKey);
            return Create(cpk);
        }

        public static Certificate Create(CryptoKey pubKey, string keyRole, string subjectName, string subjectType)
        {
            var cpk = CR.CertifiedPublicKey.Create(pubKey, keyRole, subjectName, subjectType);
            return Create(cpk);
        }

        public static Certificate Create(CryptoKey pubKey, DateTime notBefore, DateTime notAfter)
        {
            var cpk = CR.CertifiedPublicKey.Create(pubKey, notBefore, notAfter);
            return Create(cpk);
        }

        public static Certificate Create(CertifiedPublicKey certifiedPublicKey)
        {
            var res = new Certificate();
            res.CertificateData = TypedData.Create(certifiedPublicKey).ToByteString();
            return res;
        }

        public string Explain()
        {
            var cpk = AsCertifiedPublicKey;
            return $"AsCertifiedPublicKey:[{cpk?.Explain()}], CertifyingSig:[{CertifyingSig?.Explain()}]";
        }
    }
}