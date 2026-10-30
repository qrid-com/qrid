using Org.BouncyCastle.Security;
using Qrid.Common.Proto.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Qrs.Tunnel.Client
{
    public class FileAsymKeyItemProvider : IAsymKeyItemProvider
    {
        private bool _savePairedKeys = true;

        private string _mostRecentKeyId = string.Empty;
        private string _asym_file_prefix = "_asym-key-item-";
        private string _asym_file_pattern = "_asym-key-item-*";

        private string _Folder;
        public string Folder
        {
            get => _Folder;
            set 
            { 
                Directory.CreateDirectory(value); 
                _Folder = value; 
            }
        }

        public FileAsymKeyItemProvider()
        {
        }

        public FileAsymKeyItemProvider(string folder)
        {
            Folder = folder;
        }

        private string GetFileName(string keyId)
        {
            return Path.Combine(Folder, $"{_asym_file_prefix}{keyId}");
        }

        private string GetMostRecentKeyIdFileName()
        {
            return Path.Combine(Folder, $"_most-recent-key-id");
        }

        public List<AsymKeyItem> GetAllAsymKeyItems()
        {
            var res = new List<AsymKeyItem>();
            var files = System.IO.Directory.EnumerateFiles(Folder, _asym_file_pattern);
            foreach (var file in files)
            {
                var aki = GetByFileName(file);
                if (aki != null )
                    res.Add(aki);
            }
            return res;
        }

        public AsymKeyItem GetByFileName(string fileName)
        {
            if (File.Exists(fileName))
            {
                var content = File.ReadAllText(fileName);
                var res = new AsymKeyItem(CryptoKey.Create(content));
                return res;
            }
            else
            {
                return null;
            }
        }

        public AsymKeyItem Get(string keyId)
        {
            var fileName = GetFileName(keyId);
            return GetByFileName(fileName);
        }

        public void SaveAsymKeyItem(AsymKeyItem item)
        {
            if (!_savePairedKeys) 
                return;
            if (item.KeyPair != null)
                throw new ApplicationException("Saving AsymKeyItem with KeyPair is not supported by this method");

            var content = item.PubKey.GetBase64String();
            var fileName = GetFileName(item.KeyHashId);
            File.WriteAllText(fileName, content);
        }

        string IAsymKeyItemProvider.GetMostRecentKeyId()
        {
            string res;
            var fileName = GetMostRecentKeyIdFileName();
            if (File.Exists(fileName))
            {
                res = File.ReadAllText(fileName) ?? string.Empty;
                res = res.Trim();
            }
            else
            {
                res = string.Empty;
            }
            _mostRecentKeyId = res;
            return res;
        }

        void IAsymKeyItemProvider.SetMostRecentKeyId(string keyId)
        {
            if (!_savePairedKeys)
                return;

            keyId = (keyId ?? string.Empty).Trim();
            if (keyId == _mostRecentKeyId)
                return;

            var fileName = GetMostRecentKeyIdFileName();
            File.WriteAllText(fileName, keyId);
            _mostRecentKeyId = keyId;
        }
    }
}
