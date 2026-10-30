using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.Ocsp;
using Qrid.Common.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using CR = Qrid.Common.Proto.Crypto;
using KeyRole = Qrid.Common.Proto.Const.KeyRole;

namespace Qrid.Common.Crypto.Certificate
{

    public class CertContext
    {
        // data accumulated while Validating certificate chain

        public SubjectsList Subjects;
        public string LeafKeyRole; // leaf KeyRole, i.e. first KeyRole on the other side from root (so it is called "leaf")
        public string KeyRolePath; // KeyRoles concatenated from root to leaf, starts from "/", like "//ca.reputation."

        public bool IsValid = false;
        public string ErrorMessage = string.Empty;
        public string ExplainedContent = string.Empty;

        public CertContext()
        {

        }

        public static CertContext CreateAndVerify(CR.Certificate cert)
        {
            var res = new CertContext();
            res.Validate(cert, CertCache.Instance);
            return res;
        }

        private bool Overwriting(string oldValue, string newValue)
        {
            // cannot overwtite non-empty oldValue with a different value

            if (oldValue == null || newValue == oldValue)
                return false;
            else
                return true;
        }


        
        public void Validate(CR.Certificate certificate, CertCache cache)
        {
            IsValid = false;
            ErrorMessage = string.Empty;
            Subjects = new SubjectsList(null);
            KeyRolePath = string.Empty;
            
            
            int level = 0;
            CertCacheItem cachedItem = null;
            var localItems = new List<CertCacheItem>(8);
            var cert = certificate;
            string previousKeyRole = null;

            // LOOP 1
            // loop through validating certs,
            // until got some cert that is already in cache, or root
            while (cert != null )
            {
                // ===================================================================

                var thisCpk = cert.AsCertifiedPublicKey;

                if (level == 0)
                    LeafKeyRole = thisCpk.KeyRole;
                else
                {
                    // CHAIN OF KEY ROLES - see if THIS KeyRole is allowed ISSUER for previousKeyRole
                    if (!CertHierarchy.Instance.IsValidIssuerRole(previousKeyRole, thisCpk.KeyRole))
                    {
                        ErrorMessage = $"IssuerKeyRole='{thisCpk.KeyRole}' is not allowed for KeyRole='{previousKeyRole}' where Issuer is at level '{level}' in this cert chain counting from the leaf to root";
                        return;
                    }
                }

                var key32 = cert.CertificateData.GetHashCode();
                cachedItem = cache.GetItemCompareAllBytes(key32, cert.CertificateData); // also verifies that cached cert is identical byte by byte
                if (cachedItem != null) break;

                // not in cache
                // validate this level of cert

                // VERIFY CertificateData - either by Issuer's Sig or by special rules for Root-Of-Chain:

                if (cert.IsRootOfChain)
                {
                    // VERIFY as ROOT OF CHAIN
                    // root of chain must be also RootOfTrust 
                    if (!cert.IsRootOfTrust(out var errorMessage, thisCpk))
                    {
                        ErrorMessage = $"Invalid Root-Of-Trust certificate at level {level}, error message is: {errorMessage}";
                        return;
                    }
                    
                    // so far no errors - we've reached End Of Chain AND it ends with Root-Of-Trust PubKey which is self-signed with valid Sig and KeyRole
                    // in the next part, there will be more checks on Subjects and KeyRoles hierarchy
                    // we will now exit the LOOP (but not in the next line)
                }
                else
                {
                    // VERIFY as in the MIDDLE of the CHAIN
                    if (!CryptoUtils.VerifyCertificateHashSig(cert))
                    {
                        ErrorMessage = $"Invalid signature at level {level}, KeyRole='{thisCpk.KeyRole}'";
                        return;
                    }

                    // verify is SubjType is valid for KeyRole
                    if (!CertHierarchy.Instance.IsValidSubjectType(thisCpk.KeyRole, thisCpk.SubjectType))
                    {
                        ErrorMessage = $"SubjectType='{thisCpk.SubjectType}' is not allowed for KeyRole='{thisCpk.KeyRole}' at level {level}";
                        return;
                    }

                    // verify Certificate Dates

                    if (DateTime.UtcNow > thisCpk.NotAfterOrMin)
                    {
                        ErrorMessage = $"Certificate at (level {level}, KeyRole='{thisCpk.KeyRole}') has expired - '{thisCpk.NotAfterOrMin}'";
                        return;
                    }
                    if (DateTime.UtcNow < thisCpk.NotBeforeOrMax)
                    {
                        ErrorMessage = $"Certificate at (level {level}, KeyRole='{thisCpk.KeyRole}') is before its active date - '{thisCpk.NotBeforeOrMax}'";
                        return;
                    }
                }

                // =====================================================================

                // this level of cert seems to be VALID (and not yet in cache)
                // add it to localItems list (will be added to cache later at the end if all is valid)
                var newItem = new CertCacheItem();
                newItem.CertificateData = cert.CertificateData;
                newItem.CertifiedPublicKey = thisCpk;
                newItem.Key32 = key32;
                localItems.Add(newItem);
                level++;

                // NEXT CERT - move to the next cert 
                cert = cert.IssuerCertificate; // null will exit the LOOP
                previousKeyRole = thisCpk.KeyRole;
            }

            // clone subjects from Cached Item
            var subjects = new SubjectsList(cachedItem?.Subjects);
            var keyRolePath = new StringBuilder(cachedItem?.KeyRolePath ?? string.Empty);

            localItems.Reverse();

            // LOOP 2
            // collect and validate Subjects and build KeyRolePath's
            // start from the item (whose Issuer was found in the Cache) and move towards leaf cert
            foreach(var localItem in localItems)
            {
                if (!subjects.AddSubject(localItem.CertifiedPublicKey.SubjectType, localItem.CertifiedPublicKey.SubjectName))
                {
                    ErrorMessage = subjects.ErrorMessage;
                    return;
                }

                // SUBJECTS - clone current subjects into current item
                localItem.Subjects = new SubjectsList(subjects);

                // KEY ROLE PATH
                keyRolePath.Append("/").Append(localItem.CertifiedPublicKey.KeyRole);
                localItem.KeyRolePath = keyRolePath.ToString();

                level--;
            }

            // if both User and Domain are set, make sure User string has the same Domain in it

            var user = subjects.User;
            var domain = subjects.Domain;
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(user))
            {
                string parsedUser;
                string parsedDomain;
                if (!SharedUtils.ParseFullUserName(user, out parsedUser, out parsedDomain))
                {
                    ErrorMessage = $"Cannot parse User = '{user}'";
                    return;
                }
                if (domain != parsedDomain)
                {
                    ErrorMessage = $"Certificate chain contains both User = '{user}' and Domain = '{domain}' that does NOT match domain part of User";
                    return;
                }
                    
            }

