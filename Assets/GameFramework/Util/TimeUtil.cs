using Cysharp.Text;
using System;

namespace GameFramework
{
    /// <summary>
    /// 时间工具类，进行各种时间转换
    /// </summary>
    public static partial class TimeUtil
    {
        public static string[] Number = new string[] {
        "00", "01", "02", "03", "04", "05", "06", "07", "08", "09",
        "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
        "30", "31", "32", "33", "34", "35", "36", "37", "38", "39",
        "40", "41", "42", "43", "44", "45", "46", "47", "48", "49",
        "50", "51", "52", "53", "54", "55", "56", "57", "58", "59",
        "60", "61", "62", "63", "64", "65", "66", "67", "68", "69",
        "70", "71", "72", "73", "74", "75", "76", "77", "78", "79",
        "80", "81", "82", "83", "84", "85", "86", "87", "88", "89",
        "90", "91", "92", "93", "94", "95", "96", "97", "98", "99",
        "100", "101", "102", "103", "104", "105", "106", "107", "108", "109",
        "110", "111", "112", "113", "114", "115", "116", "117", "118", "119",
        "120", "121", "122", "123", "124", "125", "126", "127", "128", "129",
        "130", "131", "132", "133", "134", "135", "136", "137", "138", "139",
        "140", "141", "142", "143", "144", "145", "146", "147", "148", "149",
        "150", "151", "152", "153", "154", "155", "156", "157", "158", "159",
        "160", "161", "162", "163", "164", "165", "166", "167", "168", "169",
        "170", "171", "172", "173", "174", "175", "176", "177", "178", "179",
        "180", "181", "182", "183", "184", "185", "186", "187", "188", "189",
        "190", "191", "192", "193", "194", "195", "196", "197", "198", "199",
        "200"};

        public static string[] Number_TrimZero = new string[] {
        "0",  "1",  "2",  "3",  "4",  "5",  "6",  "7",  "8",  "9",
        "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
        "30", "31", "32", "33", "34", "35", "36", "37", "38", "39",
        "40", "41", "42", "43", "44", "45", "46", "47", "48", "49",
        "50", "51", "52", "53", "54", "55", "56", "57", "58", "59",
        "60", "61", "62", "63", "64", "65", "66", "67", "68", "69",
        "70", "71", "72", "73", "74", "75", "76", "77", "78", "79",
        "80", "81", "82", "83", "84", "85", "86", "87", "88", "89",
        "90", "91", "92", "93", "94", "95", "96", "97", "98", "99",
        "100", "101", "102", "103", "104", "105", "106", "107", "108", "109",
        "110", "111", "112", "113", "114", "115", "116", "117", "118", "119",
        "120", "121", "122", "123", "124", "125", "126", "127", "128", "129",
        "130", "131", "132", "133", "134", "135", "136", "137", "138", "139",
        "140", "141", "142", "143", "144", "145", "146", "147", "148", "149",
        "150", "151", "152", "153", "154", "155", "156", "157", "158", "159",
        "160", "161", "162", "163", "164", "165", "166", "167", "168", "169",
        "170", "171", "172", "173", "174", "175", "176", "177", "178", "179",
        "180", "181", "182", "183", "184", "185", "186", "187", "188", "189",
        "190", "191", "192", "193", "194", "195", "196", "197", "198", "199",
        "200"};

        public static int DayToSecond = 86400;
        public static int DayToMinutes = 1440;
        public static long DayToMillSeconds = 86400000;

        private static string DayText = "";
        private static string HoursText = "";
        private static string MinuteText = "";
        private static string SecondText = "";

        public static void SetDateText(string day, string hour, string minute, string second)
        {
            DayText = day;
            HoursText = hour;
            MinuteText = minute;
            SecondText = second;
        }

        public static int ClampTo99(this int num)
        {
            return Math.Clamp(num, 0, 99);
        }

        public static int ClampTo199(this int num)
        {
            return Math.Clamp(num, 0, 199);
        }

        public static string SecondsToHMSTimeText(this int seconds, bool trimHour = false)
        {
            return SecondsToHMSTimeText((long)seconds, trimHour);
        }

        public static string SecondsToHMSTimeText(this float seconds, bool trimHour = false)
        {
            return SecondsToHMSTimeText((long)seconds, trimHour);
        }

        public static string SecondsToHMSTimeText(this long seconds, bool trimHour = false)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            int totalHours = span.Days * 24 + span.Hours;
            if (totalHours == 0 && trimHour)
            {
                return ZString.Format("{0}:{1}", Number[span.Minutes], Number[span.Seconds]);
            }
            return ZString.Format("{0}:{1}:{2}", Number[totalHours.ClampTo99()], Number[span.Minutes], Number[span.Seconds]);
        }

