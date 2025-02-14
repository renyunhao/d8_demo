using System;

namespace GameFramework
{
    public delegate void TimerCallback(object paraml);

    public enum TimerStatus 
    {
        Running,
        /// <summary>
        /// 计时器结束后会延迟一帧触发回调，此状态为这一帧
        /// </summary>
        Closing,
        /// <summary>
        /// 标记了正在触发回调的状态
        /// </summary>
        Triggering,
        Closed,
    }

    public class TimerData
    {
        private bool compensation;
        private object paraml;
        private TimerCallback callback;

        public int TimerID { get; private set; }

        public long RemainTimeToMillsecond { get; private set; }

        public float RemainTimeToSecond { get { return RemainTimeToMillsecond * 0.001f; } }

        public long EndTimestampToMillsecond { get; private set; }

        public float EndTimestampToSecond { get { return EndTimestampToMillsecond * 0.001f; } }

        public TimerStatus Status { get; private set; }

        public bool Pausing { get; private set; }

        public void Update()
        {
            if (Pausing)
            {
                return;
            }

            if (Status == TimerStatus.Running)
            {
                RemainTimeToMillsecond = EndTimestampToMillsecond - TimerSystem.TimestampMillisecond;
                RefreshStatus();
                if (Status == TimerStatus.Closing)
                {
                    //将计时结束的Timer加入删除列表，在删除的时候触发结束事件，此处不触发
                    //若在此处触发结束事件，使用使用者可能在事件响应方法中添加一个新的Timer，导致TimerMgr的Update中out sync错误
                    TimerSystem.JoinDeleteList(TimerID, true);
                }
            }
        }

        public void InitializeTimerWithTimestamp(int id, long timestamp, TimerCallback callback, object paraml)
        {
            TimerID = id;
            EndTimestampToMillsecond = timestamp * 1000;
            this.callback = callback;
            this.paraml = paraml;

            compensation = false;
            RemainTimeToMillsecond = EndTimestampToMillsecond - TimerSystem.TimestampMillisecond;
            RefreshStatus();
        }

        public void InitializeTimerWithTimeInterval(int id, float period, TimerCallback callback, object paraml)
        {
            TimerID = id;
            EndTimestampToMillsecond = (long)(period * 1000) + TimerSystem.TimestampMillisecond;
            this.callback = callback;
            this.paraml = paraml;

            compensation = true;
            RemainTimeToMillsecond = (long)(period * 1000);
            RefreshStatus();
        }

        public void TriggerCallback()
        {
            Status = TimerStatus.Triggering;
            callback?.Invoke(paraml);
        }

        public void Pause()
        {
            Pausing = true;
            RemainTimeToMillsecond = EndTimestampToMillsecond - TimerSystem.TimestampMillisecond;
        }

        public void Continue()
        {
            Pausing = false;
            if (compensation)
            {
                EndTimestampToMillsecond = TimerSystem.TimestampMillisecond + RemainTimeToMillsecond;
            }
        }

        public void Close()
        {
            Status = TimerStatus.Closed;
            callback = null;
        }

        public void Clear()
        {
            compensation = false;
            Pausing = false;
            TimerID = 0;
            callback = null;
            paraml = null;

            Status = TimerStatus.Closed;
        }

        private void RefreshStatus()
        {
            Status = TimerSystem.TimestampMillisecond >= EndTimestampToMillsecond ? TimerStatus.Closing : TimerStatus.Running;
        }
    }
}