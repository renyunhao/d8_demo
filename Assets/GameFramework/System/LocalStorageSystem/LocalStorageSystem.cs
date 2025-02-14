using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace GameFramework
{
    public static partial class LocalStorageSystem
    {
        private static string DES_KEY = "desskeyy";
        private static string DES_IV = "dessiviv";

        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        public static bool HasKey(string key)
        {
            string encrypedKey = EncryptString(key);
            return PlayerPrefs.HasKey(encrypedKey);
        }

        public static void DeleteKey(string key)
        {
            string encrypedKey = EncryptString(key);
            PlayerPrefs.DeleteKey(encrypedKey);
            PlayerPrefs.Save();
        }

        #region Private Methods

        private static void SaveIntValue(string key, int finalValue)
        {
            SaveStringValue(key, finalValue.ToString());
        }

        private static void SaveLongValue(string key, long finalValue)
        {
            SaveStringValue(key, finalValue.ToString());
        }

        private static void SaveStringValue(string key, string finalValue)
        {
            string encrypedKey = EncryptString(key);
            string encryptedValue = CombineEncryptKeyAndValue(key, finalValue);
            PlayerPrefs.SetString(encrypedKey, encryptedValue);
            PlayerPrefs.Save();
        }

        private static void SaveBoolValue(string key, bool finalValue)
        {
            SaveIntValue(key, finalValue ? 1 : 0);
        }

        private static bool LoadBoolValue(string key, bool defaultValue)
        {
            string savedString = LoadStringValue(key);
            if (string.IsNullOrEmpty(savedString) == false)
            {
                int realValue = int.Parse(savedString);
                return (realValue == 1);
            }
            else
            {
                SaveBoolValue(key, defaultValue);
                return defaultValue;
            }
        }

        private static int LoadIntValue(string key, int defaultValue)
        {
            string savedString = LoadStringValue(key);
            if (string.IsNullOrEmpty(savedString) == false)
            {
                int realValue = int.Parse(savedString);
                return realValue;
            }
            else
            {
                SaveIntValue(key, defaultValue);
                return defaultValue;
            }
        }

        public static long LoadLongValue(string key, long defaultValue)
        {
            string savedString = LoadStringValue(key);
            if (string.IsNullOrEmpty(savedString) == false)
            {
                long realValue = long.Parse(savedString);
                return realValue;
            }
            else
            {
                SaveLongValue(key, defaultValue);
                return defaultValue;
            }
        }

        private static string LoadStringValue(string key)
        {
            string encrypedKey = EncryptString(key);
            if (PlayerPrefs.HasKey(encrypedKey))
            {
                string savedString = PlayerPrefs.GetString(encrypedKey);
                string[] decryptStrings = SplitDecryptKeyAndValue(savedString);
                if (decryptStrings != null && decryptStrings.Length == 2)
                {
                    if (decryptStrings[0].Equals(key))
                    {
                        return decryptStrings[1];
                    }
                    else
                    {
                        return String.Empty;
                    }
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        private static string CombineEncryptKeyAndValue(string key, string val)
        {
            return EncryptString(key + "@" + val);
        }

        private static string[] SplitDecryptKeyAndValue(string encryptString)
        {
            if (string.IsNullOrEmpty(encryptString) == false)
            {
                string decryptedString = DecryptString(encryptString);
                return decryptedString.Split(new char[] { '@' });
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 加密string
        /// </summary>
        /// <param name="stringToEncrypt">原string</param>
        /// <returns>加密后的string</returns>
        public static string EncryptString(string stringToEncrypt)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);
                des.Key = Encoding.UTF8.GetBytes(DES_KEY);
                des.IV = Encoding.UTF8.GetBytes(DES_IV);
                MemoryStream ms = new MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = System.Convert.ToBase64String(ms.ToArray());
                ms.Close();
                return str;
            }
        }

        /// <summary>
        /// 解密string
        /// </summary>
        /// <param name="stringToDecrypt">加密后的string</param>
        /// <returns>原string</returns>
        public static string DecryptString(string stringToDecrypt)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] inputByteArray = System.Convert.FromBase64String(stringToDecrypt);
                des.Key = Encoding.UTF8.GetBytes(DES_KEY);
                des.IV = Encoding.UTF8.GetBytes(DES_IV);
                MemoryStream ms = new MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Encoding.UTF8.GetString(ms.ToArray());
                ms.Close();
                return str;
            }
        }

        #endregion
    }
}