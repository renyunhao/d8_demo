using GameFramework;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

public class ScheduleFunc
{

    public byte ScheduleId;
    /// <summary>
    /// 结束时间戳（毫秒）
    /// </summary>
    public long FinishTimestamp; 

    /// <summary>
    /// Tick的间隔时长 单位秒
    /// </summary>
    public float Duration;

    /// <summary>
    /// 过去的时间间隔
    /// </summary>
    public float PastTime;

    /// <summary>
    /// 每次Tick执行的回调方法
    /// </summary>
    public System.Func<bool> TickFunc;

}

/// <summary>
/// 仿照Timer实现一个回调Schedule
/// </summary>
public static class ScheduleTimer
{
    private static LinkedList<ScheduleFunc> timerList = new LinkedList<ScheduleFunc>();
    private static ObjectPool<ScheduleFunc> timerPool = new ObjectPool<ScheduleFunc>(CreateScheduleFunc);
    private static List<ScheduleFunc> removeList = new List<ScheduleFunc>();
    private static byte TopScheduleIndex = 100;
    /// <summary>
    /// 启动一个固定时间段回调的计时器
    /// </summary>
    /// <param name="interval">时间间隔(秒)</param>
    /// <param name="func">间隔回调函数</param>
    /// <param name="FinishTime">结束时间戳，为了保持一致性用TimerSystem.TimestampMillisecond获取 </param>
    public static byte AddTick(float interval, System.Func<bool> func, long FinishTime = 0)
    {
        Assert.IsNotNull(func, "TickCallback  func have to be a method.");
        var timer = timerPool.Get();
        timer.ScheduleId = TopScheduleIndex++;
        timer.Duration = interval;
        timer.PastTime = 0;
        timer.TickFunc = func;
        timer.FinishTimestamp = FinishTime;

        timerList.AddLast(timer);
        return timer.ScheduleId;
    }

    public static void RemoveTick(byte scheduleId)
    {
        List<ScheduleFunc> tempList = new();
        var itor = timerList.GetEnumerator();
        while (itor.MoveNext())
        {
            if(itor.Current.ScheduleId == scheduleId)
            {
                tempList.Add(itor.Current);
            }
        }
        for(int i = tempList.Count - 1; i > 0; i--)
        {
            timerList.Remove(tempList[i]);
        }
        tempList.Clear();
    }

    public static void Update()
    {
        if (timerList.Count <= 0) return;
        var itor = timerList.GetEnumerator();
        while (itor.MoveNext())
        {
            var tick = itor.Current;
            tick.PastTime += Time.deltaTime;
            if(tick.PastTime >= tick.Duration)
            {
                var result = tick.TickFunc.Invoke();
                tick.PastTime -= tick.Duration;
#if DEBUG
                if (!result)
                {
                    GameFramework.Debug.LogWarning($"[TickTimer] TickFunc={tick.TickFunc.Method.Name} invoke return false.");
                }
#endif

            }
            if (tick.FinishTimestamp != 0 && tick.FinishTimestamp <= TimerSystem.TimestampMillisecond)
            {
                removeList.Add(tick);
            }

        }

        // 移除
        if(removeList.Count > 0)
        {
            foreach(var i in removeList)
            {
                timerList.Remove(i);
            }
            removeList.Clear();
        }
    }

    #region Implements ObjectPool

    private static ScheduleFunc CreateScheduleFunc()
    {
        return new ScheduleFunc();
    }

    public static void ClearPool()
    {
        if (timerPool != null)
        {
            timerPool.Clear();
            timerPool = null;
        }
    }
    #endregion
}
