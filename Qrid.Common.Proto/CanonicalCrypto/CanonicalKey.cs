using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public class CanonicalKey : ProtoBase
    {
        public virtual Crypto.KeyType KeyType
        {
            get
            {
                // to be overwritten in derived classes
                throw new NotImplementedException();
            }
        }

        public virtual Crypto.KeyAlgo KeyAlgo
        {
            get
            {
                // to be overwritten in derived classes
                throw new NotImplementedException();
            }
        }

    }
}
