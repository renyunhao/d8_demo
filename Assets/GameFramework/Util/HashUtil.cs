using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace GameFramework
{
    /// <summary>
    /// 提供用于计算指定文件哈希值的方法
    /// <example>例如计算文件的MD5值:
    /// <code>
    ///   String hashMd5 = GameFramework.ComputeMD5("MyFile.txt");
    /// </code>
    /// </example>
    /// <example>例如计算文件的SHA1值:
    /// <code>
    ///   String hashSha1 = GameFramework.ComputeSHA1("MyFile.txt");
    /// </code>
    /// </example>
    /// </summary>
    public static class HashUtil
    {
        public enum HashType
        {
            MD5,
            SHA1
        }

        /// <summary>
        ///  计算指定文件的MD5值
        /// </summary>
        /// <param name="fileName">指定文件的完全限定名称</param>
        /// <returns>返回值的字符串形式</returns>
        public static string ComputeMD5(String fileName)
        {
            string hashMD5 = string.Empty;
            //检查文件是否存在，如果文件存在则进行计算，否则返回空值
            if (File.Exists(fileName))
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    //计算文件的MD5值
                    System.Security.Cryptography.MD5 calculator = System.Security.Cryptography.MD5.Create();
                    Byte[] buffer = calculator.ComputeHash(fs);
                    calculator.Clear();
                    //将字节数组转换成十六进制的字符串形式
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        stringBuilder.Append(buffer[i].ToString("x2"));
                    }
                    hashMD5 = stringBuilder.ToString();
                }
            }
            return hashMD5;
        }

        /// <summary>
        /// 计算指定字符串的MD5值
        /// </summary>
        /// <param name="name">指定字符串</param>
        /// <returns></returns>
        public static string ComputeMD5WithString(string input)
        {
            string hashMD5 = string.Empty;
            if (string.IsNullOrEmpty(input) == false)
            {
                MD5 md5Hash = MD5.Create();
                byte[] buffer = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    stringBuilder.Append(buffer[i].ToString("x2"));
                }
                hashMD5 = stringBuilder.ToString();
            }
            return hashMD5;
        }

        /// <summary>
        ///  计算指定文件的SHA1值
        /// </summary>
        /// <param name="fileName">指定文件的完全限定名称</param>
        /// <returns>返回值的字符串形式</returns>
        public static string ComputeSHA1(String fileName)
        {
            string hashSHA1 = string.Empty;
            //检查文件是否存在，如果文件存在则进行计算，否则返回空值
            if (File.Exists(fileName))
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    //计算文件的SHA1值
                    SHA1 calculator = SHA1.Create();
                    Byte[] buffer = calculator.ComputeHash(fs);
                    calculator.Clear();
                    //将字节数组转换成十六进制的字符串形式
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        stringBuilder.Append(buffer[i].ToString("x2"));
                    }
                    hashSHA1 = stringBuilder.ToString();
                }
            }
            return hashSHA1;
        }

        /// <summary>
        /// 计算给定路径下所有文件的Hash值，并存入指定文件outputFileName中
        /// 第一行是一个版本信息（年月日时分秒）
        /// 剩余每一行格式都是：文件相对路径|文件hash值|文件大小
        /// 相对路径的根目录就是sourceFolder
        /// </summary>
        /// <param name="sourceFolder">源文件路径</param>
        /// <param name="outputFileName">输出文件名，将输出到sourceFolder路径下</param>
        /// <param name="hashType">计算Hash值的方法</param>
        public static string ComputeVersionAndHash(string sourceFolder, string outputFileName, HashType hashType = HashType.MD5)
        {
            StringBuilder sb = new StringBuilder(100);
            string version = DateTime.Now.ToString("yyyyMMddHHmmss");
            sb.AppendLine(version);

            sourceFolder = sourceFolder.Replace("\\", "/");
            DirectoryInfo dir = new DirectoryInfo(sourceFolder);
            FileInfo[] files = dir.GetFiles("*", SearchOption.AllDirectories);

            for (var i = 0; i < files.Length; ++i)
            {
                FileInfo fileInfo = files[i];
                if (fileInfo.Name.EndsWith(".manifest")
                    || fileInfo.Name.EndsWith(".meta")
                    || fileInfo.Name.Equals(outputFileName))
                {
                    continue;
                }
                string hashString = string.Empty;
                if (hashType == HashType.MD5)
                {
                    hashString = ComputeMD5(fileInfo.FullName);
                }
                else
                {
                    hashString = ComputeSHA1(fileInfo.FullName);
                }
                string relativePath = fileInfo.FullName.Replace("\\", "/").Replace(sourceFolder, "").Trim('/');
                sb.Append(relativePath);
                sb.Append("|");
                sb.Append(hashString);
                sb.Append("|");
                sb.Append(fileInfo.Length);
                if (i < files.Length - 1)
                {
                    sb.AppendLine();
                }
            }
            string outputFilePath = Path.Combine(sourceFolder, outputFileName);
            string fileContent = sb.ToString();
            File.WriteAllText(outputFilePath, fileContent);
            return fileContent;
        }
    }
}