using Qrid.Common.Proto;
using Qrid.Common.Proto.CanonicalCrypto;
using Qrs.Proto;
using System;
using System.Collections.Generic;
using CR = Qrid.Common.Proto.Crypto;
using Messaging = Qrid.Proto.Messaging;

namespace Qrs.Proto.Extensions
{
    public static class TypedDataExtensions
    {
        public static Messaging.Message AsMessage(this TypedData td)
        {
            
                // todo - uncomment this validation and add DataType.Message
                //if (DataType != DataType.CryptoKeyPair)
                //    throw new ApplicationException($"Invalid operation - TypedData.DataType='{DataType}' cannot be converted into CertifiedPublicKey");

                var res = Messaging.Message.Create(td.DataBytes);
                return res;
        }
        
    }
}
