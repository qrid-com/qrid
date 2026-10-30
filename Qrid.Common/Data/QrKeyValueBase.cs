using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Qrid.Common.Data
{
    public class QrKeyValueBase
    {
        
        public Dictionary<string, string> Params;
        public string Error;

        public QrKeyValueBase()
        {
            Params = new Dictionary<string, string>();
        }

        public bool IsValid => string.IsNullOrWhiteSpace(Error);

        public string GetParam(string key)
        {
            return Params.ContainsKey(key) ? Params[key] : string.Empty;
        }

        public void SetParam(string key, string value)
        {
            Params[key] = value; // todo: key.ToLower(), or CanonicalKey(key)
        }

        public bool IsValidKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            // only allow "word" characters in key - no spaces, no special characters, no punctuation
            var m = Regex.Match(key, "^\\w+$");
            return m.Success;
        }

        public void Parse(string qr)
        {
            Error = null;
            //Params = null;

            // perform restrictive validation - there could potentially be an attack
            // when user is tricked to scan malicious QR code
            var m = Regex.Match(qr, "[^\u0020-\u007E]");
            if (m.Success)
            {
                Error = $"QR string contains invalid characters with ascii codes outside of 32..126 range";
                return;
            }

            var newParams = new Dictionary<string, string>();
            var parts = qr.Split('&');
            foreach(string p in parts)
            {
                int i = p.IndexOf("=");
                if (i > 0)
                {
                    var key = p.Substring(0, i).ToLower().Trim();
                    if (!IsValidKey(key))
                    {
                        Error = $"Invalid key '{key}'";
                        return;
                    }
                    var value = p.Substring(i + 1);
                    value = Uri.UnescapeDataString(value);
                    newParams.Add(key, value);
                }
                else
                {
                    Error = $"Cannot find '=' in '{p}'";
                    return;
                }
            }
            Params = newParams;
        }

        public string ToQrString()
        {
            string res = string.Empty;
            foreach(string key in Params.Keys)
            {
                var value = Params[key];
                value = Uri.EscapeDataString(value); // base64 may include '/' and '='. In our case '=' may screw the parsing. Not in the above implementation, but still... Let's just encode/decode
                res += $"&{key}={value}";
            }

            return res.StartsWith("&") ? res.Substring(1) : res;
        }

    }
}
