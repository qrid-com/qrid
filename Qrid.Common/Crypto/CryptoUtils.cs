using Google.Protobuf;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Utilities;
using Qrid.Common.Crypto.Certificate;
using Qrid.Common.Http;
using Qrid.Common.Proto;
using Qrid.Common.Proto.CanonicalCrypto;
using Qrid.Common.Proto.Const;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrid.Proto;
using Qrid.Proto.Discovery;
using Qrid.Proto.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Agreement = Org.BouncyCastle.Crypto.Agreement;
using BcCrypto = Org.BouncyCastle.Crypto;
using BcMath = Org.BouncyCastle.Math;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using Generators = Org.BouncyCastle.Crypto.Generators;
using GPB = Google.Protobuf;
using GWK = Google.Protobuf.WellKnownTypes;
using Parameters = Org.BouncyCastle.Crypto.Parameters;
using QCP = Qrid.Common.Proto;
using QP = Qrid.Proto;
using Signers = Org.BouncyCastle.Crypto.Signers;

namespace Qrid.Common.Crypto
{
    public static class CryptoUtils
    {
        #region Root-Of-Trust

        // from Keys_6\RootOfTrust_20260228_101507.pub file
        // todo - make it less exposed, i.e. make it private, ideally find a way to make it IMMUTABLE
        public static readonly CryptoKey RootOfTrustPubKey = CryptoKey.Create("CAEQAxpHCiEAmJ2Eq0Bn0V+whFS9PltEqU9C7nEfEoX5f5gNaEzFlyMSIFBtVRgoUPQzotuQ/WxGBFOq1cnIFXoW3yPiD6iJiu2AGAE=");

        private static byte[] DeprecatedBytes1234 = GetBytesFromUtfString("1234");

        public static bool IsRootOfTrust(this Qrid.Common.Proto.Crypto.Certificate certificate, out string errorMessage, CertifiedPublicKey cpk = null)
        {
            // MAIN RootOfTrust validation function
            // checks if this cert is a self-signed RootOfTrust cert with proper KeyRole

            if (cpk == null) cpk = certificate.AsCertifiedPublicKey;

            // 1) Public Key must be in the list of RootOfTrust keys
            if (!IsRootOfTrustPublicKey(cpk.PublicKey))
            {
                errorMessage = $"Invalid Root-Of-Trust Public Key - certificate contains Public Key that does not belong to Root-Of-Trust list of keys";
                return false;
            }

            // 2) RootOfTrust must be self-signed - verify signature
            if (!CryptoUtils.VerifyCertificateHashSigWithTrustedPubKey(certificate, cpk.PublicKey))
            {
                errorMessage = $"Invalid Root-Of-Trust self-signing signature'";
                return false;
            }
            
            // 3) hardcoded KeyRole for RootOfTrust
            if (cpk.KeyRole != KeyRole.CA.RootOfTrust)
            {
                errorMessage = $"Invalid Root-Of-Trust KeyRole - got KeyRole='{cpk.KeyRole}' while expecting '{KeyRole.CA.RootOfTrust}'";
                return false;
            }

            // all is good
            errorMessage = null;
            return true;
        }

        public static bool IsRootOfTrustPublicKey(CertifiedKeyPair ckp)
        {
            return IsRootOfTrustPublicKey(ckp.KeyPair.PublicKey);
        }

        public static bool IsRootOfTrustPublicKey(CryptoKey key)
        {
            // I've checked protobuf generated code, and .Equals will also do byte-by-byte comparison of .KeyBytes
            return key.Equals(RootOfTrustPubKey);
        }

        #endregion

        

        public static byte[] GetRandomBytes(int length)
        {
            // todo - verify if we use this properly
            // sometimes we generate secret key and (immediately???) generate more random bytes that are PUBLIC (like DekId)
            var random = new SecureRandom();
            var res2 = random.GenerateSeed(length);
            return res2;

        }

        public static string GetRandomBytesToString(int bytesLength)
        {
            byte[] bytes = GetRandomBytes(bytesLength);
            string res = Convert.ToBase64String(bytes);
            return res;
        }

        public static ByteString GetRandomBytesToByteString(int bytesLength)
        {
            byte[] bytes = GetRandomBytes(bytesLength);
            var res = ByteString.CopyFrom(bytes);
            return res;
        }

        public static string GetRandomBytesToStringLength(int stringLength)
        {
            byte[] bytes = GetRandomBytes(stringLength);
            string res = Convert.ToBase64String(bytes).Substring(0, stringLength);
            return res;
        }

        public static string GetRandomLowercaseLetters(int length)
        {
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            StringBuilder res = new StringBuilder(length);
            for (int i = 0; i<length; i++)
                res.Append(lowercaseChars[GetSecureRandomIndex(lowercaseChars)]);
            return res.ToString();
        }

        public static string GetRandomPassword(int length)
        {
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string numericChars = "0123456789";
            const string specialChars = "!@#$%^&*()-_=+[]{};:'\",.<>?/~";

            // Ensure at least one character from each category
            char uppercaseChar = uppercaseChars[GetSecureRandomIndex(uppercaseChars)];
            char lowercaseChar = lowercaseChars[GetSecureRandomIndex(lowercaseChars)];
            char numericChar = numericChars[GetSecureRandomIndex(numericChars)];
            char specialChar = specialChars[GetSecureRandomIndex(specialChars)];

            // Combine all characters 
            string allChars = uppercaseChars + lowercaseChars + numericChars + specialChars;
            
            // Use StringBuilder for efficient string manipulation
            StringBuilder password = new StringBuilder();

            // Add the pre-selected characters
            password.Append(uppercaseChar);
            password.Append(lowercaseChar);
            password.Append(numericChar);
            password.Append(specialChar);

            // Add the remaining characters randomly
            for (int i = 4; i < length; i++)
            {
                int randomIndex = GetSecureRandomIndex(allChars);
                password.Append(allChars[randomIndex]);
            }

            // so the first 4 chars have a certain pattern
            // let's just randomize the string to break that pattern

            var randomizedPassword = RandomizeString(password.ToString());

            return randomizedPassword;
        }

        private static int GetSecureRandomIndex(string source)
        {
            SecureRandom secureRandom = new SecureRandom();
            return secureRandom.Next(source.Length);
        }

        public static string RandomizeString(string input)
        {
            char[] characters = input.ToCharArray();
            Random random = new Random(); // do NOT need a SecureRandom here

            // Fisher-Yates shuffle algorithm
            for (int i = characters.Length - 1; i > 0; i--)
            {
                int randomIndex = random.Next(0, i + 1); // .Next returns up to i, cannot return i+1
                char temp = characters[i];
                characters[i] = characters[randomIndex];
                characters[randomIndex] = temp;
            }

            return new string(characters);
        }

        public static byte[] GetBytesFromUtfString(string utf8String)
        {
            var res = Encoding.UTF8.GetBytes(utf8String);
            return res;
        }

        public static string GetUtfStringFromBytes(byte[] bytes)
        {
            var res = Encoding.UTF8.GetString(bytes);
            return res;
        }

        public static byte[] GetPasswordHash(string password, string saltBase64, KdfParams kdfParams, string derivationDomain)
        {
            // derive 128 bytes
            var res = DeriveBytes(128, password, saltBase64, kdfParams, derivationDomain);
            return res;
        }

        public static byte[] DeriveBytes(int length, string password, string saltBase64, KdfParams kdfParams, string derivationDomain)
        {
            // why dont we put derivationDomain into KdfParamsArgon2.AssociatedData?
            // b/c 1) derivationDomain is expected to be constant value that rarely if ever need to be changed, even in future versions
            // and 2) KdfParams is serialized and stored, so I did not feel like in this case it makes sense to store the const in there, increasing its size
            // and 3) if stored, then theoretically it could be manipulated; not stored - then one thing less to warry about
            var combinedPassword =  derivationDomain + password; // I dont think we need a separator byte here
            switch(kdfParams.KdfType)
            {
                case KdfType.Argon2Id:
                    return DeriveBytesArgon2(length, combinedPassword, saltBase64, KdfParamsArgon2.Create(kdfParams.KdfParams_), Argon2Parameters.Argon2id);
                case KdfType.Argon2I:
                    return DeriveBytesArgon2(length, combinedPassword, saltBase64, KdfParamsArgon2.Create(kdfParams.KdfParams_), Argon2Parameters.Argon2i);
                default:
                    throw new ArgumentException($"Not supported KdfType='{kdfParams.KdfType}'");
            }
        }

        public static byte[] DeriveBytesArgon2(int length, string password, string saltBase64, KdfParamsArgon2 kdfParams, int argonType)
        {
            byte[] passwordBytes = GetBytesFromUtfString(password);
            byte[] saltBytes = Convert.FromBase64String(saltBase64);
            byte[] associatedData = (kdfParams.AssociatedData ?? ByteString.Empty).ToByteArray();

            var parameters = new Argon2Parameters.Builder(argonType)
                .WithSalt(saltBytes)
                .WithParallelism(kdfParams.Parallelism)        // Number of threads
                .WithMemoryPowOfTwo(kdfParams.MemoryPowerOf2)  // Value of 16 would mean 2^16 Kb = 64 Mb
                .WithIterations(kdfParams.Iterations)          // Number of iterations
                .WithAdditional(associatedData)                // AssociatedData
                .Build();

            var generator = new Argon2BytesGenerator();
            generator.Init(parameters);

            byte[] output = new byte[length]; // derive length bytes
            generator.GenerateBytes(passwordBytes, output, 0, output.Length);

            return output;
        }

        public static CryptoKey DeriveAesKey(string password, string saltBase64, KdfParams kdfParams, string derivationDomain)
        {
            var keyBytes = DeriveBytes(32, password, saltBase64, kdfParams, derivationDomain);
            var res = CR.CryptoKey.CreateAes(keyBytes);
            return res;
        }

        public static string GetSecCurveName(CR.EccCurve eccCurve)
        {
            switch (eccCurve)
            {
                case CR.EccCurve.Secp256R1:
                    return "secp256r1";
                case CR.EccCurve.Secp384R1:
                    return "secp384r1";
                default:
                    return null; // let's not throw here, let it throw in GetEccCurve, but only if it is in the call stack
            }
        }
        public static X9ECParameters GetEccCurve(CR.EccCurve eccCurve)
        {
            var secCurveName = GetSecCurveName(eccCurve);
            var res = SecNamedCurves.GetByName(secCurveName);
            return res;
        }

        public static CC.EccKeyPair DeriveEccKeyPair(string password, string saltBase64, CR.EccCurve eccCurve)
        {
            X9ECParameters p = GetEccCurve(eccCurve);

            var res = new CC.EccKeyPair();
            res.PrivateKey = DeriveEccPrivateKey(password, saltBase64, eccCurve);
            res.PublicKey = CalculateEccPublicKey(res.PrivateKey);
            return res;
        }

        public static CC.EccPrivateKey DeriveEccPrivateKey(string password, string saltBase64, CR.EccCurve eccCurve)
        {

            byte[] passwordBytes = GetBytesFromUtfString(password);
            byte[] saltBytes = Convert.FromBase64String(saltBase64);
            byte[] userUuidBytes = GetBytesFromUtfString("1234");

            // TODO - change from Argon2i to Argon2id
            var argonParameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2i)
                .WithSalt(saltBytes)
                .WithParallelism(2)             // Number of threads
                .WithMemoryAsKB(8192)           // 8 MB memory cost - TODO - increase to 64 Mb
                .WithIterations(3)              // Number of iterations
                .WithAdditional(userUuidBytes)  // AssociatedData
                .Build();

            X9ECParameters p = GetEccCurve(eccCurve);
            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
            var numBytes = p.N.ToByteArrayUnsigned().Length;

            var generator = new Argon2BytesGenerator();
            generator.Init(argonParameters);

            byte[] keyCandidateBytes = new byte[numBytes]; // derive numBytes bytes, should be around 32 bytes = 256 bit
            generator.GenerateBytes(passwordBytes, keyCandidateBytes, 0, keyCandidateBytes.Length);

