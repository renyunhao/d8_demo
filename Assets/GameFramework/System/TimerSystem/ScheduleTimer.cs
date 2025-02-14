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
    /// ����ʱ��������룩
    /// </summary>
    public long FinishTimestamp; 

    /// <summary>
    /// Tick�ļ��ʱ�� ��λ��
    /// </summary>
    public float Duration;

    /// <summary>
    /// ��ȥ��ʱ����
    /// </summary>
    public float PastTime;

    /// <summary>
    /// ÿ��Tickִ�еĻص�����
    /// </summary>
    public System.Func<bool> TickFunc;

}

/// <summary>
/// ����Timerʵ��һ���ص�Schedule
/// </summary>
public static class ScheduleTimer
{
    private static LinkedList<ScheduleFunc> timerList = new LinkedList<ScheduleFunc>();
    private static ObjectPool<ScheduleFunc> timerPool = new ObjectPool<ScheduleFunc>(CreateScheduleFunc);
    private static List<ScheduleFunc> removeList = new List<ScheduleFunc>();
    private static byte TopScheduleIndex = 100;
    /// <summary>
    /// ����һ���̶�ʱ��λص��ļ�ʱ��
    /// </summary>
    /// <param name="interval">ʱ����(��)</param>
    /// <param name="func">����ص�����</param>
    /// <param name="FinishTime">����ʱ�����Ϊ�˱���һ������TimerSystem.TimestampMillisecond��ȡ </param>
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

        // �Ƴ�
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
