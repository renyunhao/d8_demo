using Cysharp.Text;
using System;
using System.Globalization;

namespace GameFramework
{
    public static class TextUtil
    {
        /// <summary>
        /// 将数字转成K(千),M(百万),B(十亿)的文本形式，保留一位小数
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string ToKMB(this int num)
        {
            if (num < 1000)
            {
                return num.ToString();
            }

            if (num > 999999999 || num < -999999999)
            {
                return num.ToString("0,,,.#B", CultureInfo.InvariantCulture);
            }
            else if (num > 999999 || num < -999999)
            {
                return num.ToString("0,,.#M", CultureInfo.InvariantCulture);
            }
            else if (num > 999 || num < -999)
            {
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            else
            {
                return num.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 将数字转成K(千),M(百万),B(十亿)的文本形式，保留一位小数
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string ToKMB(this long num)
        {
            if (num < 1000)
            {
                return num.ToString();
            }

            if (num > 999999999 || num < -999999999)
            {
                return num.ToString("0,,,.#B", CultureInfo.InvariantCulture);
            }
            else if (num > 999999 || num < -999999)
            {
                return num.ToString("0,,.#M", CultureInfo.InvariantCulture);
            }
            else if (num > 999 || num < -999)
            {
                return num.ToString("0,.#K", CultureInfo.InvariantCulture);
            }
            else
            {
                return num.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string[] ByteUnit = new string[] { "B", "K", "M", "G" };

        public static string ToByteKMB(this long num)
        {
            double doubleNum = num;

            int unitIndex = 0;
            while (doubleNum >= 1024)
            {
                doubleNum /= 1024;
                unitIndex++;
            }

            return ZString.Concat(doubleNum.ToString("F1"), ByteUnit[unitIndex]);
        }

        public enum VersionCompareResult
        {
            Error = -2,
            Lower = -1,
            Equal = 0,
            Higher = 1
        }

        /// <summary>
        /// 比较版本号，版本号的格式为：X.X.X 其中X只能是数字，.的数量并不限定只能3个
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static VersionCompareResult CompareVersion(this string source, string target)
        {
            VersionCompareResult result = VersionCompareResult.Error;
            var sourceSplits = source.Split('.');
            var targetSplits = target.Split('.');

            if (sourceSplits.Length != targetSplits.Length)
            {
                Debug.LogError($"传入的版本号格式不一致：source {source} target {target}");
                return result;
            }

            try
            {
                for (int i = 0; i < sourceSplits.Length; i++)
                {
                    int sourceNum = Convert.ToInt32(sourceSplits[i]);
                    int targetNum = Convert.ToInt32(targetSplits[i]);
                    result = (VersionCompareResult)sourceNum.CompareTo(targetNum);
                    if (result != VersionCompareResult.Equal)
                    {
                        return result;
                    }
                }
                return result;
            }
            catch
            {
                Debug.LogError($"传入的版本号不是数字格式：source {source} target {target}");
                result = VersionCompareResult.Error;
                return result;
            }
        }
    }
}