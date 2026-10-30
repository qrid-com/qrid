using Qrid.Common.Crypto;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrs.Tunnel.Client
{
    public class AsymKeyItem
    {
        public CR.CryptoKeyPair KeyPair; // may be null if we dont have the Private Key
        public CR.CryptoKey PubKey;
        public string PubKeyString;
        public string KeyHashId;

        public CR.CryptoKey GetPairsPrivateKey()
        {
            return KeyPair?.PrivateKey;
        }

        public CR.CryptoKey GetPairsPublicKey()
        {
            return KeyPair?.PublicKey;
        }

        public AsymKeyItem(CC.EccKeyPair keyPair)
            : this(CR.CryptoKeyPair.Create(keyPair))
        {
        }

        public AsymKeyItem(CR.CryptoKeyPair keyPair)
        {
            KeyPair = keyPair;
            PubKey = GetPairsPublicKey();
            PubKeyString = PubKey.GetBase64String();
            KeyHashId = CryptoUtils.GetPubKeyHashId(PubKey);
        }

        // use this Ctr when we dont have the PrivKey,
        // i.e. this Item represents someone else's PubKey, for example it is a Receiver's PubKey
        public AsymKeyItem(CR.CryptoKey crPubKey)
        {
            KeyPair = null;
            PubKey = crPubKey;
            PubKeyString = PubKey.GetBase64String();
            KeyHashId = CryptoUtils.GetPubKeyHashId(PubKey);
        }

        // use this Ctr when we have scanned PubKey from QR Code
        public AsymKeyItem(string crPubKey)
        {
            KeyPair = null;
            //PubKey = CR.CryptoKey.FromCanonicalKey(CC.EccPublicKey.Create(ccPubKey)); 
            PubKey = CR.CryptoKey.Create(crPubKey);
            PubKeyString = PubKey.GetBase64String();
            KeyHashId = CryptoUtils.GetPubKeyHashId(PubKey);
        }
    }
}
