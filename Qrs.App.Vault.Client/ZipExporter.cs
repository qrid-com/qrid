using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace Qrs.App.Vault.Client
{
    // Creates standard, password-encrypted ZIP files.
    //
    // WinZip AES-256 encryption is used (the strongest key-derivation available for the
    // standard ZIP format: PBKDF2 as defined by the WinZip AE-2 specification). The produced
    // file is a regular .zip that can be decrypted with standard AES-capable ZIP tools on a
    // modern Linux server (for example 7-Zip / p7zip).
    public static class ZipExporter
    {
        public const int AesKeySize = 256;

        // Writes the given in-vault files (entry name + raw bytes) into an encrypted ZIP at zipPath.
        public static void CreateEncryptedZip(string zipPath, IReadOnlyList<(string EntryName, byte[] Content)> entries, string password)
        {
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentException("Zip file path cannot be empty.", nameof(zipPath));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Zip password cannot be empty.", nameof(password));

            var directory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var fileStream = File.Create(zipPath);
            using var zipStream = new ZipOutputStream(fileStream);
            zipStream.SetLevel(6);
            zipStream.Password = password;

            foreach (var (entryName, content) in entries)
            {
                var bytes = content ?? Array.Empty<byte>();
                var entry = new ZipEntry(ZipEntry.CleanName(entryName))
                {
                    DateTime = DateTime.Now,
                    Size = bytes.Length,
                    AESKeySize = AesKeySize,
                };
                zipStream.PutNextEntry(entry);
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.CloseEntry();
            }

            zipStream.Finish();
        }
    }
}
