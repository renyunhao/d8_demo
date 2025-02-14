using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace GameFramework
{
    public static class TimerSystem
    {
        public static event Action Event_DayChanged;
        public static event Action Event_WeekChanged;
        public static event Action Event_MonthChanged;

        public readonly static int DayOfSecond = 86400;
        public readonly static int HourOfSecond = 3600;
        public readonly static int MinuteOfSecond = 60;

        private static int onlyID = 1;
        private static Stopwatch stopwatch = new Stopwatch();
        private static GenericPool<TimerData> timerPool = new GenericPool<TimerData>();
        private static Dictionary<int, TimerData> usingTimers = new Dictionary<int, TimerData>();

        private static LinkedList<int> deleteWithCallbackList = new LinkedList<int>();
        private static LinkedList<int> deleteNoCallbackList = new LinkedList<int>();

        //暂停列表（暂停时TimerMgr并不会暂停，回调列表中可能已经存在计时器，这部分计时器也需要暂停掉，用容器暂时存储起来）
        private static LinkedList<int> pauseWithCallbackList = new LinkedList<int>();
        private static LinkedList<int> pauseNoCallbackList = new LinkedList<int>();

        private static long timestampMillisecond;
        public static long TimestampMillisecond => timestampMillisecond + stopwatch.ElapsedMilliseconds;
        public static int TimestampSecond => (int)(TimestampMillisecond / 1000);
        public static TimeZoneInfo CurrentTimeZoneInfo { get; private set; }
        /// <summary>
        /// 当天开始时间戳（毫秒）
        /// </summary>
        public static long StartOfDayTimestampMillSeconds { get; private set; }
        /// <summary>
        /// 当天开始时间戳（秒）
        /// </summary>
        public static long StartOfDayTimestampSecond => StartOfDayTimestampMillSeconds / 1000;
        /// <summary>
        /// 当天结束时间戳（毫秒）
        /// </summary>
        public static long EndOfDayTimestampMillsecond { get; private set; }
        /// <summary>
        /// 当天结束时间戳（秒）
        /// </summary>
        public static long EndOfDayTimestampSecond => EndOfDayTimestampMillsecond / 1000;
        /// <summary>
        /// 距离当天结束剩余的毫秒数
        /// </summary>
        public static long EndOfDayRemainMillsecond => EndOfDayTimestampMillsecond - TimestampMillisecond;
        /// <summary>
        /// 距离当天结束剩余的秒数
        /// </summary>
        public static long EndOfDayRemainSecond => EndOfDayRemainMillsecond / 1000;
        /// <summary>
        /// 当周开始时间戳（毫秒）
        /// </summary>
        public static long StartOfWeekTimestampMillSeconds { get; private set; }
        /// <summary>
        /// 当周开始时间戳（秒）
        /// </summary>
        public static long StartOfWeekTimestampSecond => StartOfWeekTimestampMillSeconds / 1000;
        /// <summary>
        /// 当周结束时间戳（毫秒）
        /// </summary>
        public static long EndOfWeekTimestampMillsecond { get; private set; }
        /// <summary>
        /// 当周结束时间戳（秒）
        /// </summary>
        public static long EndOfWeekTimestampSecond => EndOfWeekTimestampMillsecond / 1000;
        /// <summary>
        /// 距离当天结束剩余的毫秒数
        /// </summary>
        public static long EndOfWeekRemainMillsecond => EndOfWeekTimestampMillsecond - TimestampMillisecond;
        /// <summary>
        /// 距离当天结束剩余的秒数
        /// </summary>
        public static long EndOfWeekRemainSecond => EndOfWeekRemainMillsecond / 1000;
        /// <summary>
        /// 当月结束时间戳(毫秒)
        /// </summary>
        public static long EndOfMonthTimestampMillisecond { get; private set; }
        /// <summary>
        /// 当月结束时间戳(秒)
        /// </summary>
        public static long EndOfMonthTimestampSecond => EndOfMonthTimestampMillisecond / 1000;
        /// <summary>
        /// 距离当天结束剩余的毫秒数
        /// </summary>
        public static long EndOfMonthRemainMillsecond => EndOfMonthTimestampMillisecond - TimestampMillisecond;
        /// <summary>
        /// 距离当天结束剩余的秒数
        /// </summary>
        public static long EndOfMonthRemainSecond => EndOfMonthRemainMillsecond / 1000;

        public static bool Initialize { get; private set; }

        static TimerSystem()
        {
            UpdateUtil.AddUpdate(Update);
        }

        public static void Update(float deltaTime)
        {
            if (Initialize)
            {
                if (TimestampMillisecond >= EndOfMonthTimestampMillisecond)
                {
                    Event_MonthChanged?.Invoke();
                }
                if (TimestampMillisecond >= EndOfWeekTimestampMillsecond)
                {
                    Event_WeekChanged?.Invoke();
                }
                if (TimestampMillisecond >= EndOfDayTimestampMillsecond)
                {
                    Event_DayChanged?.Invoke();
                    UpdateSpecifyTimestamp();
                }
            }

            foreach (var item in usingTimers.Values)
            {
                //item的Update方法中会将结束计时的Timer加入删除列表
                item.Update();
            }

            if (deleteNoCallbackList.Count > 0)
            {
                ProcessDeleteListNoCallback();
            }

            if (deleteWithCallbackList.Count > 0)
            {
                ProcessDeleteListWithCallback();
            }
        }

        #region Public Method

        /// <summary>
        /// 重置服务器时间戳
        /// </summary>
        /// <param name="serverTimestamp">服务器时间戳</param>
        /// <param name="serverTimeZoneOffset">服务器时间相对于UTC+0的偏移量，单位秒</param>
        public static void ResetSeverTimestamp(long serverTimestamp, int serverTimeZoneOffset)
        {
            timestampMillisecond = serverTimestamp;
            TimeSpan span = TimeSpan.FromSeconds(serverTimeZoneOffset);
            CurrentTimeZoneInfo = TimeZoneInfo.CreateCustomTimeZone("Server TimeZone", span, "服务器时区", "服务器时区");
            stopwatch.Restart();

            UpdateSpecifyTimestamp();
        }

        public static void UpdateSpecifyTimestamp()
        {
            DateTime current = TimeUtil.ConvertTimestampToDateTime(TimestampMillisecond, CurrentTimeZoneInfo);

            DateTime startOfDay = current.Date;
            StartOfDayTimestampMillSeconds = TimeUtil.ConvertDateTimeToTimestampMillisecond(startOfDay, CurrentTimeZoneInfo);

            DateTime endOfDay = current.AddDays(1).Date;
            EndOfDayTimestampMillsecond = TimeUtil.ConvertDateTimeToTimestampMillisecond(endOfDay, CurrentTimeZoneInfo);

            var DayOfWeek = current.DayOfWeek;
            int week = (int)current.DayOfWeek;
            if (DayOfWeek == DayOfWeek.Sunday)
                week = 7;
            DateTime startOfWeek = current.AddDays(-week + 1).Date;
            StartOfDayTimestampMillSeconds = TimeUtil.ConvertDateTimeToTimestampMillisecond(startOfWeek, CurrentTimeZoneInfo);
            DateTime endOfWeek = current.AddDays(8 - week).Date;
            EndOfWeekTimestampMillsecond = TimeUtil.ConvertDateTimeToTimestampMillisecond(endOfWeek, CurrentTimeZoneInfo);

            DateTime nextMonth = current.AddMonths(1);
            DateTime endOfMonth = new DateTime(nextMonth.Year, nextMonth.Month, 1);
            EndOfMonthTimestampMillisecond = TimeUtil.ConvertDateTimeToTimestampMillisecond(endOfMonth, CurrentTimeZoneInfo);
        }

        #region 操作所有计时器

        public static void Close()
        {
            //usingTimer
            foreach (var item in usingTimers)
            {
                if (item.Value.Status == TimerStatus.Running)
                {
                    item.Value.Close();
                    JoinDeleteList(item.Key, false);
                }
            }
            ProcessDeleteListNoCallback();

            foreach (var item in deleteWithCallbackList)
            {
                RecycleTimer(item);
            }
            deleteWithCallbackList.Clear();

            foreach (var item in pauseNoCallbackList)
            {
                RecycleTimer(item);
            }
            pauseNoCallbackList.Clear();

            foreach (var item in pauseWithCallbackList)
            {
                RecycleTimer(item);
            }
            pauseWithCallbackList.Clear();
        }

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public static void Pause()
        {
            foreach (var item in usingTimers)
            {
                if (item.Value.Status == TimerStatus.Running)
                {
                    item.Value.Pause();
                }
            }
            foreach (var item in deleteNoCallbackList)
            {
                pauseNoCallbackList.AddLast(item);
            }
            deleteNoCallbackList.Clear();

            foreach (var item in deleteWithCallbackList)
            {
                pauseWithCallbackList.AddLast(item);
            }
            deleteWithCallbackList.Clear();
        }

        /// <summary>
        /// 将所有暂停的计时器恢复
        /// </summary>
        public static void Continue()
        {
            foreach (var item in usingTimers)
            {
                if (item.Value.Pausing)
                {
                    item.Value.Continue();
                }
            }
            foreach (var item in pauseNoCallbackList)
            {
                deleteNoCallbackList.AddLast(item);
            }
            pauseNoCallbackList.Clear();

            foreach (var item in pauseWithCallbackList)
            {
                deleteWithCallbackList.AddLast(item);
            }
            pauseWithCallbackList.Clear();

            ProcessDeleteListNoCallback();
            ProcessDeleteListWithCallback();
        }

        #endregion

        #region 操作单个计时器

        /// <summary>
        /// 开启计时器
        /// </summary>
        /// <param name="timeStamp">结束时间戳（秒）</param>
        /// <param name="callback">计时器结束回调</param>
        /// <param name="paraml">其他参数</param>
        /// <returns></returns>
        public static int StartTimerWithTimestamp(long timeStamp, TimerCallback callback = null, object paraml = null)
        {
            if (timeStamp - TimestampSecond <= 0)
            {
                // Debug.LogError("计时器的计时时间存在问题，建议开启计时器之前先判断一下是否有必要开启计时器：" + timeStamp);
                return 0;
            }
            onlyID++;
            TimerData data = timerPool.GetInstance();
            data.InitializeTimerWithTimestamp(onlyID, timeStamp, callback, paraml);
            if (usingTimers.ContainsKey(onlyID) == false)
            {
                usingTimers.Add(onlyID, data);
            }
            else
            {
                Debug.LogError("错误：开启计时器时，，使用了一个正在使用的计时器");
            }
            return onlyID;
        }

        /// <summary>
        /// 开启计时器传入时间为时间段
        /// </summary>
        /// <param name="period">单位：秒</param>
        /// <param name="callback"></param>
        /// <param name="pararml"></param>
        /// <returns></returns>
        public static int StartTimerWithTimeInterval(float period, TimerCallback callback = null, object paraml = null)
        {
            if (period <= 0)
            {
                Debug.LogError("计时器的计时时间存在问题，建议开启计时器之前先判断一下是否有必要开启计时器：" + period);
                return 0;
            }
            onlyID++;
            TimerData data = timerPool.GetInstance();
            data.InitializeTimerWithTimeInterval(onlyID, period, callback, paraml);
            if (usingTimers.ContainsKey(onlyID) == false)
            {
                usingTimers.Add(onlyID, data);
            }
            else
            {
                Debug.LogError("错误：开启计时器时，，使用了一个正在使用的计时器");
            }
            return onlyID;
        }

        public static void Close(int timerID)
        {
            if (usingTimers.ContainsKey(timerID))
            {
                if (usingTimers[timerID].Status == TimerStatus.Running)
                {
                    usingTimers[timerID].Close();
                    JoinDeleteList(timerID, false);
                }
                else if (usingTimers[timerID].Status == TimerStatus.Closing)
                {
                    //closing状态标记了：计时器已经结束但是回调尚未触发；这时关闭需要清理掉回调
                    if (deleteWithCallbackList.Contains(timerID))
                    {
                        deleteWithCallbackList.Remove(timerID);
                        JoinDeleteList(timerID, false);
                    }
                }
                else if (usingTimers[timerID].Status == TimerStatus.Triggering)
                {
                    //Debug.LogError("错误的计时器调用,在回调触发中关闭了计时器");
                }
                else
                {
                    //Debug.LogError("需要关闭的计时器，已经关闭");
                }
            }
            else
            {
                //Debug.LogError("需要关闭的计时器没有在正在使用的列表中");
            }
        }

        public static TimerStatus GetTimerStatus(int timerID)
        {
            TimerStatus status;
            if (usingTimers.ContainsKey(timerID))
            {
                status = usingTimers[timerID].Status;
            }
            else
            {
                status = TimerStatus.Closed;
            }
            return status;
        }

        public static TimerData GetTimerData(int timerID)
        {
            if (usingTimers.ContainsKey(timerID))
            {
                return usingTimers[timerID];
            }
            return null;
        }

        public static long GetRemainTimeToMillsecond(int timerID)
        {
            long result = 0;
            if (usingTimers.ContainsKey(timerID))
            {
                if (usingTimers[timerID].Status == TimerStatus.Running)
                {
                    result = usingTimers[timerID].RemainTimeToMillsecond;
                }
                else
                {
                    Debug.LogError("获取计时器的剩余时间，，此计时器已关闭或暂停");
                }
            }
            else
            {
                Debug.LogError("获取不存在的计时器剩余时间");
            }
            return result;
        }

        public static float GetRemainTimeToSecond(int timerID)
        {
            float result = 0;
            if (usingTimers.ContainsKey(timerID))
            {
                if (usingTimers[timerID].Status != TimerStatus.Closed)
                {
                    result = usingTimers[timerID].RemainTimeToSecond;
                }
                else
                {
                    Debug.LogError("获取计时器的剩余时间，，此计时器已关闭或暂停");
                }
            }
            else
            {
                Debug.LogError("获取计时器的剩余时间，，此计时器不存在");
            }
            return result;
        }

        public static long GetEndTimeToMillsecond(int timerID)
        {
            long result = 0;
            if (usingTimers.ContainsKey(timerID))
            {
                if (usingTimers[timerID].Status == TimerStatus.Running)
                {
                    result = usingTimers[timerID].EndTimestampToMillsecond;
                }
                else
                {
                    Debug.LogError("获取计时器的结束时间，，此计时器已关闭或暂停");
                }
            }
            else
            {
                Debug.LogError("获取计时器的剩余时间，，此计时器不从在");
            }
            return result;
        }

        public static float GetEndTimeToSecond(int timerID)
        {
            float result = 0;
            if (usingTimers.ContainsKey(timerID))
            {
                if (usingTimers[timerID].Status != TimerStatus.Closed)
                {
                    result = usingTimers[timerID].EndTimestampToSecond;
                }
                else
                {
                    Debug.LogError("获取计时器的剩余时间，，此计时器已关闭或暂停");
                }
            }
            else
            {
                Debug.LogError("获取计时器的剩余时间，，此计时器不从在");
            }
            return result;
        }

        #endregion

        #endregion

        #region 内部方法

        public static void JoinDeleteList(int timerID, bool triggerCallback)
        {
            if (usingTimers.ContainsKey(timerID))
            {
                if (triggerCallback)
                {
                    if (deleteWithCallbackList.Contains(timerID) == false)
                    {
                        deleteWithCallbackList.AddLast(timerID);
                    }
                    else
                    {
                        Debug.LogError("将计时器重复加入回调列表");
                    }
                }
                else
                {
                    if (deleteNoCallbackList.Contains(timerID) == false)
                    {
                        deleteNoCallbackList.AddLast(timerID);
                    }
                    else
                    {
                        Debug.LogError("将计时器重复的加入没有回调列表中");
                    }
                }
            }
        }

        private static void ProcessDeleteListWithCallback()
        {
            while (deleteWithCallbackList.Count > 0)
            {
                int timerId = deleteWithCallbackList.First.Value;
                deleteWithCallbackList.RemoveFirst();

                TimerData timerData = usingTimers[timerId];
                //触发结束回调
                timerData.TriggerCallback();
                //回收Timer
                RecycleTimer(timerId);
            }
        }

        private static void ProcessDeleteListNoCallback()
        {
            //遍历删除列表，触发结束回调，并移除Timer
            foreach (var timerID in deleteNoCallbackList)
            {
                RecycleTimer(timerID);
            }

            //清空删除列表
            deleteNoCallbackList.Clear();
        }

        private static void RecycleTimer(int timerID)
        {
            if (usingTimers.ContainsKey(timerID))
            {
                usingTimers[timerID].Clear();
                timerPool.RecycleInstance(usingTimers[timerID]);
                usingTimers.Remove(timerID);
            }
            else
            {
                Debug.LogError("要回收的计时器不在使用列表中");
            }
        }

        #endregion
    }
}