using System;
using System.Collections.Generic;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Proto.Extensions
{
    public static class DictionaryStringCertificateExtensions
    {
        public static void AddCertificateFromString(this Dictionary<string, CR.Certificate> dict, string certString)
        {
            if (!string.IsNullOrWhiteSpace(certString))
            {
                var cert = CR.Certificate.Create(certString);
                var key = cert.AsCertifiedPublicKey.KeyRole;

                dict.Add(key, cert);
            }
        }
    }
}
