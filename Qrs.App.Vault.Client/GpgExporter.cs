using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Linq;

namespace Qrs.App.Vault.Client
{
    // Creates OpenPGP symmetric (passphrase) encrypted files (.gpg).
    //
    // AES-256 symmetric encryption is used with the passphrase as the only decryption method,
    // so the produced file can be decrypted with standard OpenPGP tools on a modern host, for
    // example: gpg -d <file>.gpg (gpg prompts for the password).
    public static class GpgExporter
    {
        // Writes a single in-vault file (entry name + raw bytes) into a passphrase-encrypted
        // OpenPGP file at outputPath.
        public static void CreateEncryptedFile(string outputPath, string entryName, byte[] content, string password)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output file path cannot be empty.", nameof(outputPath));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var bytes = content ?? Array.Empty<byte>();
            // In-vault paths use '/' as the separator on all platforms, so take the last segment
            // rather than Path.GetFileName (which would not split on '/' on Windows).
            var name = string.IsNullOrEmpty(entryName) ? "data" : entryName.Split('/').Last();

            using var fileStream = File.Create(outputPath);

            var encGen = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, new SecureRandom());
            encGen.AddMethod(password.ToCharArray(), HashAlgorithmTag.Sha256);

            using var encOut = encGen.Open(fileStream, new byte[1 << 16]);

            var compGen = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
            using var compOut = compGen.Open(encOut);

            var litGen = new PgpLiteralDataGenerator();
            using (var litOut = litGen.Open(compOut, PgpLiteralData.Binary, name, bytes.Length, DateTime.UtcNow))
            {
                litOut.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