        public static string SecondsToHMSTimeWordText(this long seconds, bool trimHour = false)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            int totalHours = span.Days * 24 + span.Hours;
            if (totalHours == 0 && trimHour)
            {
                return ZString.Format("{0}{1}{2}{3}", Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
            }
            return ZString.Format("{0}{1}{2}{3}{4}{5}", Number[totalHours.ClampTo99()], HoursText, Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
        }

        public static string SecondsToDHMSTimeWordText(float seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.Days >= 1)
            {
                return ZString.Format("{0}{1}{2}{3}{4}{5}{6}{7}", Number[span.Days], DayText, Number[span.Hours], HoursText, Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
            }
            else if (span.Hours >= 1)
            {
                return ZString.Format("{0}{1}{2}{3}{4}{5}", Number[span.Hours], HoursText, Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
            }
            else if (span.Minutes >= 1)
            {
                return ZString.Format("{0}{1}{2}{3}", Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
            }
            else if (span.Seconds > 0)
            {
                return ZString.Format("{0}{1}", Number[span.Seconds], SecondText);
            }
            else
            {
                return ZString.Format("{0}{1}", Number[0], SecondText);
            }
        }

        public static string MillisecondsToHMSTimeText(this long milliseconds, bool trimHour = false)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);
            int totalHours = span.Days * 24 + span.Hours;
            if (totalHours == 0 && trimHour)
            {
                return ZString.Format("{0}:{1}", Number[span.Minutes], Number[span.Seconds]);
            }
            return ZString.Format("{0}:{1}:{2}", Number[totalHours.ClampTo99()], Number[span.Minutes], Number[span.Seconds]);
        }

        public static string MillisecondsToHMSTimeWordText(this long milliseconds, bool trimHour = false)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);
            int totalHours = span.Days * 24 + span.Hours;
            if (totalHours == 0 && trimHour)
            {
                return ZString.Format("{0}{1}", Number[span.Minutes], Number[span.Seconds]);
            }
            return ZString.Format("{0}{1}{2}{3}{4}{5}", Number[totalHours.ClampTo99()], HoursText, Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
        }

        public static string MillisecondsToDHMSTimeWordText(float millseconds)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(millseconds);
            if (span.Days >= 1)
            {
                return ZString.Format("{0}{1}{2}{3}{4}{5}{6}{7}", Number[span.Days], DayText, Number[span.Hours], HoursText, Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
            }
            else if (span.Hours >= 1)
            {
                return ZString.Format("{0}{1}{2}{3}{4}{5}", Number[span.Hours], HoursText, Number[span.Minutes], MinuteText, Number[span.Seconds], SecondText);
            }
            else if (span.Minutes >= 1)
            {
                return ZString.Format("{0}{1}{2}{3}", Number[span.Minutes], Number[span.Seconds], SecondText);
            }
            else if (span.Seconds > 0)
            {
                return ZString.Format("{0}{1}", Number[span.Seconds], SecondText);
            }
            else
            {
                return ZString.Format("{0}{1}", Number[0], SecondText);
            }
        }

        /// <summary>
        /// 将日期转换为时间戳（秒）
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long ConverDateTimeToTimestampSecond(DateTime date, TimeZoneInfo timeZoneInfo)
        {
            DateTime dtStart = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), timeZoneInfo);
            return (long)(date - dtStart).TotalSeconds;
        }

        /// <summary>
        /// 将日期转换为时间戳（毫秒）
        /// </summary>
        /// <param name="date"></param>
        /// <param name="timeZoneInfo"></param>
        /// <returns></returns>
        public static long ConvertDateTimeToTimestampMillisecond(DateTime date, TimeZoneInfo timeZoneInfo)
        {
            return ConverDateTimeToTimestampSecond(date, timeZoneInfo) * 1000;
        }

        /// <summary>
        /// 将时间戳转换为日期
        /// </summary>
        /// <param name=”timestamp”></param>
        /// <returns></returns>
        public static DateTime ConvertTimestampToDateTime(long timestamp, TimeZoneInfo timeZoneInfo)
        {
            DateTime dtStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), timeZoneInfo);
            return dtStart.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// 判断两个时间戳是不是同一天
        /// </summary>
        /// <param name="timestamp1"></param>
        /// <param name="timestamp2"></param>
        /// <returns></returns>
        public static bool IsDifferentDay(long timestamp1, long timestamp2)
        {
            long day1 = timestamp1 / 86400000;
            long day2 = timestamp2 / 86400000;
            return day1 != day2;
        }
    }
}