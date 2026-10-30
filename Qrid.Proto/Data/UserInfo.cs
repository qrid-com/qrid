using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Data
{
    public partial class UserInfo : Qrid.Common.Proto.ProtoBase
    {

        public static string FullUserName(string username, string domain)
        {
            return username.Trim().ToLower() + "@" + domain.Trim().ToLower();
        }

        public string FullUserName()
        {
            return $"{Username}@{Domain}";
        }

    }
}
