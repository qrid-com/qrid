using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrid.Common.Utils
{
    public class SharedUtils
    {
        public static DateTime GetDateTimeNowTruncatedToMinutes()
        {
            // do NOT want to record exact DateTime with full precision 
            // to avoid somehow leaking info that could possibly be used to attack some potentially weak RNG implementation
            var now = SharedUtils.GetUtcNowUnkinded(); 
            var res = now.Date.AddHours(now.Hour).AddMinutes(now.Minute);
            return res;
        }

        public static Timestamp GetTimestampNowTruncatedToMinutes()
        {
            // Timestamp.FromDateTime() requires DateTimeKind.Utc,
            // and GetDateTimeNowTruncatedToMinutes() returns an "unkinded" (Unspecified) UTC value
            return Timestamp.FromDateTime(DateTime.SpecifyKind(GetDateTimeNowTruncatedToMinutes(), DateTimeKind.Utc));
        }

        public static DateTime GetUtcNowUnkinded()
        {
            DateTime res = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            return res;
        }

        public static bool ParseFullUserName(string fullUserName, out string userName, out string domain)
        {
            var p = fullUserName?.Split('@');
            if (p == null || p.Length != 2)
            {
                //throw new ApplicationException($"Cannot parse fullUserName='{fullUserName}'");
                userName = null;
                domain = null;
                return false;
            }
            userName = p[0];
            domain = p[1];
            return true;
        }

        public static string GetCreateDateString(DateTime? d)
        {
            if (d.HasValue && d.Value > DateTime.MinValue)
            {
                var n = DateTime.Now;

                if (d.Value.Date >= n.Date)
                {
                    // Today (or Tomorrow b/c of this UTC glitch is still not properly fixed - TODO)
                    return d.Value.ToString("h:mm tt");
                }
                if (d.Value.Date.AddDays((double)d.Value.DayOfWeek) == n.Date.AddDays((double)n.DayOfWeek))
                {
                    // This Week
                    return d.Value.ToString("ddd h:mm tt");
                }
                return d.Value.ToString("MM/dd/yyyy");
            }
            else
            {
                return string.Empty;
            }
        }

        public static bool IsIpAddressV4(string hostNameOrIpAddress)
        {
            // do not want to use Regex here for performance reasons
            // also it will say that "......." string is IP Address, so it is not really a correct implementation
            // but it is good enough to distinguish between host name and IP addr, and this is how we use it 
            var chars = hostNameOrIpAddress.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (!(Char.IsDigit(c) || c == '.'))
                    return false;
            }
            return true;
        }

        // this is for debugging DateTimeKind issues (Utc vs Unspecified etc)

        //public static string GetCreateDateString(DateTime? d)
        //{
        //    if (d.HasValue && d.Value > DateTime.MinValue)
        //    {
        //        var kind = string.Empty;
        //        switch (d.Value.Kind)
        //        {
        //            case DateTimeKind.Unspecified:
        //                kind = "??? ";
        //                break;
        //            case DateTimeKind.Utc:
        //                kind = "UTC ";
        //                break;
        //            case DateTimeKind.Local:
        //                kind = "LOC ";
        //                break;
        //            default:
        //                kind = "--- ";
        //                break;
        //        }
        //        return kind + d.Value.ToString("MM/dd h:mm tt");
        //    }
        //    else
        //    {
        //        return string.Empty;
        //    }
        //}

        public static string GetInvariantFolderPath(string folderPath, string appRootPath)
        {
            folderPath = folderPath.Trim().Replace("\\", "/").ToLower();
            appRootPath = appRootPath.Trim().Replace("\\", "/").ToLower();
            if (folderPath.EndsWith("/")) folderPath = folderPath.Substring(0, folderPath.Length - 1);
            if (appRootPath.EndsWith("/")) appRootPath = appRootPath.Substring(0, appRootPath.Length - 1);


            if (string.IsNullOrWhiteSpace(appRootPath)) return folderPath;
            if (!folderPath.StartsWith(appRootPath)) return folderPath;

            var res = folderPath.Substring(appRootPath.Length);
            return res;

        }
    }
}
