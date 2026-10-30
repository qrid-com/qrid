using System;
using System.Collections.Generic;
using System.Text;

namespace Qrid.Common.Extensions
{
    public static class ByteArrayExtensions
    {
        public static string ToString64(this byte[] bytes)
        {
            return bytes != null ? Convert.ToBase64String(bytes) : null;
        }
    }
}
