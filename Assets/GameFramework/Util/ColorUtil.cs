using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace GameFramework
{
    public static class ColorUtil
    {
        private static Regex regex;

        const string RedColorString = "#FF2121";
        const string GreenColorString = "#50FF21";
        const string GrayColorString = "#404040";



        /// <summary>
        /// 将形如 #ABCDEF 或 #ABCDEFFF 格式的字符串转为Unity Color对象
        /// </summary>
        /// <param name="hexString">Hex颜色字符串</param>
        /// <returns></returns>
        public static Color ToColor(this string hexString)
        {
            if (regex == null)
            {
                regex = new Regex("#?([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})?");
            }
            Match match = regex.Match(hexString);
            if (match.Success)
            {
                float r = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber) / 255f;
                float g = int.Parse(match.Groups[2].Value, NumberStyles.HexNumber) / 255f;
                float b = int.Parse(match.Groups[3].Value, NumberStyles.HexNumber) / 255f;
                float a = 1;

                if (string.IsNullOrEmpty(match.Groups[4].Value) == false)
                {
                    a = int.Parse(match.Groups[4].Value, NumberStyles.HexNumber) / 255f;
                }

                return new Color(r, g, b, a);
            }
            else
            {
                Debug.LogError(string.Format("Input: {0} is not valid hex color string", hexString));
            }
            return Color.black;
        }

        public static Color ToGray(this Color origin)
        {
            float gray = origin.r * 0.299f + origin.g * 0.587f + origin.b * 0.114f;
            return new Color(gray, gray, gray, origin.a);
        }

        public static VertexGradient ToGray(this VertexGradient origin)
        {
            VertexGradient result;
            result.topLeft = origin.topLeft.ToGray();
            result.topRight = origin.topRight.ToGray();
            result.bottomLeft = origin.bottomLeft.ToGray();
            result.bottomRight = origin.bottomRight.ToGray();
            return result;
        }

        /// <summary>
        /// 将 Unity Color对象转为十六进制格式字符串
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ToHexString(this Color color)
        {
            var color32 = (Color32)color;
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color32.r, color32.g, color32.b, color32.a);
        }

        public static Color GetRedColor()
        {
            return ToColor(RedColorString);
        }

        public static Color GetGreenColor()
        {
            return ToColor(GreenColorString);
        }

        public static Color GetGrayColor()
        {
            return ToColor(GrayColorString);
        }
    }
}
