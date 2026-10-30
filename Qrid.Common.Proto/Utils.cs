using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Qrid.Common.Proto
{
    public class Utils
    {
        // Hash code of byte array by XOR'ing all 4-byte words
        public static int GetHashCode(byte[] data)
        {
            int hashCode = 0;
            int i = 0;

            while (i + 4 <= data.Length)
            {
                int value = BitConverter.ToInt32(data, i);
                hashCode ^= value;
                i += 4;
            }

            if (i < data.Length)
            {
                int remainingBytes = data.Length - i;
                int value = 0;
                for (int j = 0; j < remainingBytes; j++)
                {
                    value |= (data[i + j] << (8 * j));
                }
                hashCode ^= value;
            }

            return hashCode;
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public static bool IsValidUsername(string fullUsername)
        {
            // Username part: only Word \w letters in Username part, no dots, etc, minimum 1 char
            // Domain part: only Word(s), optionally separated by dots, min 1 Word
            return Regex.IsMatch(fullUsername, @"^(\w+)@(\w+)(\.\w+)*$");
        }

        public static bool IsValidDomain(string domain)
        {
            // Domain part: only Word(s), optionally separated by dots, min 1 Word
            return Regex.IsMatch(domain, @"^(\w+)(\.\w+)*$");
        }

        public static bool IsValidEmail(string email)
        {
            // TODO - simplify this regex ?
            const string pattern = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
            return Regex.IsMatch(email, pattern);
        }

        public static char[] RecipientSeparators = ",; ".ToCharArray();
        public static List<string> ParseRecipients(string recipients)
        {
            var res = recipients.Split(RecipientSeparators)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLower())
                .Where(s => IsValidUsername(s))
                .ToList();

            return res;
        }

        public static void StringDifference(StringBuilder diff, string fieldName, string s1, string s2)
        {
            var eq = (s1 ?? string.Empty) == (s2 ?? string.Empty);
            if (!eq)
            {
                // not equal, i.e. different
                var prefix = diff.Length == 0 ? string.Empty : ", ";
                var delta = $"{prefix}{fieldName}(old:'{s1 ?? "null"}' new:'{s2 ?? "null"}')";
                diff.Append(delta);
            }
        }

        public static void StringDifferenceDekPay(StringBuilder diff, string fieldName, string s1dek, string s2pay)
        {
            var eq = (s1dek ?? string.Empty) == (s2pay ?? string.Empty);
            if (!eq)
            {
                // not equal, i.e. different
                var prefix = diff.Length == 0 ? string.Empty : ", ";
                var delta = $"{prefix}{fieldName}(DEK:'{s1dek ?? "null"}' PAYLOAD:'{s2pay ?? "null"}')";
                diff.Append(delta);
            }
        }

        public static void DictionaryDifference(StringBuilder diff, Dictionary<string, string> d1, Dictionary<string, string> d2)
        {
            //var diff1 = d1.Except(d2).ToList();
            //var diff2 = d2.Except(d1).ToList();

            // 1) get superset of all keys, no duplicates
            var keys = d1.Keys.Union(d2.Keys).ToList();
            foreach (var key in keys)
            {
                var b1 = d1.TryGetValue(key, out string value1);
                var b2 = d2.TryGetValue(key, out string value2);
                StringDifference(diff, key, b1 ? value1 : "missing", b2 ? value2 : "missing");
            }
        }

    }
}
