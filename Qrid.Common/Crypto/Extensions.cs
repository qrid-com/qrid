using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using QP = Qrid.Proto;
using GWK = Google.Protobuf.WellKnownTypes;
using GPB = Google.Protobuf;

using BcMath = Org.BouncyCastle.Math;
using BcCrypto = Org.BouncyCastle.Crypto;
using Generators = Org.BouncyCastle.Crypto.Generators;
using Parameters = Org.BouncyCastle.Crypto.Parameters;
using Signers = Org.BouncyCastle.Crypto.Signers;
using Agreement = Org.BouncyCastle.Crypto.Agreement;
using Encoders = Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using System.IO;

namespace Qrid.Common.Crypto
{
    public static class Extensions
    {
        public static BcCrypto.ICipherParameters AsBouncyEcPublicKeyParams(this CR.CryptoKey pubKey)
        {
            
            var ccKey = CC.EccPublicKey.Create(pubKey);
            return ccKey.AsBouncyEcPublicKeyParams();
        }
        public static BcCrypto.ICipherParameters AsBouncyEcPublicKeyParams(this CC.EccPublicKey pubKey)
        {
            //var x9 = Crypto.EC.CustomNamedCurves.GetByName("curve25519");
            //var ecdp = new Parameters.ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());

            X9ECParameters p = CryptoUtils.GetEccCurve(pubKey.EccCurve);
            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);

            var res = new ECPublicKeyParameters(CryptoUtils.GetPubKeyPoint(pubKey, p.Curve), parameters);

            return res;
        }

        public static BcCrypto.ICipherParameters AsBouncyEcPrivateKeyParams(this CR.CryptoKey privKey)
        {
            var ccKey = CC.EccPrivateKey.Create(privKey);
            return ccKey.AsBouncyEcPrivateKeyParams();
        }

        public static BcCrypto.ICipherParameters AsBouncyEcPrivateKeyParams(this CC.EccPrivateKey privKey)
        {
            //var x9 = Crypto.EC.CustomNamedCurves.GetByName("curve25519");
            //var ecdp = new Parameters.ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());

            X9ECParameters p = CryptoUtils.GetEccCurve(privKey.EccCurve);
            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);

            var res = new ECPrivateKeyParameters(privKey.D.AsBouncyBigInteger(), parameters);

            return res;
        }

        public static BcCrypto.ICipherParameters AsBouncyMlKemPublicKeyParams(this CR.CryptoKey pubKey)
        {
            if (pubKey.KeyType != CR.KeyType.Public)
                throw new ArgumentException($"pubKey.KeyType ({pubKey.KeyType}) != CR.KeyType.Public", "pubKey");

            if(pubKey.KeyAlgo != CR.KeyAlgo.MlKem)
                throw new ArgumentException($"pubKey.KeyAlgo ({pubKey.KeyAlgo}) != CR.KeyAlgo.MlKem", "pubKey");

            var publicKey = MLKemPublicKeyParameters.FromEncoding(MLKemParameters.ml_kem_768, pubKey.KeyBytes.ToByteArray());
            return publicKey;

        }

        public static BcCrypto.ICipherParameters AsBouncyMlKemPrivateKeyParams(this CR.CryptoKey privKey)
        {
            if (privKey.KeyType != CR.KeyType.Private)
                throw new ArgumentException($"privKey.KeyType ({privKey.KeyType}) != CR.KeyType.Private", "privKey");

            if (privKey.KeyAlgo != CR.KeyAlgo.MlKem)
                throw new ArgumentException($"privKey.KeyAlgo ({privKey.KeyAlgo}) != CR.KeyAlgo.MlKem", "privKey");

            var privateKey = MLKemPrivateKeyParameters.FromEncoding(MLKemParameters.ml_kem_768, privKey.KeyBytes.ToByteArray());
            return privateKey;

        }


        public static BigInteger AsBouncyBigInteger(this GPB.ByteString byteString)
        {
            return new BigInteger(byteString.ToByteArray());
        }


    }
}
