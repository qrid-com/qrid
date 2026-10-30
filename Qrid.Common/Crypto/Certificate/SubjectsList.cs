//using Utils = Qrid.MobileCrypto.Utils;
using Qrid.Common.Proto.Const;
using System.Collections.Generic;

namespace Qrid.Common.Crypto.Certificate
{

    public class SubjectsList
    {
        // data accumulated while Validating certificate chain

        public Dictionary<string, string> Subjects;

        public string App => GetSubject(SubjectType.App);
        public string User => GetSubject(SubjectType.User); // Full Username like "user@domain"
        public string Server => GetSubject(SubjectType.Server);
        public string Domain => GetSubject(SubjectType.Domain);
        public string CertificateAuthority => GetSubject(SubjectType.CertificateAuthority);


        public bool IsValid;
        public string ErrorMessage;

        public SubjectsList(SubjectsList source)
        {
            if (source == null)
            {
                Subjects = new Dictionary<string, string>(6);
                IsValid = true;
                ErrorMessage = string.Empty;
            }
            else
            {
                Subjects = new Dictionary<string, string>(source.Subjects);
                IsValid = source.IsValid;
                ErrorMessage = source.ErrorMessage;
            }
        }

        private bool Overwriting(string oldValue, string newValue)
        {
            if (oldValue == null || newValue == oldValue)
                return false;
            else
                return true;
        }

        public bool AddSubject(string subjectType, string subjectName)
        {
            if (string.IsNullOrWhiteSpace(subjectType)) return true;
            
            string oldValue = null;
            Subjects.TryGetValue(subjectType, out oldValue);

            if (Overwriting(oldValue, subjectName))
            {
                ErrorMessage = $"Error with SubjectType='{subjectType}' - certificate chain cannot contain different values '{oldValue}' and '{subjectName}'";
                IsValid = false;
                return false;
            }
            else
            {
                Subjects[subjectType] = subjectName;
                return true;
            }
        }

        private string GetSubject(string subjectType)
        {
            string res = null;
            Subjects?.TryGetValue(subjectType, out res);
            return res;
        }

        
        public string ToShortStr()
        {
            return "";
            //return $"UserKey={UserKey.ToShortStr()}, KeyObject?.DataBytes='{KeyObject?.DataBytes.ToBase64()}'";
        }

    }
}
