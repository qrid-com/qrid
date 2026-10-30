using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Qrid.Common.Client.Extensions
{
    public static class TaskExtensions
    {
        public static bool IsCompletedSuccessfully(this Task t)
        {
            return (t.Status == TaskStatus.RanToCompletion) && (!t.IsFaulted) && (!t.IsCanceled);
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

    }
}