            // VALID - if we've made it here, then Certificate is valid
            // add all the items to the cache, only now, after all the validations have been successfull

            foreach (var localItem in localItems)
            {
                cache.AddItem(localItem);
            }

            Subjects = subjects;
            KeyRolePath = keyRolePath.ToString();

            IsValid = true; 
            
            return;
        }

        public void Explain(CR.Certificate certificate)
        {
            IsValid = false;
            ErrorMessage = string.Empty;
            Subjects = new SubjectsList(null);

            var sb = new StringBuilder(1024);

            int level = 0;
            CertCacheItem cachedItem = null;
            var localItems = new List<CertCacheItem>(8);
            var cert = certificate;

            // LOOP 1
            // loop through validating certs,
            // until got some cert that is already in cache
            while (cert != null)
            {
                sb.AppendLine($"======== level {level} =========");
                sb.AppendLine(cert.Explain());

                // do it here just to set a KeyRole for level=0
                var thisCpk = cert.AsCertifiedPublicKey;
                if (level == 0)
                    LeafKeyRole = thisCpk.KeyRole;

                // validate this level of cert

                // verify CertificateData - either by Issuer's Sig or by making sure it contains Root-Of-Trust PubKey:

                if (cert.IsRootOfChain)
                {
                    // root of chain must be also RootOfTrust in order for entire chain to be valid
                    if (!cert.IsRootOfTrust(out var errorMessage, thisCpk))
                    {
                        ErrorMessage = $"The last certificate in the chain (KeyRole='{thisCpk.KeyRole}', level={level}) fails Root-Of-Trust checks with this error: {errorMessage}";
                        sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                    }

                    // exit LOOP 1 b/c we've reached End Of Chain.
                    // There is no Subject and no Sig at this cert. Only a PublicKey
                    sb.AppendLine($"======== ROOT OF TRUST (directly above^^^^) =========");

                    break;
                }

                if (!CryptoUtils.VerifyCertificateHashSig(cert))
                {
                    ErrorMessage = $"Invalid signature at level {level}, KeyRole='{thisCpk.KeyRole}'";
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }

                // verify is SubjType is valid for KeyRole
                if (!CertHierarchy.Instance.IsValidSubjectType(thisCpk.KeyRole, thisCpk.SubjectType))
                {
                    ErrorMessage = $"SubjectType='{thisCpk.SubjectType}' is not allowed for KeyRole='{thisCpk.KeyRole}' at level {level}";
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }

                // verify Certificate Dates

                if (DateTime.UtcNow > thisCpk.NotAfterOrMin)
                {
                    ErrorMessage = $"Certificate at (level {level}, KeyRole='{thisCpk.KeyRole}') has expired - '{thisCpk.NotAfterOrMin}'";
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }
                if (DateTime.UtcNow < thisCpk.NotBeforeOrMax)
                {
                    ErrorMessage = $"Certificate at (level {level}, KeyRole='{thisCpk.KeyRole}') is before its active date - '{thisCpk.NotBeforeOrMax}'";
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }

                // this level of cert seems to be VALID
                // add it to localItems list
                var newItem = new CertCacheItem();
                newItem.CertificateData = cert.CertificateData;
                newItem.CertifiedPublicKey = thisCpk;
                newItem.Key32 = -1;


                // move to the next cert (which is the Issuer)
                // IssuerCertificate is not null here, because we've checked IsRootOfChain above, and would have exited the loop already
                cert = cert.IssuerCertificate;

                // validate if Issuer Key Role is good, while we still have thisCpk holding previous cert's data
                //
                var issuerCpk = cert.AsCertifiedPublicKey;
                var issuerKeyRole = cert.IsRootOfChain ? KeyRole.CA.RootOfTrust : issuerCpk.KeyRole;
                if (!CertHierarchy.Instance.IsValidIssuerRole(thisCpk.KeyRole, issuerKeyRole))
                {
                    ErrorMessage = $"IssuerKeyRole='{issuerKeyRole}' is not allowed for KeyRole='{thisCpk.KeyRole}' at level {level}";
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }

                level++;
                localItems.Add(newItem);
            }

            // clone subjects from Cached Item
            var subjects = new SubjectsList(cachedItem?.Subjects);

            localItems.Reverse();

            // LOOP 2
            // collect and validate Subjects
            // start from the item (whose Issuer was found in the Cache) and move towards leaf cert
            foreach (var localItem in localItems)
            {
                if (!subjects.AddSubject(localItem.CertifiedPublicKey.SubjectType, localItem.CertifiedPublicKey.SubjectName))
                {
                    ErrorMessage = subjects.ErrorMessage;
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }

                // clone current subjects into current item
                localItem.Subjects = new SubjectsList(subjects);

                level--;
            }

            Subjects = subjects; // no more changes to "subjects", just use this obj instance directly, no need to clone

            // if both User and Domain are set, make sure User string has the same Domain in it

            var user = Subjects.User;
            var domain = Subjects.Domain;
            if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(user))
            {
                string parsedUser;
                string parsedDomain;
                if (!SharedUtils.ParseFullUserName(user, out parsedUser, out parsedDomain))
                {
                    ErrorMessage = $"Cannot parse User = '{user}'";
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }
                if (domain != parsedDomain)
                {
                    ErrorMessage = $"Certificate chain contains both User = '{user}' and Domain = '{domain}' that does NOT match domain part of User";
                    sb.AppendLine($"####### ERROR: {ErrorMessage} #######");
                }

            }

            IsValid = string.IsNullOrEmpty(ErrorMessage);
            sb.AppendLine($"==============  DONE !!! ==============");
            ExplainedContent = sb.ToString();
            return;
        }

        public string ToShortStr()
        {
            return "";
            //return $"UserKey={UserKey.ToShortStr()}, KeyObject?.DataBytes='{KeyObject?.DataBytes.ToBase64()}'";
        }

    }
}