            var biKeyCandidate = new BigInteger(1, keyCandidateBytes);

            // this is how BouncyCastle validates ECC PrivateKey D against N:
            // private key D must be in the interval [1, n - 1]

            var biPrivateKey = biKeyCandidate.Mod(p.N); // make sure D does not exceed N

            if (biPrivateKey.CompareTo(BigInteger.One) < 0)
                throw new ApplicationException("possibility of this ever happening is extemelly low but if you see this then please try some different password ");

            parameters.ValidatePrivateScalar(biPrivateKey); // should not throw, b/c we have taken precausions above, but still let's call it here

            var res = new CC.EccPrivateKey();
            res.D = GPB.ByteString.CopyFrom(biPrivateKey.ToByteArray());
            res.EccCurve = eccCurve;

            return res;
        }

        [Conditional("DEBUG")]
        public static void DebugCompare(BigInteger a, BigInteger b, string labelA, string labelB)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{labelA}.CompareTo({labelB}) = {a.CompareTo(b)}");
            sb.AppendLine($"{labelA}.SignValue = {a.SignValue}, {labelB}.SignValue = {b.SignValue}");
            sb.AppendLine($"{labelA}.BitLength = {a.BitLength}, {labelB}.BitLength = {b.BitLength}");
            string s = sb.ToString();
            Console.WriteLine(s);
        }

        public static CC.EccPublicKey CalculateEccPublicKey(CC.EccPrivateKey ccPrivKey)
        {
            var res = new CC.EccPublicKey();
            res.EccCurve = ccPrivKey.EccCurve;
            X9ECParameters p = GetEccCurve(ccPrivKey.EccCurve);
            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
            var biPrivateKey = new BigInteger(1, ccPrivKey.D.ToByteArray());

            DebugCompare(biPrivateKey, p.N, "biPrivateKey", "p.N");

            if (biPrivateKey.CompareTo(p.N) != -1)
            {
                throw new ApplicationException("we have created invalid ECC PrivateKey: d > n which is wrong");
            }

            ECPrivateKeyParameters privKey = new ECPrivateKeyParameters(biPrivateKey, parameters);

            // calculate PubKey from PrivKey: Q = d*G
            var q = p.G.Multiply(new BigInteger(1, ccPrivKey.D.ToByteArray())).Normalize(); // todo - do we need .Normalize()??

            ECPublicKeyParameters pubKey = new ECPublicKeyParameters(q, parameters);
            var pubKeyXBytes = pubKey.Q.XCoord.ToBigInteger().ToByteArray();
            var pubKeyYBytes = pubKey.Q.YCoord.ToBigInteger().ToByteArray();

            res.XQ = GPB.ByteString.CopyFrom(pubKeyXBytes);
            res.YQ = GPB.ByteString.CopyFrom(pubKeyYBytes);

            return res;
        }

        public static CR.CryptoKey GetRandomSymKey()
        {
            // get random AES-256 key
            var symBytes = GetRandomBytes(32);
            var symKey = new CR.CryptoKey();
            symKey.KeyAlgo = CR.KeyAlgo.Aes;
            symKey.KeyBytes = GPB.ByteString.CopyFrom(symBytes);
            return symKey;
        }

        public static CC.EccPrivateKey GetRandomEccPrivateKey(CR.EccCurve eccCurve)
        {

            X9ECParameters p = GetEccCurve(eccCurve);
            ECDomainParameters parameters = new ECDomainParameters(p.Curve, p.G, p.N, p.H);
            var numBytes = p.N.ToByteArrayUnsigned().Length;

            var keyCandidateBytes = GetRandomBytes(numBytes); // should be around 32 bytes = 256 bit

            var biKeyCandidate = new BigInteger(1, keyCandidateBytes);

            // this is how BouncyCastle validates ECC PrivateKey D against N:
            // private key D must be in the interval [1, n - 1]

            var biPrivateKey = biKeyCandidate.Mod(p.N); // make sure D does not exceed N

            if (biPrivateKey.CompareTo(BigInteger.One) < 0)
                throw new ApplicationException("possibility of this ever happening is extemelly low but if you see this then please try some different password ");

            parameters.ValidatePrivateScalar(biPrivateKey); // should not throw, b/c we have taken precausions above, but still let's call it here

            var res = new CC.EccPrivateKey();
            res.D = GPB.ByteString.CopyFrom(biPrivateKey.ToByteArray());
            res.EccCurve = eccCurve;

            return res;
        }

        public static CC.EccKeyPair GetRandomEccKeyPair()
        {
            // for now, just get random ECC P-256 key pair
            return GetRandomEccKeyPair(CR.EccCurve.Secp256R1);
        }

        public static CR.CryptoKeyPair GetRandomAsymCryptoKeyPair(bool postQuantum = false)
        {
            var eccPair = CR.CryptoKeyPair.Create(GetRandomEccKeyPair(CR.EccCurve.Secp256R1));
            if (!postQuantum) return eccPair;
            
            // PQC requested - use Hybrid ML-KEM + ECC
            var mlkPair = GetRandomMlKemKeyPair();
            var hybridPair = CR.CryptoKeyPair.CreateWith(eccPair, mlkPair);
            return hybridPair;
        }

        public static CR.CryptoKeyPair GetRandomMlKemKeyPair()
        {
            var rng = new SecureRandom();

            // 1) ML-KEM keypair
            var kemKeyGen = new MLKemKeyPairGenerator();
            kemKeyGen.Init(new MLKemKeyGenerationParameters(rng, MLKemParameters.ml_kem_768));
            AsymmetricCipherKeyPair kemPair = kemKeyGen.GenerateKeyPair();
            var mlKemPrivate = ((MLKemPrivateKeyParameters)kemPair.Private).GetEncoded();
            var mlKemPublic = ((MLKemPublicKeyParameters)kemPair.Public).GetEncoded();

            var res = new CR.CryptoKeyPair();
            res.PrivateKey = CryptoKey.CreateWith(mlKemPrivate, KeyAlgo.MlKem, KeyType.Private);
            res.PublicKey = CryptoKey.CreateWith(mlKemPublic, KeyAlgo.MlKem, KeyType.Public);
            return res;
        }

        public static CC.EccKeyPair GetRandomEccKeyPair(CR.EccCurve eccCurve)
        {
            var res = new CC.EccKeyPair();
            res.PrivateKey = GetRandomEccPrivateKey(eccCurve);
            res.PublicKey = CalculateEccPublicKey(res.PrivateKey);
            return res;
        }

        // TODO - consider using this _Proper instead of the above
        public static CC.EccKeyPair GetRandomEccKeyPair_Proper(CR.EccCurve eccCurve)
        {
            var rng = new SecureRandom();

            // EC P-256 (secp256r1) keypair
            X9ECParameters x9 = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256r1");
            var ecDomain = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());
            var gen = new ECKeyPairGenerator();
            gen.Init(new ECKeyGenerationParameters(ecDomain, rng));
            var ecPair = gen.GenerateKeyPair();
            var ecPrivate = (ECPrivateKeyParameters)ecPair.Private;
            var ecPublic = (ECPublicKeyParameters)ecPair.Public;


            var res = new CC.EccKeyPair();
            res.PrivateKey = null; // TODO - use ecPrivate
            res.PublicKey = null;  // TODO - use ecPublic
            return res;
        }


        public static CR.HashSig Sign(byte[] data, CR.CryptoKey privKey)
        {
            var ccKey = CC.EccPrivateKey.Create(privKey);

            //var x9 = Crypto.EC.CustomNamedCurves.GetByName("curve25519");
            //var ecdp = new Parameters.ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());

            X9ECParameters p = GetEccCurve(ccKey.EccCurve);
            ECDomainParameters parameters = new Parameters.ECDomainParameters(p.Curve, p.G, p.N, p.H);


            Parameters.ECPrivateKeyParameters privKeyParams;
            privKeyParams = new ECPrivateKeyParameters(new BigInteger(1, ccKey.D.ToByteArray()), parameters);

            var signer = new Signers.ECDsaSigner();
            signer.Init(true, privKeyParams);

            // prepare Hash
            var cryptoHash = GetHash(data);
            var cryptoHashBytes = cryptoHash.ToByteArray();

            // sign the HashBytes
            // ??? should we sign entire ProtoBuf CryptoHash object or just CryptoHash.HashBytes?
            // if we just sign CryptoHash.HashBytes then in the future if for some reason we would want to add
            // some new field to CryptoHash definition, we do NOT want to break the existing signatres / certificates etc 
            // if the sign entire CryptoHash, then it feels a bit more secure to protect from this scenario
            // - attacker updates hash algo field from Sha3 to MD5
            // - attacker finds finds MD5 preimage matching what is already signed
            // - and on top of that we forget to check expected hash type somewhere in our code

            // we dont support MD5 but still let's just sign the entire CryptoHash object (i.e. including hash type)
            BigInteger[] signatureArray = signer.GenerateSignature(cryptoHashBytes);  // return new BigInteger[]{ r, s };

            var ccSig = new CC.SigEcDsa();
            ccSig.R = GPB.ByteString.CopyFrom(signatureArray[0].ToByteArray());
            ccSig.S = GPB.ByteString.CopyFrom(signatureArray[1].ToByteArray());

            var crSig = new CR.CryptoSig();
            crSig.SigType = CR.SigType.EcDsa;
            crSig.SigBytes = ccSig.GetByteString();

            var res = HashSig.CreateWith(cryptoHash, crSig);
            return res;
        }

        public static string GetPubKeyHashId(CR.CryptoKey pubKey)
        {
            // not to be used for verifying keys by humans
            // intended to be used to identify PubKey in data structures without explicitly specifying PubKey
            // also let's make it safe for FILE NAMES and for URLs
            // so we have selected an algo (SHA3x256) with long Id length but extremely low collision chance
            // not to be used like key fingerprint (i.e. not to be used for verifying keys by humans)
            // this function was originally intended to be used in QRS QrCommand protocol
            if (pubKey.KeyType != KeyType.Public)
                throw new InvalidParameterException($"only Public Keys are supported by GetPubKeyHashId(). Received pubKey.KeyType = '{pubKey.KeyType}' while expecting 'KeyType.Public'");

            var hash = GetHash(pubKey.ToByteArray(), HashAlgo.Sha3X256);

            // make it safe for FILE NAMES and for URLs
            return hash.GetBase64Safe();
        }

        // GPB.ByteString data
        public static CR.CryptoHash GetHash(GPB.ByteString data)
        {
            return GetHash(data.ToByteArray());
        }

        public static CR.CryptoHash GetHash(GPB.ByteString data, HashAlgo hashAlgo)
        {
            return GetHash(data.ToByteArray(), hashAlgo);
        }

        public static CR.CryptoHash GetHash(byte[] data)
        {
            return GetHash(data, HashAlgo.Sha3X512);
        }

        public static CR.CryptoHash GetHash(byte[] data, HashAlgo hashAlgo)
        {
            IDigest initializedDigest = GetDigestImplementation(hashAlgo);
            var hashBytes = GetHashBytes(data, initializedDigest);
            return CR.CryptoHash.CreateHash(hashBytes, hashAlgo);
        }

        public static IDigest GetDigestImplementation(HashAlgo hashAlgo)
        {
            switch (hashAlgo)
            {
                case HashAlgo.Sha3X512:
                    return new Org.BouncyCastle.Crypto.Digests.Sha3Digest(512);
                case HashAlgo.Sha3X256:
                    return new Org.BouncyCastle.Crypto.Digests.Sha3Digest(256);
                case HashAlgo.Sha2X256:
                    return new Org.BouncyCastle.Crypto.Digests.Sha256Digest();
                default:
                    throw new ArgumentException($"Unsupported hash algorithm '{hashAlgo}'");
            }
        }
        public static byte[] GetHashBytes(byte[] data, IDigest initializedDigest)
        {
            initializedDigest.BlockUpdate(data, 0, data.Length);
            byte[] hashBytes = new byte[initializedDigest.GetDigestSize()]; // in bytes
            initializedDigest.DoFinal(hashBytes, 0);
            return hashBytes;
        }

        public static CR.HashSig Sign(ByteString data, CR.CryptoKey privKey)
        {
            return Sign(data.ToByteArray(), privKey);
        }


        public static string GetTunnelHashId(byte[] tunnelPubKeyBytes)
        {
            // Hash of PubKey but also a Tunnel Id
            var hash = GetHash(tunnelPubKeyBytes, HashAlgo.Sha3X256);
            // make it safe for FILE NAMES and for URLs
            return hash.GetBase64Safe();
        }

        #region HMAC

        public static CR.CryptoHmac GetHmac(GPB.ByteString data, byte[] key)
        {
            return GetHmac(data.ToByteArray(), key);
        }

        public static CR.CryptoHmac GetHmac(GPB.ByteString data, byte[] key, HashAlgo hashAlgo)
        {
            return GetHmac(data.ToByteArray(), key, hashAlgo);
        }

        public static CR.CryptoHmac GetHmac(byte[] data, byte[] key)
        {
            return GetHmac(data, key, HashAlgo.Sha3X256);
        }

        public static CR.CryptoHmac GetHmac(byte[] data, byte[] key, HashAlgo hashAlgo)
        {
            HMac hmac = new HMac(GetDigestImplementation(hashAlgo));
            hmac.Init(new KeyParameter(key));
            hmac.BlockUpdate(data, 0, data.Length);
            byte[] hmacBytes = new byte[hmac.GetMacSize()];
            hmac.DoFinal(hmacBytes, 0);

            return CR.CryptoHmac.CreateHmac(hmacBytes, hashAlgo);
        }

        public static CR.HmacBytes GetHmacBytes(byte[] data, byte[] key, HashAlgo hashAlgo = HashAlgo.Sha3X256)
        {
            var res = new CR.HmacBytes();
            res.Hmac = GetHmac(data, key, hashAlgo);
            res.Bytes = ByteString.CopyFrom(data);
            return res;
        }

        public static CR.HmacBytes GetHmacBytes(ByteString data, byte[] key, HashAlgo hashAlgo = HashAlgo.Sha3X256)
        {
            var res = new CR.HmacBytes();
            res.Hmac = GetHmac(data, key, hashAlgo);
            res.Bytes = data;
            return res;
        }

        public static CR.HmacBytes GetHmacBytes(ProtoBase data, byte[] key, HashAlgo hashAlgo = HashAlgo.Sha3X256)
        {
            var res = new CR.HmacBytes();
            res.Hmac = GetHmac(data.GetByteArray(), key, hashAlgo);
            res.Bytes = data.GetByteString();
            return res;
        }

        public static bool IsValidHmac(byte[] data, byte[] key, CR.CryptoHmac hmac)
        {
            var newHmac = GetHmac(data, key, hmac.HashAlgo);
            return newHmac.Equals(hmac);
        }

        public static bool IsValidHmac(CR.HmacBytes hmacBytes, byte[] key)
        {
            return IsValidHmac(hmacBytes.Bytes.ToByteArray(), key, hmacBytes.Hmac);
        }

        #endregion



        public static BcMath.EC.ECPoint GetPubKeyPoint(CC.EccPublicKey pubKey, BcMath.EC.ECCurve curve)
        {
            var res = curve.CreatePoint(
                new BigInteger(1, pubKey.XQ.ToByteArray()),
                new BigInteger(1, pubKey.YQ.ToByteArray())
                );
            return res;
        }

        #region Verify ================================================================

        public static bool VerifyCertSignedBytes(CR.CertSignedBytes csb, out CertContext certContext)
        {
            var res = VerifyCertifiedSig(csb.Bytes.ToByteArray(), csb.Sig, out certContext);
            return res && (certContext?.IsValid ?? false);
        }

        public static bool VerifyCertSignedBytes(CR.CertSignedBytes csb, string keyRolePath, out CertContext certContext)
        {
            var res = VerifyCertifiedSig(csb.Bytes.ToByteArray(), csb.Sig, keyRolePath, out certContext);
            return res && (certContext?.IsValid ?? false);
        }

        public static bool VerifySignedBytesWithCertificate(CR.SignedBytes sb, CR.Certificate certificate, out CertContext certContext)
        {
            var certifiedSig = CR.CertifiedSig.Create(sb.Sig, certificate);
            var res = VerifyCertifiedSig(sb.Bytes.ToByteArray(), certifiedSig, out certContext);
            return res && (certContext?.IsValid ?? false);
        }

        public static bool VerifyCertifiedSig(byte[] data, CR.CertifiedSig sig, out CertContext certContext)
        {
            return VerifyCertifiedSig(data, sig, null, out certContext);
        }

        public static bool VerifyCertifiedSig(byte[] data, CR.CertifiedSig sig, string keyRolePath, out CertContext certContext)
        {
            var sigRes = VerifyHashSig(data, sig.HashSig, sig.SigCertificate.PublicKey);
            if (sigRes)
            {
                // sig is good, let's validate the cert now
                return VerifyCertificate(sig.SigCertificate, keyRolePath, out certContext);
            }
            else
            {
                // sig is not good, so no need to validate cert
                certContext = new CertContext();
                certContext.ErrorMessage = "Invalid signature in CertifiedSig";
                certContext.IsValid = false;
                return false;
            }
        }


        // ???
        //public static bool Verify(GPB.ByteString data, CR.CertifiedSig sig)
        //{
        //    var bytes = data.ToByteArray();
        //    return VerifyHashSig(bytes, sig.HashSig, sig.SigCertificate.PublicKey);
        //}

        public static bool VerifyCertifiedKeyPair(CR.CertifiedKeyPair ckp, out CertContext certContext)
        {
            certContext = new CertContext();
            // compare PubKeys
            if (!ckp.KeyPair.PublicKey.Equals(ckp.Certificate.PublicKey))
            {
                certContext.IsValid = false;
                certContext.ErrorMessage = "Certificate is not matching the KeyPair - Public Keys in KeyPair and its Certificate are not the same";
                return false;
            }

            // validate Cert
            certContext.Validate(ckp.Certificate, CertCache.Instance);
            return certContext.IsValid;
        }
        public static bool VerifyCertificate(CR.Certificate cert, out CertContext certContext)
        {
            certContext = new CertContext();
            certContext.Validate(cert, CertCache.Instance);
            return certContext.IsValid;
        }

        public static bool VerifyCertificate(CR.Certificate cert, string keyRolePath, out CertContext certContext)
        {
            var res = VerifyCertificate(cert, out certContext);
            if (res && keyRolePath != null)
            {
                // extra check expected key roles path from root to leaf
                if (certContext.KeyRolePath != keyRolePath)
                {
                    res = false;
                    certContext.IsValid = false;
                    certContext.ErrorMessage = $"Invalid KeyRolePath - got '{certContext.KeyRolePath}' while expecting '{keyRolePath}'";
                }
            }
            return res;
        }

        public static bool VerifyCertificate(CR.Certificate cert, string keyRolePath, string fullUsername, string domain, string app, out CertContext certContext)
        {
            var res = VerifyCertificate(cert, out certContext);
            if (!res) return false;
            // check expected key roles path from root to leaf
            if (keyRolePath != null && certContext.KeyRolePath != keyRolePath)
            {
                certContext.IsValid = false;
                certContext.ErrorMessage = $"Invalid KeyRolePath - got '{certContext.KeyRolePath}' while expecting '{keyRolePath}'";
                return false;
            }
            // check expected user
            if (fullUsername !=null && certContext.Subjects.User != fullUsername)
            {
                certContext.IsValid = false;
                certContext.ErrorMessage = $"Invalid user - got '{certContext.Subjects.User}' while expecting '{fullUsername}'";
                return false;
            }

            var effectiveDomain = domain;

            if (fullUsername != null)
            {
                if (!SharedUtils.ParseFullUserName(fullUsername, out var parsedUser, out var parsedDomain))
                {
                    certContext.IsValid = false;
                    certContext.ErrorMessage = $"Cannot parse username '{fullUsername}' - must be in 'user@domain' format";
                    return false;
                }

                if (domain != null && parsedDomain != domain)
                {
                    certContext.IsValid = false;
                    certContext.ErrorMessage = $"Domain '{domain}' must be the same as domain in username '{fullUsername}'";
                    return false;
                }
                
                effectiveDomain = parsedDomain;
            }

            // check domain
            if (effectiveDomain != null && certContext.Subjects.Domain != effectiveDomain)
            {
                certContext.IsValid = false;
                certContext.ErrorMessage = $"Invalid domain - got '{certContext.Subjects.Domain}' while expecting '{effectiveDomain}'";
                return false;
            }
            // check app
            if (app != null && certContext.Subjects.App != app)
            {
                certContext.IsValid = false;
                certContext.ErrorMessage = $"Invalid App - got '{certContext.Subjects.App}' while expecting '{app}'";
                return false;
            }

            // TODO MUSTDO - what about Subjects.Server ???

            return true;
        }

        public static bool VerifyCertifiedKeyPair(CR.CertifiedKeyPair ckp, string keyRolePath, string fullUsername, string domain, string app, out CertContext certContext)
        {
            var res = VerifyCertificate(ckp.Certificate, keyRolePath, fullUsername, domain, app, out certContext);
            if (!res) return false;
            // check the PubKey in CKP matching PubKey in cert
            // compare PubKeys
            if (!ckp.KeyPair.PublicKey.Equals(ckp.Certificate.PublicKey))
            {
                certContext.IsValid = false;
                certContext.ErrorMessage = "Invalid CertifiedKeyPair - Public Keys in KeyPair and its Certificate are not the same";
                return false;
            }
            return true;
        }

        #region IsValidCertificate

        // Reputation
        public static bool IsValidReputationTokenSigner(CR.Certificate cert, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.ReputationTokenSigner, out certContext);
        }

        // Server
        public static bool IsValidEnclaveRead(CR.Certificate cert, string domain, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.EnclaveRead, null, domain, null, out certContext); 
        }

        public static bool IsValidEmailerRead(CR.Certificate cert, string domain, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.EmailerRead, null, domain, null, out certContext);
        }

        // MA - user level certs
        public static bool IsValidAccountCertificate(CR.Certificate cert, string fullUsername, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.AccountCertificate, fullUsername, null, null, out certContext); // will take Domain from username; does NOT have App
        }
        
        public static bool IsValidMobileTunnelReceiver(CR.Certificate cert, string fullUsername, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.MobileTunnelReceiver, fullUsername, null, null, out certContext); // will take Domain from username; does NOT have App
        }
        
        public static bool IsValidMobileApiAccess(CR.Certificate cert, string fullUsername, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.MobileApiAccess, fullUsername, null, null, out certContext);
        }

        // (user + app) level certs
        public static bool IsValidMobileAsymKeyWrap(CR.Certificate cert, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.MobileAsymKeyWrap, fullUsername, null, app, out certContext);
        }

        // Client app certs
        public static bool IsValidClientApiAccess(CR.Certificate cert, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.ClientApiAccess, fullUsername, null, app, out certContext);
        }
        public static bool IsValidClientDataSign(CR.Certificate cert, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.ClientDataSign, fullUsername, null, app, out certContext);
        }
        public static bool IsValidClientMessageRead(CR.Certificate cert, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.ClientMessageRead, fullUsername, null, app, out certContext);
        }
        public static bool IsValidClientMessageSign(CR.Certificate cert, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertificate(cert, KeyRolePath.ClientMessageSign, fullUsername, null, app, out certContext);
        }

        #endregion

        #region IsValid CertifiedKeyPair

        // Reputation
        public static bool IsValidReputationTokenSigner(CR.CertifiedKeyPair ckp, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.ReputationTokenSigner, null, null, null, out certContext);
        }

        // Domain
        public static bool IsValidDomainAccounts(CR.CertifiedKeyPair ckp, string domain, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.DomainAccounts, null, domain, null, out certContext);
        }

        // Server
        public static bool IsValidEnclaveRead(CR.CertifiedKeyPair ckp, string domain, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.EnclaveRead, null, domain, null, out certContext);
        }

        public static bool IsValidEmailerRead(CR.CertifiedKeyPair ckp, string domain, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.EmailerRead, null, domain, null, out certContext);
        }

        // MA - user level certs
        public static bool IsValidAccountCertificate(CR.CertifiedKeyPair ckp, string fullUsername, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.AccountCertificate, fullUsername, null, null, out certContext); // will take Domain from username; does NOT have App
        }

        public static bool IsValidMobileTunnelReceiver(CR.CertifiedKeyPair ckp, string fullUsername, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.MobileTunnelReceiver, fullUsername, null, null, out certContext); // will take Domain from username; does NOT have App
        }

        public static bool IsValidMobileApiAccess(CR.CertifiedKeyPair ckp, string fullUsername, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.MobileApiAccess, fullUsername, null, null, out certContext);
        }

        // (user + app) level certs
        public static bool IsValidMobileAsymKeyWrap(CR.CertifiedKeyPair ckp, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.MobileAsymKeyWrap, fullUsername, null, app, out certContext);
        }

        // Client app certs
        public static bool IsValidClientApiAccess(CR.CertifiedKeyPair ckp, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.ClientApiAccess, fullUsername, null, app, out certContext);
        }
        public static bool IsValidClientDataSign(CR.CertifiedKeyPair ckp, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.ClientDataSign, fullUsername, null, app, out certContext);
        }
        public static bool IsValidClientMessageRead(CR.CertifiedKeyPair ckp, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.ClientMessageRead, fullUsername, null, app, out certContext);
        }
        public static bool IsValidClientMessageSign(CR.CertifiedKeyPair ckp, string fullUsername, string app, out CertContext certContext)
        {
            return VerifyCertifiedKeyPair(ckp, KeyRolePath.ClientMessageSign, fullUsername, null, app, out certContext);
        }

        #endregion



        public static bool VerifyCertificateHashSig(CR.Certificate cert)
        {
            // CertifyingSig was created by Issuer using CertifyingSig.SigCertificate.PublicKey
            // CertifyingSig signs CertificateData

            if (cert.CertifyingSig == null)
                return false;

            return CryptoUtils.VerifyHashSig(cert.CertificateData, cert.CertifyingSig.HashSig, cert.CertifyingSig.SigCertificate.PublicKey);

        }

        public static bool VerifyCertificateHashSigWithTrustedPubKey(CR.Certificate cert, CR.CryptoKey trustedPubKey)
        {
            // this is used to verify self-signed ROOT certificate that comes last at the end of the cert chain 

            if (cert.CertifyingSig == null)
                return false;

            return CryptoUtils.VerifyHashSig(cert.CertificateData, cert.CertifyingSig.HashSig, trustedPubKey);

        }

        public static bool VerifyHashSig(GPB.ByteString data, CR.HashSig sig, CR.CryptoKey pubKey)
        {
            var bytes = data.ToByteArray();
            return VerifyHashSig(bytes, sig, pubKey);  
        }

        public static bool VerifyHashSig(byte[] data, CR.HashSig sig, CR.CryptoKey pubKey)
        {
            var ccKey = CC.EccPublicKey.Create(pubKey);

            //var x9 = Crypto.EC.CustomNamedCurves.GetByName("curve25519");
            //var ecdp = new Parameters.ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());

            X9ECParameters p = GetEccCurve(ccKey.EccCurve);
            ECDomainParameters parameters = new Parameters.ECDomainParameters(p.Curve, p.G, p.N, p.H);

            //p.Curve.CreatePoint()

            Parameters.ECPublicKeyParameters pubKeyParams;
            pubKeyParams = new ECPublicKeyParameters(GetPubKeyPoint(ccKey, p.Curve), parameters);

            var signer = new Signers.ECDsaSigner();
            signer.Init(false, pubKeyParams); // false means for verifying, i.e. not for signing

            // TODO MUSTDO - properly implement different sig types!
            if (sig.Sig.SigType != SigType.EcDsa) throw new ApplicationException($"Attempt to verify unsupported SigType = '{sig.Sig.SigType}'"); 
            var ccSig = CC.SigEcDsa.Create(sig.Sig.SigBytes.ToByteArray());

            // 1. calculate Hash of data
            var oldHash = CryptoHash.Create(sig.CryptoHashBytes);
            
            // 2. compare if Hash matches old Hash (stored in sig) byte-by-byte
            if (!VerifyHash(data, oldHash))
            {
                // hash bytes do NOT match!
                // TODO - create overloaded method with "out errorCode" that would signal if Hash or Sig was the problem
                return false;
            }

            // 3. verify crypto sig on crypto hash bytes
            var res = signer.VerifySignature(
                sig.CryptoHashBytes.ToByteArray(),
                new BigInteger(1, ccSig.R.ToByteArray()),
                new BigInteger(1, ccSig.S.ToByteArray())
                );

            return res;
        }

        public static bool VerifyHash(byte[] data, CryptoHash oldHash)
        {
            var newHash = GetHash(data, oldHash.HashAlgo);
            return newHash.IsEqualHash(oldHash);
        }

        public static bool VerifyHash(ByteString data, CryptoHash oldHash)
        {
            var newHash = GetHash(data, oldHash.HashAlgo);
            return newHash.IsEqualHash(oldHash);
        }

        public static bool VerifyEcDsa(string text, BigInteger[] sig, Parameters.ECPrivateKeyParameters pubKey)
        {
            var message = Encoding.UTF8.GetBytes(text);

            var signer = new Signers.ECDsaSigner();
            signer.Init(false, pubKey);
            var res = signer.VerifySignature(message, sig[0], sig[1]);  // return new BigInteger[]{ r, s };

            return res;
        }

        #endregion

        #region Explain

        public static bool ExplainCertificate(CR.Certificate cert, out CertContext certContext)
        {
            certContext = new CertContext();
            certContext.Explain(cert);
            return certContext.IsValid;
        }

        #endregion

        public static CR.SignedBytes SignBytes(byte[] data, CR.CryptoKey privKey)
        {
            var res = new CR.SignedBytes();
            res.Sig = Sign(data, privKey);
            res.Bytes = GPB.ByteString.CopyFrom(data);
            return res;
        }

        public static CR.SignedBytes SignBytes(GPB.ByteString data, CR.CryptoKey privKey)
        {
            var res = new CR.SignedBytes();
            res.Sig = Sign(data.ToByteArray(), privKey);
            res.Bytes = data;
            return res;
        }

        public static CR.CertifiedSig SignCertified(GPB.ByteString data, CR.CryptoKey privKey, CR.Certificate certificate)
        {
            return SignCertified(data.ToByteArray(), privKey, certificate);
        }

        public static CR.CertifiedSig SignCertified(byte[] data, CR.CryptoKey privKey, CR.Certificate certificate)
        {
            // todo - verify that privKey is a valid key for PubKey that is in Certificate

            var res = new CR.CertifiedSig();
            res.HashSig = Sign(data, privKey);
            res.SigCertificate = certificate;

            return res;
        }

        public static CR.CertifiedSig SignCertified(byte[] data, CR.CertifiedKeyPair certifiedKeyPair)
        {
            return SignCertified(data, certifiedKeyPair.KeyPair.PrivateKey, certifiedKeyPair.Certificate);
        }

        public static CR.CertifiedSig SignCertified(GPB.ByteString data, CR.CertifiedKeyPair certifiedKeyPair)
        {
            return SignCertified(data, certifiedKeyPair.KeyPair.PrivateKey, certifiedKeyPair.Certificate);
        }

        public static void SignCertified(CR.CertSignedBytes csb, CR.CertifiedKeyPair certifiedKeyPair)
        {
            csb.Sig = SignCertified(csb.Bytes, certifiedKeyPair.KeyPair.PrivateKey, certifiedKeyPair.Certificate);
        }

        public static CR.CertSignedBytes SignCertified(ProtoBase obj, CR.CertifiedKeyPair certifiedKeyPair)
        {
            var res = new CR.CertSignedBytes();
            res.Bytes = obj.GetByteString();
            res.Sig = SignCertified(res.Bytes, certifiedKeyPair.KeyPair.PrivateKey, certifiedKeyPair.Certificate);
            return res;
        }

        public static string SignQridConfiguration(QridConfiguration qridConfig, CertifiedKeyPair certifiedKeyPair)
        {
            // sign domain config
            var configJson = System.Text.Json.JsonSerializer.Serialize(qridConfig);
            var config = StringObject.CreateWithString(configJson);
            var signedConfig = CryptoUtils.SignCertified(config, certifiedKeyPair);
            var res = signedConfig.GetBase64String();
            return res;
        }

        public static QridConfiguration VerifyQridConfiguration(string qridConfigBase64, string domain, out string errorMessage)
        {
            var signedConfig = CR.CertSignedBytes.Create(qridConfigBase64);

            // TODO - make sure this is only avail in DEBUG build and not avail in RELEASE build
            var isDomainName = !SharedUtils.IsIpAddressV4(domain); // false when domain param contains IP address
            
            // verify sig and cert and get CertContext
            if (!CryptoUtils.VerifyCertSignedBytes(signedConfig, out var certContext))
            {
                errorMessage = $"Invalid QridConfiguration - cannot verify signature b/c of the following Error: {certContext?.ErrorMessage}";
                return null;
            }

            // verify Domain in Cert
            if (isDomainName && certContext.Subjects.Domain != domain)
            {
                errorMessage = $"Invalid QridConfiguration - signing certificate has Domain = '{certContext.Subjects.Domain}' which is different from '{domain}'";
                return null;
            }

            // verify LeafKeyRole on the Cert   
            if (certContext.LeafKeyRole != QCP.Const.KeyRole.CA.DomainConfig)
            {
                errorMessage = $"Invalid QridConfiguration - signing certificate has LeafKeyRole = '{certContext.LeafKeyRole}' which is different from '{QCP.Const.KeyRole.CA.DomainConfig}'";
                return null;
            }

            // decode QridConfiguration
            var config = StringObject.Create(signedConfig.Bytes);
            var configJson = config.Value;
            var qridConfig = System.Text.Json.JsonSerializer.Deserialize<QridConfiguration>(configJson);

            // verify Domain in QridConfiguration
            if (isDomainName && qridConfig.Domain != domain)
            {
                errorMessage = $"Invalid QridConfiguration - QridConfiguration has Domain = '{qridConfig.Domain}' which is different from '{domain}'";
                return null;
            }

            errorMessage = null;
            return qridConfig;
        }


        public static CR.SymEncryptedBytes SymEncrypt(byte[] data, CR.CryptoKey symKey, byte[] associatedData = null)
        {
            // TODO - looks like we need to check symKey.KeyAlgo, and then decide what sym algo (and with what key size) to use here
            
            var keyBytes = symKey.KeyBytes.ToByteArray();
            if (keyBytes.Length != 32)
                throw new ApplicationException($"Only support AES GCM 256 bit keys where key size is 32 bytes. Called with keySize={keyBytes.Length} bytes");
            var res = new CR.SymEncryptedBytes();
            res.EncryptionAlgo = EncryptionAlgo.AesGcm256;
            res.Bytes = GPB.ByteString.CopyFrom(EncryptWithAesGcm(data, symKey.KeyBytes.ToByteArray(), associatedData));
            return res;
        }
        
        public static CR.SymEncryptedBytes SymEncrypt(ProtoBase data, CR.CryptoKey symKey, ProtoBase associatedData = null)
        {
            var res = SymEncrypt(data.GetByteArray(), symKey, associatedData?.GetByteArray());
            return res;
        }

        public static CR.SymEncryptedBytes SymEncrypt(ProtoBase data, CR.CryptoKey symKey, byte[] associatedData = null)
        {
            var res = SymEncrypt(data.GetByteArray(), symKey, associatedData);
            return res;
        }

        public static CR.SymEncryptedBytes SymEncryptPaddedString(string data, CR.CryptoKey symKey, ProtoBase associatedData = null)
        {
            var res = SymEncryptPaddedString(data, symKey, associatedData?.GetByteArray());
            return res;
        }

        public static CR.SymEncryptedBytes SymEncryptPaddedString(string data, CR.CryptoKey symKey, byte[] associatedData = null)
        {
            // random lenght padding to avoid leaking information about secret's length
            var randomLengthPadding = "".PadRight(CryptoUtils.GetRandomInteger(0, 50), 'p');
            var res = SymEncrypt(PaddedData.CreateWith(data, randomLengthPadding).GetByteArray(), symKey, associatedData);
            return res;
        }

        public static CertSignedBytes SymEncryptThenSign(byte[] data, byte[] associatedData, CryptoKey encryptionKey, CertifiedKeyPair signingKey)
        {
            var res = new CertSignedBytes();
            res.Bytes = CryptoUtils.SymEncrypt(data, encryptionKey, associatedData).ToByteString();
            res.Sig = CryptoUtils.SignCertified(res.Bytes, signingKey);
            return res;
        }

        public static bool SymDecryptAndVerifySig(CertSignedBytes csb, byte[] associatedData, CryptoKey encryptionKey, out byte[] data, out string errorMessage)
        {
            // verify sig
            if (VerifyCertSignedBytes(csb, out var certContext))
            {
                // decrypt, if sig is valid
                data = CryptoUtils.SymDecrypt(SymEncryptedBytes.Create(csb.Bytes), encryptionKey, associatedData);
                errorMessage = null;
                return true;
            }
            else
            {
                // invalid sig
                data = null;
                errorMessage = $"Invalid signature while validating CertSignedBytes! The error is '{certContext.ErrorMessage}'";
                return false;
            }
        }

        public static byte[] SymDecrypt(CR.SymEncryptedBytes data, CR.CryptoKey symKey, byte[] associatedData)
        {
            switch(data.EncryptionAlgo)
            {
                case EncryptionAlgo.Unspecified:
                    return DecryptWithKey_Obsolete(data.Bytes.ToByteArray(), symKey.KeyBytes.ToByteArray(), null);
                case EncryptionAlgo.AesGcm128:
                    throw new ApplicationException("AesGcm128 is not supported");
                case EncryptionAlgo.AesGcm256:
                    return DecryptWithAesGcm(data, symKey, associatedData);
                default:
                    throw new ApplicationException("Unsupported EncryptionAlgo");
            }         
        }

        public static byte[] SymDecrypt(string dataString, CR.CryptoKey symKey, byte[] associatedData)
        {
            var data = CR.SymEncryptedBytes.Create(dataString);
            return SymDecrypt(data, symKey, associatedData);
            //return DecryptWithKey(data.Bytes.ToByteArray(), symKey.KeyBytes.ToByteArray(), null);
        }

        public static string SymDecryptPaddedString(string dataString, CR.CryptoKey symKey, byte[] associatedData)
        {
            var data = CR.SymEncryptedBytes.Create(dataString);
            return SymDecryptPaddedString(data, symKey, associatedData);
        }
        public static string SymDecryptPaddedString(SymEncryptedBytes data, CR.CryptoKey symKey, byte[] associatedData)
        {
            var bytes = SymDecrypt(data, symKey, associatedData);
            var paddedData = PaddedData.Create(bytes);
            return paddedData.AsString;
        }

        private const int AES_GCM_MAC_SIZE_BYTES = 16;

        // Originally intended to be used in "QRID Password Backup Image" b/c we do NOT want any MAC or authenticated enc there b/c we do not want to enable offline brute-forcing when image is leaked
        public static byte[] EncryptWithAesCtr(byte[] messageToEncrypt, byte[] key)
        {
            // this will never happen for current byte[] callers, but keep the guard in long arithmetic in case the implementation changes in the future
            if (messageToEncrypt.LongLength >= 68719476736L)
                throw new ArgumentException("AES CTR (as we use it here with 12 byte Nonce and 4 byte CTR) must not encrypt a message larger than 64Gb with the same Key + Nonce. Please chunk your message into smaller pieces with each chunk having its unique Nonce");

            // Using random nonce large enough not to repeat
            var nonce = GetRandomBytes(12); // The standard is 92-bit nonce (12 bytes)

            // Build 16-byte IV (nonce + counter)
            // CTR needs full block size IV (16 bytes for AES)
            var iv = new byte[16];
            Array.Copy(nonce, 0, iv, 0, nonce.Length); // last 4 bytes = initial counter (starts at 0)

            var cipher = new BufferedBlockCipher(new SicBlockCipher(new AesEngine())); // SIC = CTR mode
            var parameters = new ParametersWithIV(new KeyParameter(key), iv);
            cipher.Init(true, parameters);

            // Generate Cipher Text
            var cipherText = new byte[cipher.GetOutputSize(messageToEncrypt.Length)];
            var len = cipher.ProcessBytes(messageToEncrypt, 0, messageToEncrypt.Length, cipherText, 0);
            cipher.DoFinal(cipherText, len);

            // Assemble resulting AesCtrNonAuthBytes proto object
            var res = AesCtrNonAuthBytes.CreateWith(nonce, cipherText);
            return res.ToByteArray();
        }

        public static byte[] EncryptWithAesGcm(byte[] messageToEncrypt, byte[] key, byte[] associatedData = null)
        {
            // this will never happen for current byte[] callers, but keep the guard in long arithmetic in case the implementation changes in the future
            if (messageToEncrypt.LongLength >= 68719476736L)
                throw new ArgumentException("AES GCM (as we use it here with 12 byte Nonce and 4 byte CTR) must not encrypt a message larger than 64Gb with the same (Key + Nonce). Explanation: 64Gb = (2^(4 byte CTR * 8 bit))*(16 bytes AES block size). Please chunk your message into smaller pieces with each chunk having its unique Nonce, or use a unique AES key for every chunk");

            // Non-secret Payload Optional
            associatedData = associatedData ?? new byte[] { };

            // Using new random nonce large enough not to repeat
            var randomNonce = GetRandomBytes(12); // The standard is 92-bit nonce (12 bytes), new value on every call

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 8 * AES_GCM_MAC_SIZE_BYTES, randomNonce, associatedData); // 128 bit mac size
            cipher.Init(true, parameters);

            // Generate Cipher Text With Auth Tag
            var cipherTextWithMac = new byte[cipher.GetOutputSize(messageToEncrypt.Length)];
            var len = cipher.ProcessBytes(messageToEncrypt, 0, messageToEncrypt.Length, cipherTextWithMac, 0);
            cipher.DoFinal(cipherTextWithMac, len);

            // Assemble resulting AesGcmBytes proto object
            var res = AesGcmBytes.CreateWith(randomNonce, AES_GCM_MAC_SIZE_BYTES, cipherTextWithMac);
            return res.ToByteArray();
        }

        // used internally only as part of Hybrid PQC IES schema
        internal static byte[] EncryptWithAesGcmHybrid(byte[] messageToEncrypt, byte[] key, byte[] associatedData, byte[] nonce)
        {
            // this will never happen for current byte[] callers, but keep the guard in long arithmetic in case the implementation changes in the future
            if (messageToEncrypt.LongLength >= 68719476736L)
                throw new ArgumentException("AES GCM (as we use it here with 12 byte Nonce and 4 byte CTR) must not encrypt a message larger than 64Gb with the same Key + Nonce. Please chunk your message into smaller pieces with each chunk having its unique Nonce");

            if (nonce.Length != 12)
                throw new ArgumentException($"nonce length is {nonce.Length} but we expect here exactly 12 bytes nonce");

            associatedData ??= Array.Empty<byte>();
            
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 8 * AES_GCM_MAC_SIZE_BYTES, nonce, associatedData); // 128 bit mac size
            cipher.Init(true, parameters);

            //Generate Cipher Text With Auth Tag
            var cipherTextWithMac = new byte[cipher.GetOutputSize(messageToEncrypt.Length)];
            var len = cipher.ProcessBytes(messageToEncrypt, 0, messageToEncrypt.Length, cipherTextWithMac, 0);
            cipher.DoFinal(cipherTextWithMac, len);
            
            return cipherTextWithMac;
        }

        public static byte[] EncryptWithKey(byte[] messageToEncrypt, byte[] key, byte[] nonSecretPayload = null)
        {
            throw new ApplicationException("This method is obsolete. Use EncryptWithAesGcm instead");
        }

        public static byte[] DecryptWithAesCtr(CR.SymEncryptedBytes data, CR.CryptoKey symKey)
        {
            if (data == null || data.Bytes == null || data.Bytes.Length == 0)
            {
                throw new ArgumentException("data.Bytes are required!", "data");
            }

            var acb = AesCtrNonAuthBytes.Create(data.Bytes);

            // Grab Nonce
            var nonce = acb.Nonce?.ToByteArray() ?? Array.Empty<byte>();
            // make sure it is at least 96 bit nonce (12 bytes)
            // TODO - do we really want this? Many cases we just use KEY once, and may want a file larger than 64Gb, so may need a CTR longer than 4 bytes (5 bytes? So we could encrypt 256*64Gb = 16Tb file) and consequently nonce shorter than 12 bytes (11 bytes?)
            if (nonce.Length < 12)
                throw new ApplicationException($"Nonce is too short - {nonce.Length} bytes");

            // Create IV
            var iv = new byte[16];
            Array.Copy(nonce, iv, nonce.Length);

            // Grab KeyBytes
            var keyBytes = symKey.KeyBytes.ToByteArray();
            if (keyBytes.Length != 32)
                throw new ApplicationException($"Unsupported AES CTR key size - {keyBytes.Length} bytes");

            var cipher = new BufferedBlockCipher(new SicBlockCipher(new AesEngine()));
            var parameters = new ParametersWithIV(new KeyParameter(keyBytes), iv);
            cipher.Init(false, parameters);

            //Decrypt Cipher Text
            var cipherText = acb.Bytes?.ToByteArray() ?? Array.Empty<byte>();
            var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
            var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);

            len += cipher.DoFinal(plainText, len);
            return plainText;
        }


        public static byte[] DecryptWithAesGcm(CR.SymEncryptedBytes data, CR.CryptoKey symKey, byte[] associatedData)
        {
            if (data == null || data.Bytes == null || data.Bytes.Length == 0)
            {
                throw new ArgumentException("data.Bytes are required!", "data");
            }

            var agb = AesGcmBytes.Create(data.Bytes);

            // Grab AssociatedData or Empty
            associatedData = associatedData ?? Array.Empty<byte>();

            // Grab Nonce
            var nonce = agb.Nonce?.ToByteArray() ?? Array.Empty<byte>();
            // make sure it is at least 96 bit nonce (12 bytes)
            // TODO - do we really want this? Many cases we just use KEY once, and may want a file larger than 64Gb, so may need a CTR longer than 4 bytes (5 bytes? So we could encrypt 256*64Gb = 16Tb file) and consequently nonce shorter than 12 bytes (11 bytes?)
            //      - however, 12 bytes GCM nonce is a NIST standard - NIST Special Publication 800-38D, published in November 2007
            if (nonce.Length < 12)
                throw new ApplicationException($"Nonce is too short - {nonce.Length} bytes");

            // Grab KeyBytes
            var keyBytes = symKey.KeyBytes.ToByteArray();
            if (keyBytes.Length != 32)
                throw new ApplicationException($"Unsupported AES GCM key size - {keyBytes.Length} bytes");

            if (agb.MacLength < AES_GCM_MAC_SIZE_BYTES)
                throw new ApplicationException($"Mac is too short - {agb.MacLength} bytes");


            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(keyBytes), agb.MacLength * 8, nonce, associatedData);
            cipher.Init(false, parameters);

            //Decrypt Cipher Text
            var cipherText = agb.BytesWithMac?.ToByteArray() ?? Array.Empty<byte>();
            var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
            var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);


            // TODO - handle exceptions here? Or higher up in the calling stack?
            // this will throw if we use wrong key to decrypt - a very real scenario - we need to handle this
            // right now, it will just crash the app - BAD


            // (!!!) throws Org.BouncyCastle.Crypto.InvalidCipherTextException: mac check in GCM failed
            len += cipher.DoFinal(plainText, len);

            return plainText;
        }

        // only used internally as part of Hybrid PQC IES schema
        internal static byte[] DecryptWithAesGcmHybrid(byte[] key, byte[] nonce, byte[] ciphertext, byte[] aad)
        {
            var gcm = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 8 * AES_GCM_MAC_SIZE_BYTES, nonce, aad); // MAC size 128 bits (16 bytes)
            gcm.Init(false, parameters);
            byte[] outBuf = new byte[gcm.GetOutputSize(ciphertext.Length)];
            int len = gcm.ProcessBytes(ciphertext, 0, ciphertext.Length, outBuf, 0);
            gcm.DoFinal(outBuf, len);
            return outBuf;
        }


        // TODO - remove this old code b/c it must not be used
        public static byte[] DecryptWithKey_Obsolete(byte[] encryptedMessage, byte[] key, byte[] associatedData)
        {

            //User Error Checks
            //CheckKey(key);

            if (encryptedMessage == null || encryptedMessage.Length == 0)
            {
                throw new ArgumentException("Encrypted Message Required!", "encryptedMessage");
            }

            int associatedDataLength = associatedData?.Length ?? 0;

            using (var cipherStream = new MemoryStream(encryptedMessage))
            using (var cipherReader = new BinaryReader(cipherStream))
            {
                //Grab Payload
                var nonSecretPayload = cipherReader.ReadBytes(associatedDataLength);

                // !!!! TODO - compare associatedData vs nonSecretPayload - must match exactly
                //
                //

                //Grab Nonce
                var nonce = cipherReader.ReadBytes(16); // 128 bit nonce

                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), 128, nonce, associatedData); // 128 bit mac
                cipher.Init(false, parameters);

                //Decrypt Cipher Text
                var cipherText = cipherReader.ReadBytes(encryptedMessage.Length - associatedDataLength - nonce.Length);
                var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
                var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);


                // TODO - handle exceptions here? Or higher up in the calling stack?
                // this will throw if we use wrong key to decrypt - a very real scenario - we need to handle this
                // right now, it will just crash the app - BAD
                // Org.BouncyCastle.Crypto.InvalidCipherTextException: mac check in GCM failed
                len += cipher.DoFinal(plainText, len);

                return plainText;
            }
        }

        #region Encrypt with Password

        public static KdfParams GetKdfParamsDefaults(KdfType? kdfType = null)
        {
            var res = new KdfParams();
            res.KdfType = kdfType ?? KdfType.Argon2Id;
            res.KdfParams_ =  GetKdfParamsDefaultsInternal(res.KdfType);
            return res;
        }

        public static ByteString GetKdfParamsDefaultsInternal(KdfType kdfType)
        {
            int iterations = 20;
            int memoryPowerOf2 = 16; // 64 Mb
#if DEBUG 
            iterations = 2;
            memoryPowerOf2 = 13; // 8 Mb
#endif

            switch (kdfType)
            {
                case KdfType.Argon2Id:
                    {
                        var p = new KdfParamsArgon2();
                        p.Parallelism = 2;
                        p.MemoryPowerOf2 = memoryPowerOf2; // 64 Mb
                        p.Iterations = iterations; // or 3 ???
                        p.AssociatedData = ByteString.Empty;
                        return p.GetByteString();
                    }
                case KdfType.Argon2I:
                    {
                        // (!) this is the backwards compatible flow
                        var p = new KdfParamsArgon2();
                        p.Parallelism = 2;
                        p.MemoryPowerOf2 = 13; // 8192 Kb i.e. 8 Mb
                        p.Iterations = iterations; // or 3 ???
                        p.AssociatedData = ByteString.Empty;
                        return p.GetByteString();
                    }
            }
            return null;
        }

        public static PasswordEncryptedBytes PasswordEncrypt(byte[] data, string password, ProtoBase associatedData = null, KdfParams kdfParams = null, string derivationDomain = null)
        {
            var res = new PasswordEncryptedBytes();
            
            // sort out KDF and KDF Params
            res.KdfParams = kdfParams ?? GetKdfParamsDefaults();

            var saltBase64 = GetRandomBytesToString(64);
            var aesKey = DeriveAesKey(password, saltBase64, res.KdfParams, derivationDomain);
            
            res.Salt = saltBase64;
            res.Bytes = SymEncrypt(data, aesKey, associatedData?.GetByteArray()); // TODO - use AEAD (AES GCM) and pass (Salt + KdfType + KdfParams) as AD, verify on Decrypt

            return res;
        }

        public static byte[] PasswordDecrypt(PasswordEncryptedBytes encData, string password, byte[] associatedData, string derivationDomain = null)
        {
            if (encData.KdfParams == null)
            {
                // backwards compatibility
                // old data created with old code when we did not have KdfParams, and used Argon2i with hardcoded Argon params
                // to decrypt such data, let's create KdfParams here matching those "hardcoded" params in the old code
                var argonParams = new KdfParamsArgon2()
                {
                    Iterations = 20,
                    MemoryPowerOf2 = 13, // 8 Mb
                    Parallelism = 2,
                    AssociatedData = ByteString.CopyFrom(DeprecatedBytes1234)
                };
                encData.KdfParams = KdfParams.Create(KdfType.Argon2I, argonParams);
            }

            var aesKey = DeriveAesKey(password, encData.Salt, encData.KdfParams, derivationDomain);
            var res = SymDecrypt(encData.Bytes, aesKey, associatedData);
            return res;
        }

        public static PasswordEncryptedBytes PasswordEncryptString(string data, string password, ProtoBase associatedData = null)
        {
            return PasswordEncrypt(StringObject.CreateWithString(data).ToByteArray(), password, associatedData);
        }

        public static string PasswordDecryptString(PasswordEncryptedBytes encData, string password, byte[] associatedData)
        {
            var decryptedBytes = PasswordDecrypt(encData, password, associatedData);
            return StringObject.Create(decryptedBytes).Value;
        }

        public static PasswordEncryptedBytes PasswordEncryptTypedData(TypedDataList typedData, string password, ProtoBase associatedData = null)
        {
            return PasswordEncrypt(typedData.GetByteArray(), password, associatedData);
        }

        public static TypedDataList PasswordDecryptTypedData(PasswordEncryptedBytes encTypedData, string password, byte[] associatedData)
        {
            return TypedDataList.Create(PasswordDecrypt(encTypedData, password, associatedData));
        }

        public static PasswordEncryptedBytes PasswordEncryptCertifiedKeyPair(CertifiedKeyPair certifiedKeyPair, string password, ProtoBase associatedData = null)
        {
            return PasswordEncryptTypedData(TypedDataList.Create(TypedData.Create(certifiedKeyPair)), password, associatedData);
        }

        public static CertifiedKeyPair PasswordDecryptCertifiedKeyPair(PasswordEncryptedBytes encCertifiedKeyPair, string password, byte[] associatedData)
        {
            var tdl = TypedDataList.Create(PasswordDecrypt(encCertifiedKeyPair, password, associatedData));
            var res = tdl.Items[0].AsCertifiedKeyPair;
            return res;
        }

        public static PasswordEncryptedBytes PasswordEncryptCryptoKey(CryptoKey cryptoKey, string password, ProtoBase associatedData = null)
        {
            return PasswordEncryptTypedData(TypedDataList.Create(TypedData.Create(cryptoKey)), password, associatedData);
        }

        public static CryptoKey PasswordDecryptCryptoKey(PasswordEncryptedBytes encCryptoKey, string password, byte[] associatedData)
        {
            var tdl = TypedDataList.Create(PasswordDecrypt(encCryptoKey, password, associatedData));
            var res = tdl.Items[0].AsCryptoKey;
            return res;
        }

        public static PasswordEncryptedBytes PasswordEncryptCryptoKeyPair(CryptoKeyPair keyPair, string password, ProtoBase associatedData = null)
        {
            return PasswordEncryptTypedData(TypedDataList.Create(TypedData.Create(keyPair)), password, associatedData);
        }

        public static CryptoKeyPair PasswordDecryptCryptoKeyPair(PasswordEncryptedBytes encCertifiedKeyPair, string password, byte[] associatedData)
        {
            var tdl = TypedDataList.Create(PasswordDecrypt(encCertifiedKeyPair, password, associatedData));
            var res = tdl.Items[0].AsCryptoKeyPair;
            return res;
        }

        #endregion

        #region Asym Encryption

        public static CR.AsymEncryptedBytes EncryptAsym(byte[] data, CR.CryptoKey receiverPubKey)
        {
            if (receiverPubKey.KeyAlgo == KeyAlgo.HybridEcMlKem)
                return EncryptAsymHybrid(data, receiverPubKey);

            var ccReceiverPubKey = CC.EccPublicKey.Create(receiverPubKey);
            return EncryptAsym(data, ccReceiverPubKey);
        }

        public static CR.AsymEncryptedBytes EncryptAsym(byte[] data, CC.EccPublicKey receiverPubKey, byte[] derivation = null, byte[] encoding = null)
        {
            BcCrypto.BufferedBlockCipher senderCipher =
                new BcCrypto.Paddings.PaddedBufferedBlockCipher(
                    new BcCrypto.Modes.CbcBlockCipher(
                        new BcCrypto.Engines.AesEngine(
                            )));

            var agreement = new Agreement.ECDHBasicAgreement();
            var digest = DigestUtilities.GetDigest("SHA256");
            var kdf = new Generators.Kdf2BytesGenerator(digest);
            //var kdf = new Agreement.Kdf.ECDHKekGenerator(digest);
            var mac = new BcCrypto.Macs.HMac(digest);
            var engine = new BcCrypto.Engines.IesEngine(agreement, kdf, mac, senderCipher);

            // * @param derivation the derivation parameter for the KDF function - just an extra data to seed KDF
            // * @param encoding - used in MAC calculation, acts as additional authenticated data (AAD) — not encrypted, but authenticated
            //                     MAC = HMAC(macKey, ciphertext || encoding)
            // * @param macKeySize the size of the MAC key (in bits).


            // derivation - for KDF 
            // encoding - for MAC - so it is "authenticated" AssociatedData (AAD)
            // derivation and encoding could be nulls, or empty arrays, or arrays of any length
            // size of the MAC key (256 bits), size of BlockCipher key (256 bits).

            derivation ??= Array.Empty<byte>();
            encoding ??= Array.Empty<byte>();

            Parameters.IesParameters p = new Parameters.IesWithCipherParameters(derivation, encoding, 256, 256);

            //senderPrivKey - ephemeral randomly generated
            var senderKeyPair = GetRandomEccKeyPair();

            var senderPrivKeyBc = senderKeyPair.PrivateKey.AsBouncyEcPrivateKeyParams();
            var receiverPubKeyBc = receiverPubKey.AsBouncyEcPublicKeyParams();
            engine.Init(true, senderPrivKeyBc, receiverPubKeyBc, p);

            var encryptedBytes = engine.ProcessBlock(data, 0, data.Length);
            var res = AsymEncryptedBytes.CreateWith(senderKeyPair.PublicKey, encryptedBytes, derivation, encoding);
            return res;
        }

        public static CR.AsymEncryptedBytesWithKeyId EncryptAsymWithKeyId(byte[] data, CR.CryptoKey receiverPubKey)
        {
            var asymEncryptedBytes = receiverPubKey.KeyAlgo == KeyAlgo.HybridEcMlKem
                ? EncryptAsymHybrid(data, receiverPubKey)
                : EncryptAsym(data, CC.EccPublicKey.Create(receiverPubKey));
            var keyId = GetPubKeyHashId(receiverPubKey);
            return CR.AsymEncryptedBytesWithKeyId.Create(asymEncryptedBytes, keyId);
        }

        public static bool Test_GetPubKeyHashId()
        {
            // TODO MUSTDO - test and make sure that GetPubKeyHashId always produces the same value when key is transformed between CC.EccPublicKey and CR.CryptoKey and back

            var keyPair1 = GetRandomEccKeyPair();
            var ccPubKey1 = keyPair1.PublicKey;
            var crPubKey1 = CR.CryptoKey.FromCanonicalKey(ccPubKey1);
            var ccPubKey2 = CC.EccPublicKey.Create(crPubKey1);
            var crPubKey2 = CR.CryptoKey.FromCanonicalKey(ccPubKey2);

            var id1 = GetPubKeyHashId(crPubKey1);
            var id2 = GetPubKeyHashId(crPubKey2);

            return id1 == id2;
        }

        public static CR.AsymEncryptedBytesWithKeyId EncryptAsymWithKeyId(byte[] data, CC.EccPublicKey ccReceiverPubKey)
        {
            // var 
            var receiverPubKey = CR.CryptoKey.FromCanonicalKey(ccReceiverPubKey);
            return EncryptAsymWithKeyId(data, receiverPubKey);
        }

        public static QP.Messaging.Envelope GetEnvelopeFromMessage(ProtoBase message, CR.CryptoKey receiverPubKey, CR.CertifiedKeyPair signingKey)
        {
            var res = new QP.Messaging.Envelope();
            var asymEncryptedBytes = CryptoUtils.EncryptAsymTyped(message, receiverPubKey);
            res.SecureData = CryptoUtils.SignCertified(asymEncryptedBytes, signingKey);
            return res;
        }

        public static CR.AsymEncryptedBytes EncryptAsymTyped(ProtoBase obj, CR.CryptoKey receiverPubKey)
        {
            var typedDataBytes = TypedData.Create(obj).GetByteArray();
            var res = CryptoUtils.EncryptAsym(typedDataBytes, receiverPubKey);
            return res;
        }

        public static QP.Messaging.Message DecryptEnvelopeAsMessage(GPB.ByteString csbBytes, CR.CryptoKey receiverPrivKey)
        {
            var decryptedBytes = CryptoUtils.DecryptAsym(CR.AsymEncryptedBytes.Create(csbBytes), receiverPrivKey);
            var td = TypedData.Create(decryptedBytes);
            //todo - check td.DataType 
            var res = QP.Messaging.Message.Create(td.DataBytes);
            return res;
        }

        public static byte[] DecryptAsym(CR.AsymEncryptedBytes data, CR.CryptoKey receiverPrivKey)
        {
            if (data.EncryptionSchema == EncryptionSchema.HybridEcP256Mlkem768Sha256Aes256CbcSha256)
                return DecryptAsymHybrid(data, receiverPrivKey);

            var ccReceiverPrivKey = CC.EccPrivateKey.Create(receiverPrivKey);
            return DecryptAsym(data, ccReceiverPrivKey);
        }

        public static byte[] DecryptAsym(CR.AsymEncryptedBytes aebData, CC.EccPrivateKey receiverPrivKey)
        {
            if (aebData.EncryptionSchema != EncryptionSchema.IesEcP256Sha256Aes256CbcSha256)
                throw new ApplicationException($"Asym EncryptionSchema = '{aebData.EncryptionSchema.ToString()}' is not supported");

            var iesData = IesEcP256Sha256Aes256CbcSha256Bytes.Create(aebData.Bytes);

            BcCrypto.BufferedBlockCipher receiverCipher =
                new BcCrypto.Paddings.PaddedBufferedBlockCipher(
                    new BcCrypto.Modes.CbcBlockCipher(
                        new BcCrypto.Engines.AesEngine(
                            )));

            var agreement = new Agreement.ECDHBasicAgreement();
            var digest = DigestUtilities.GetDigest("SHA256");
            var kdf = new Generators.Kdf2BytesGenerator(digest);
            //var kdf = new Agreement.Kdf.ECDHKekGenerator(digest);
            var mac = new BcCrypto.Macs.HMac(digest);
            var engine = new BcCrypto.Engines.IesEngine(agreement, kdf, mac, receiverCipher);

            // * @param derivation the derivation parameter for the KDF function.
            // * @param encoding the encoding parameter for the KDF function.
            // * @param macKeySize the size of the MAC key(in bits).

            // derivation parameter for the KDF function.
            // encoding parameter for the KDF function.
            // size of the MAC key (256 bits), size of BlockCipher key (256 bits).
            Parameters.IesParameters p = new Parameters.IesWithCipherParameters(iesData.Derivation.ToByteArray(), iesData.Encoding.ToByteArray(), 256, 256);

            // TODO MUSTDO - we should NOT be using untrusted (!!!) PublicKey that came with the encrypted data
            // we should just use PublicKey that we have, or just re-calc it from PrivKey
            engine.Init(false, receiverPrivKey.AsBouncyEcPrivateKeyParams(), iesData.PublicKey.AsBouncyEcPublicKeyParams(), p);

            var encryptedBytes = iesData.Bytes.ToByteArray();
            var res = engine.ProcessBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            return res;
        }


        #endregion

        #region try decrypt - non-throwing wrappers of decrypt

        public static HttpResponseModel<BytesResponse> TrySymDecrypt(CR.SymEncryptedBytes data, CR.CryptoKey symKey, byte[] associatedData)
        {
            var res = new HttpResponseModel<BytesResponse>();
            
            if (data == null)
            {
                res.SetError($"Error - cannot decrypt null data");
                return res;
            }

            res.Data = new BytesResponse();
            try 
            { 
                res.Data.Bytes = SymDecrypt(data, symKey, associatedData);
            }
            catch (Org.BouncyCastle.Crypto.InvalidCipherTextException bcex1)
            {
                res.SetError($"Error while calling SymDecrypt() - {bcex1.Message}");
            }
            return res;
        }

        public static HttpResponseModel<BytesResponse> TrySymDecrypt(string dataString, CR.CryptoKey symKey, byte[] associatedData)
        {
            var res = new HttpResponseModel<BytesResponse>();
            res.Data = new BytesResponse();
            try
            {
                res.Data.Bytes = SymDecrypt(dataString, symKey, associatedData);
            }
            catch (Org.BouncyCastle.Crypto.InvalidCipherTextException bcex1)
            {
                res.SetError($"Error while calling SymDecrypt() - {bcex1.Message}");
            }
            return res;
        }

        public static HttpResponseModel<StringResponse> TrySymDecryptPaddedString(string dataString, CR.CryptoKey symKey, byte[] associatedData)
        {
            var res = new HttpResponseModel<StringResponse>();
            res.Data = new StringResponse();
            try
            {
                res.Data.StringValue = SymDecryptPaddedString(dataString, symKey, associatedData);
            }
            catch (Org.BouncyCastle.Crypto.InvalidCipherTextException bcex1)
            {
                res.SetError($"Error while calling SymDecryptString() - {bcex1.Message}");
            }
            return res;
        }


        #endregion

        #region Certificates

        public static CR.CertifiedKeyPair GetRandomCertifiedKeyPair(CR.CryptoKey parentPriv, CR.Certificate parentCert, CR.CertifiedPublicKey subject, bool postQuantum = false)
        {
            // get new random Key Pair
            var res = new CR.CertifiedKeyPair();
            res.KeyPair = GetRandomAsymCryptoKeyPair(postQuantum);
            subject.PublicKey = res.KeyPair.PublicKey;

            // create unsigned cert with data from subject
            res.Certificate = CR.Certificate.Create(subject);

            // now need to sign the certificate
            res.Certificate.CertifyingSig = SignCertified(res.Certificate.CertificateData, parentPriv, parentCert);

            return res;
        }

        public static CR.CertifiedKeyPair GetRandomCertifiedKeyPair(CR.CryptoKey parentPriv, CR.Certificate parentCert, string subjectName, string subjectType, string keyRole, DateTime expiryDate, bool postQuantum = false)
        {
            var certPubKeyInfo = new CR.CertifiedPublicKey
            {
                SubjectName = subjectName,
                SubjectType = subjectType,
                KeyRole = keyRole,
                PublicKey = null,
                NotBefore = GWK.Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-10)),
                NotAfter = GWK.Timestamp.FromDateTime(expiryDate.ToUniversalTime())
            };
            return GetRandomCertifiedKeyPair(parentPriv, parentCert, certPubKeyInfo, postQuantum);
        }

        public static CertificateSigningRequest CreateCertificateSigningRequest(CryptoKeyPair keyPair, string keyRole, string subjectName, string subjectType, DateTime expirationDate)
        {
            var cpk = CertifiedPublicKey.Create(
                keyPair.PublicKey,
                keyRole,
                subjectName,
                subjectType,
                expirationDate);

            var csr = new CR.CertificateSigningRequest();
            csr.CertificateData = QCP.TypedData.Create(cpk).GetByteString();
            csr.SelfSigningSig = Sign(csr.CertificateData, keyPair.PrivateKey);

            return csr;
        }

        public static bool VerifyCertificateSigningRequest(CertificateSigningRequest csr, out CR.CertifiedPublicKey cpk, out string errorMessage)
        {
            cpk = TypedData.Create(csr.CertificateData).AsCertifiedPublicKey;
            errorMessage = string.Empty;
            
            // verify that CSR Signature is valid
            if (!CryptoUtils.VerifyHashSig(csr.CertificateData, csr.SelfSigningSig, cpk.PublicKey))
            {
                errorMessage = $"CertificateSigningRequest contains invalid signature (SelfSigningSig)";
                return false;
            }

            return true;
        }

        public static bool VerifyCertificateSigningRequest(CertificateSigningRequest csr, out CR.CertifiedPublicKey cpk, out string errorMessage, string keyRole, string subjectName, string subjectType)
        {
            if (!VerifyCertificateSigningRequest(csr, out cpk, out errorMessage)) return false;

            // if the above is valid, do some extra validations below
            
            var notBefore = cpk.NotBeforeOrMax;
            var notAfter = cpk.NotAfterOrMin;

            if (notAfter < DateTime.Now.AddMinutes(1))
            {
                errorMessage = $"CertificateSigningRequest contains invalid NotAfter < DateTime.Now.AddMinutes(1)";
                return false;
            }

            if (notAfter < notBefore)
            {
                errorMessage = $"CertificateSigningRequest contains invalid dates - notAfter < notBefore";
                return false;
            }

            if (notAfter - notBefore > TimeSpan.FromDays(365*20 + 6))
            {
                errorMessage = $"CertificateSigningRequest contains invalid dates - NotBefore and NotAfter must be within 20 years of each other or less";
                return false;
            }

            if (keyRole != null && keyRole != cpk.KeyRole)
            {
                errorMessage = $"CertificateSigningRequest error: KeyRole='{cpk.KeyRole}' is different from expected value of '{keyRole}'";
                return false;
            }

            if (subjectName != null && subjectName != cpk.SubjectName)
            {
                errorMessage = $"CertificateSigningRequest error: SubjectName='{cpk.SubjectName}' is different from expected value of '{subjectName}'";
                return false;
            }

            if (subjectType != null && subjectType != cpk.SubjectType)
            {
                errorMessage = $"CertificateSigningRequest error: SubjectType='{cpk.SubjectType}' is different from expected value of '{subjectType}'";
                return false;
            }

            return true;
        }

        #endregion

        #region Certificates - SELF-SIGNED

        public static CR.CertifiedKeyPair GetRandomSelfSignedKeyPair(string keyRole)
        {
            // used for creating self-signed RootOfTrust

            // get new random Key Pair
            var keyPair = GetRandomAsymCryptoKeyPair();
            return GetSelfSignedKeyPair(keyPair, keyRole);
        }

        public static CR.CertifiedKeyPair GetSelfSignedKeyPair(CR.CryptoKeyPair keyPair, string keyRole)
        {
            // protobuf does not allow null in a string, but it is ok to have null in object (i.e. NotBefore/After)
            var cpk = new CR.CertifiedPublicKey
            {
                SubjectName = string.Empty,
                SubjectType = string.Empty,
                KeyRole = keyRole,
                PublicKey = keyPair.PublicKey,
                NotBefore = null,
                NotAfter = null
            };

            var res = new CR.CertifiedKeyPair();
            res.KeyPair = keyPair;
            res.Certificate = CR.Certificate.Create(cpk);

            // now need to SELF-SIGN the certificate
            res.Certificate.CertifyingSig = SignCertified(res.Certificate.CertificateData, keyPair.PrivateKey, null);
            return res;
        }

        #endregion

        #region Files - Certificate / KeyPair files

        public static CertifiedKeyPair LoadCertifiedKeyPair(string keyPairFile, string keyPairPassword, string certFile)
        {
            // pls use LoadAndVerifyCertifiedKeyPair() if you also want to validate the resulting cert

            // 1. read the KP file - file name ends with "_PrivateKeyPair_.privkp"
            var bytes = File.ReadAllBytes(keyPairFile);
            var passwordEncryptedBytes = PasswordEncryptedBytes.Create(bytes);

            // 2. decrypt CKP
            var cryptoKeyPair = CryptoUtils.PasswordDecryptCryptoKeyPair(passwordEncryptedBytes, keyPairPassword, null);

            // 3. read the CERT file
            var certB64 = File.ReadAllText(certFile);
            var cert = CR.Certificate.Create(certB64);

            // 4. construct CKP
            if (cryptoKeyPair != null && cert != null)
            {
                var res = CertifiedKeyPair.Create(cryptoKeyPair, cert);
                return res;
            }
            return null;
        }

        public static CertifiedKeyPair LoadAndVerifyCertifiedKeyPair(string keyPairFile, string keyPairPassword, string certFile, out CertContext certContext)
        {
            var res = CryptoUtils.LoadCertifiedKeyPair(keyPairFile, keyPairPassword, certFile);
            var valid = CryptoUtils.VerifyCertifiedKeyPair(res, out certContext);
            if (!valid)
            {
                throw new ApplicationException($"Error while loading CERTIFIED KEY PAIR / CERTIFICATE - '{certContext.ErrorMessage}'");
            }
            return res;
        }

        public static CR.Certificate LoadAndVerifyCertificate(string certFile, out CertContext certContext)
        {
            var base64 = File.ReadAllText(certFile);
            var res = CR.Certificate.Create(base64);
            var valid = CryptoUtils.VerifyCertificate(res, out certContext);
            if (!valid)
            {
                throw new ApplicationException($"Error while loading CERTIFICATE - '{certContext.ErrorMessage}'");
            }
            return res;
        }

        #endregion

        #region Messaging - validating CertifiedRecipientToken

        public static string ValidateContactToken(CertifiedRecipientToken contactToken, CertifiedRecipientType certificateType, string sender, string recipient)
        {
            if (contactToken == null)
                return "ContactToken is missing";

            //contactToken.CertifiedRecipient 
            //contactToken.TokenKey

            if (!CryptoUtils.VerifyCertSignedBytes(contactToken.CertifiedRecipient, KeyRolePath.ReputationTokenSigner, out var certContext))
                return $"Error while verifying ContactToken.CertifiedRecipient: {certContext.ErrorMessage}";
            

            // cert is authorized to sign CertifiedRecipient Tokens
            // lets extract the signed payload from CertSignedBytes 

            var certifiedRecipient = CertifiedRecipient.Create(contactToken.CertifiedRecipient.Bytes);
            if (certifiedRecipient.CertificateType != certificateType)
                return $"CertifiedRecipientToken contains unexpected CertificateType='{certifiedRecipient.CertificateType}' while expecting '{certificateType}'";

            // verify Sender
            if (certifiedRecipient.Sender != sender)
                return $"Sender from CertifiedRecipientToken ('{certifiedRecipient.Sender}') is not matching the actual sender from the Message ('{sender}')";

            // verify Recipient
            // we do not need to use/check AD when decrypting CertifiedRecipient.EncryptedRecipient b/c the entire CertifiedRecipient is signed
            var recipientObject = StringObject.Create(CryptoUtils.SymDecrypt(certifiedRecipient.EncryptedRecipient, contactToken.TokenKey, null));
            if (recipientObject.Value != recipient)
                return $"Recipient from CertifiedRecipientToken ('{recipientObject.Value}') is not matching the actual Recipient from the Message's SignedHeader ('{recipient}')";

            return string.Empty; // all is good
        }

        #endregion

        public static string GetDomain(string userName)
        {
            var a = userName.Split('@');
            if (a.Length == 2)
                return a[1].Trim().ToLower();
            else
                return null;
        }

        public static int GetRandomInteger(int min, int max)
        {
            SecureRandom secureRandom = new SecureRandom();
            return secureRandom.Next(min, max);
        }

        public static Guid GetRandomGuid()
        {
            var bytes = GetRandomBytes(16);
            return new Guid(bytes);
        }


        #region PQC Asym Encryption

        static byte[] HkdfSha256(byte[] ikm, byte[] salt, byte[] info, int length)
        {
            var hkdf = new HkdfBytesGenerator(new Sha256Digest());
            salt ??= Array.Empty<byte>();
            hkdf.Init(new HkdfParameters(ikm, salt, info));
            var okm = new byte[length];
            hkdf.GenerateBytes(okm, 0, length);
            return okm;
        }

        public static CR.AsymEncryptedBytes EncryptAsymHybrid(byte[] plaintext, CryptoKey receiverPubKey, byte[] derivation = null, byte[] associatedData = null)
        {
            var receiverHybridPub = receiverPubKey.ToHybridEcMlKemPublicKey();
            var recipientMlPub = receiverHybridPub.MlKemPublicKey.AsBouncyMlKemPublicKeyParams();
            var recipientEcPub = receiverHybridPub.EccPublicKey.AsBouncyEcPublicKeyParams();

            derivation ??= Array.Empty<byte>();
            associatedData ??= Array.Empty<byte>();


            // 1) ML-KEM encapsulate
            var encapsulator = new MLKemEncapsulator(MLKemParameters.ml_kem_768);   // encapsulator returns (secret,ciphertext)
            encapsulator.Init(new ParametersWithRandom(recipientMlPub, new SecureRandom()));
            byte[] kemSecret = new byte[encapsulator.SecretLength]; // raw kem secret bytes
            byte[] kemEncapsulation = new byte[encapsulator.EncapsulationLength]; // ciphertext to send
            encapsulator.Encapsulate(kemEncapsulation, 0, kemEncapsulation.Length, kemSecret, 0, kemSecret.Length);

            // 2) Generate ephemeral EC keypair (sender)
            var senderCkp = CryptoUtils.GetRandomAsymCryptoKeyPair();

            // 3) ECDH: compute shared secret
            var agree = new ECDHBasicAgreement();
            agree.Init(senderCkp.PrivateKey.AsBouncyEcPrivateKeyParams());
            var ecdhValue = agree.CalculateAgreement(recipientEcPub); // BigInteger
            byte[] ecdhSecret = BigIntegers.AsUnsignedByteArray(agree.GetFieldSize(), ecdhValue);

            // 4) Mix secrets and derive AEAD key via HKDF(SHA-256)
            byte[] combined = Arrays.Concatenate(kemSecret, ecdhSecret);
            byte[] salt = null; // define salt? well the keys are randomly generated few lines above, and keys will be used once. I dont think the salt here makes any difference.
            byte[] keyMaterial = HkdfSha256(combined, salt, derivation, 32 + 12); // 32 bytes AES-256 key + 12 byte nonce

            byte[] aeadKey = Arrays.CopyOfRange(keyMaterial, 0, 32);
            byte[] aeadNonce = Arrays.CopyOfRange(keyMaterial, 32, 32 + 12);

            // 5) AEAD encrypt (AES-GCM shown)
            byte[] ciphertext = CryptoUtils.EncryptWithAesGcmHybrid(plaintext, aeadKey, associatedData, aeadNonce);

            var hybridEnvelope = new HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes
            {
                MlKemEncapsulation = ByteString.CopyFrom(kemEncapsulation),
                SenderEcPublicKey = senderCkp.PublicKey,
                Derivation = ByteString.CopyFrom(derivation),
                AssociatedData = ByteString.CopyFrom(associatedData),
                Bytes = ByteString.CopyFrom(ciphertext)
            };

            var res = AsymEncryptedBytes.CreateWith(hybridEnvelope);
            return res;
        }

        public static byte[] DecryptAsymHybrid(AsymEncryptedBytes encryptedBytes, CryptoKey recipientPrivateKey)
        {
            if (encryptedBytes.EncryptionSchema != EncryptionSchema.HybridEcP256Mlkem768Sha256Aes256CbcSha256)
                throw new ArgumentException($"encryptedBytes.EncryptionSchema ({encryptedBytes.EncryptionSchema}) is different from expected value of EncryptionSchema.HybridEcP256Mlkem768Sha256Aes256CbcSha256");

            if (recipientPrivateKey.KeyAlgo != KeyAlgo.HybridEcMlKem)
                throw new ArgumentException($"recipientPrivateKey.KeyAlgo ({recipientPrivateKey.KeyAlgo}) is different from expected value of KeyAlgo.HybridEcMlKem");


            var recipientHybridPriv = recipientPrivateKey.ToHybridEcMlKemPrivateKey();
            var recipientMlPriv = recipientHybridPriv.MlKemPrivateKey.AsBouncyMlKemPrivateKeyParams();
            var recipientEcPriv = recipientHybridPriv.EccPrivateKey.AsBouncyEcPrivateKeyParams();
            var env = HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes.Create(encryptedBytes.Bytes);

            // ML-KEM decapsulate
            var decapsulator = new MLKemDecapsulator(MLKemParameters.ml_kem_768);
            decapsulator.Init(recipientMlPriv);
            byte[] kemSecret = new byte[decapsulator.SecretLength];
            decapsulator.Decapsulate(env.MlKemEncapsulation.ToByteArray(), 0, env.MlKemEncapsulation.Length, kemSecret, 0, kemSecret.Length);

            // ECDH to recover ecdhSecret
            var agree = new ECDHBasicAgreement();
            agree.Init(recipientEcPriv);
            BigInteger ecdhValue = agree.CalculateAgreement(env.SenderEcPublicKey.AsBouncyEcPublicKeyParams());
            byte[] ecdhSecret = BigIntegers.AsUnsignedByteArray(agree.GetFieldSize(), ecdhValue);

            // HKDF derive same AEAD key/nonce
            byte[] combined = Arrays.Concatenate(kemSecret, ecdhSecret);
            byte[] salt = null; // define salt? well the keys are randomly generated few lines above, and keys will be used once. I dont think the salt here makes any difference.
            byte[] keyMaterial = HkdfSha256(combined, salt, env.AssociatedData.ToByteArray(), 32 + 12);
            byte[] aeadKey = Arrays.CopyOfRange(keyMaterial, 0, 32);
            byte[] aeadNonce = Arrays.CopyOfRange(keyMaterial, 32, 32 + 12);

            // decrypt
            byte[] plaintext = CryptoUtils.DecryptWithAesGcmHybrid(aeadKey, aeadNonce, env.Bytes.ToByteArray(), env.AssociatedData.ToByteArray());
            return plaintext;
        }

        public static byte[] GetUsernamePasswordHashHash(string username, byte[] passwordHashBytes)
        {
            // hardcoded to sha3x256
            var hash = GetHash(GetBytesFromUtfString(username.ToLower().Trim()).Concat(passwordHashBytes).ToArray(), HashAlgo.Sha3X256);
            // here we want just the hash bytes, NOT the entire Proto obj bytes, otherwise what if CryptoHash proto changes? (Although not likely b/c that is simple enough, but still)
            return hash.HashBytes.ToByteArray();
        }

        public static byte[] GetPasswordHashPartial(byte[] usernamePasswordHashHash, byte[] enclaveSalt, int bitsLength = 16)
        {
            var bytes = new byte[usernamePasswordHashHash.Length + enclaveSalt.Length];
            usernamePasswordHashHash.CopyTo(bytes, 0);
            enclaveSalt.CopyTo(bytes, usernamePasswordHashHash.Length);
            var hash = GetHash(bytes, HashAlgo.Sha3X256);
            var hashBytes = hash.HashBytes.ToByteArray();
            if (bitsLength != 16)
                throw new ApplicationException($"GetPasswordHashPartial - bitsLength={bitsLength} is not supported");
            
            var res = new byte[] { hashBytes[0], hashBytes[1] };
            return res;

        }


        #endregion
    }
}
