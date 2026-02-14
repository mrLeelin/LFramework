using System;
using System.Security.Cryptography;

namespace LFramework.Runtime
{
    public class AESUtility
    {
        private static byte[] _key;
        private static byte[] _iv;
        private static bool _enabled;

        /// <summary>
        /// 初始化 AES 加密参数，必须在使用前调用
        /// </summary>
        /// <param name="keyBase64">Base64 编码的密钥（16/24/32 字节）</param>
        /// <param name="ivBase64">Base64 编码的初始化向量（16 字节）</param>
        public static void Initialize(string keyBase64, string ivBase64)
        {
            _key = Convert.FromBase64String(keyBase64);
            _iv = Convert.FromBase64String(ivBase64);
            _enabled = true;
        }

        /// <summary>
        /// 使用 AES 加密 byte[]
        /// </summary>
        /// <param name="data">要加密的 byte[] 数据</param>
        /// <returns>加密后的 byte[]</returns>
        public static byte[] Encrypt(byte[] data)
        {
            if (!_enabled || _key == null || _iv == null)
            {
                return data;
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// 使用 AES 解密 byte[]
        /// </summary>
        /// <param name="encryptedData">加密的 byte[] 数据</param>
        /// <returns>解密后的 byte[]</returns>
        public static byte[] Decrypt(byte[] encryptedData)
        {
            if (!_enabled || _key == null || _iv == null)
            {
                return encryptedData;
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                }
            }
        }
    }
}
