using Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Crypto
{
    public static class QrPassHelper
    {
        private const int RealPasswordLength = 40;

        /// <summary>
        /// Generates a random real password of 40 characters, encrypts it with the encryption password,
        /// and returns the encrypted data as a base64 string suitable for encoding into a QR code.
        /// </summary>
        public static string GenerateAndEncrypt(string encryptionPassword)
        {
            var realPassword = CryptoUtils.GetRandomPassword(RealPasswordLength);
            var encrypted = CryptoUtils.PasswordEncryptString(realPassword, encryptionPassword);
            return encrypted.GetBase64String();
        }

        /// <summary>
        /// Generates a random real password of 40 characters, encrypts it with the encryption password,
        /// and returns the encrypted data as a base64 string. Also outputs the generated real password.
        /// </summary>
        public static string GenerateAndEncrypt(string encryptionPassword, out string realPassword)
        {
            realPassword = CryptoUtils.GetRandomPassword(RealPasswordLength);
            var encrypted = CryptoUtils.PasswordEncryptString(realPassword, encryptionPassword);
            return encrypted.GetBase64String();
        }

        /// <summary>
        /// Decrypts the real password from a QR code string using the encryption password.
        /// </summary>
        public static string Decrypt(string qrCodeString, string encryptionPassword)
        {
            var encrypted = PasswordEncryptedBytes.Create(qrCodeString);
            // null associatedData: QR Pass encryption does not use associated data
            var realPassword = CryptoUtils.PasswordDecryptString(encrypted, encryptionPassword, null);
            return realPassword;
        }
    }
}
