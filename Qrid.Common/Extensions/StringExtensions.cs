using System;
using System.Collections.Generic;
using System.Text;

namespace Qrid.Common.Extensions
{
    public static class StringExtensions
    {
        public static string ToShortStr(this string s)
        {
            return s?.Substring(0, Math.Min(8, s.Length));
        }

        //Guid.TryParse(uk.WrappingKeyId, out Guid g) ? g : null

        public static Guid? ToGuidNull(this string s)
        {
            Guid? data = Guid.TryParse(s, out Guid _data) ? (Guid?)_data : null;
            return data;
        }

        public static Guid ToGuid(this string s)
        {
            // let it throw
            return Guid.Parse(s);
        }

        public static byte[] ToByteArray64(this string base64str)
        {
            return string.IsNullOrWhiteSpace(base64str) ? null : Convert.FromBase64String(base64str);
        }

        public static string TrimToLower(this string s)
        {
            return s?.Trim().ToLower();
        }

    }
}
