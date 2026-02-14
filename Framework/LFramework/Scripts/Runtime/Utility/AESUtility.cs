using System;
using System.Security.Cryptography;
using System.Text;

namespace LFramework.Runtime
{
    public class AESUtility
    {
        // 密钥长度应为 16、24 或 32 字节（128、192 或 256 位）
        private static readonly byte[] Key = Convert.FromBase64String("sZ5C8k1n+W3vmqx9q+CYz/OJKlhCKmJSiPhEXOH+5M8="); // Base64 解码后的密钥
        private static readonly byte[] IV = Convert.FromBase64String("4T2uo7j6hLlVXVgV8A+zXA=="); // Base64 解码后的初始化向量

        /// <summary>
        /// 使用 AES 加密 byte[]
        /// </summary>
        /// <param name="data">要加密的 byte[] 数据</param>
        /// <returns>加密后的 byte[]</returns>
        public static byte[] Encrypt(byte[] data)
        {
            /*
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
            */
            return data;
        }

        /// <summary>
        /// 使用 AES 解密 byte[]
        /// </summary>
        /// <param name="encryptedData">加密的 byte[] 数据</param>
        /// <returns>解密后的 byte[]</returns>
        public static byte[] Decrypt(byte[] encryptedData)
        {
            /*
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                }
            }*/
            return encryptedData;
        }
    }
}