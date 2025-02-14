using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 时间管理类的回调  ---有开始时间 --- 回调函数
    /// </summary>
    public class DelayAction
    {
        public float timePast;
        public float delay;
        public long endTimeStamp;
        public System.Action action;
        public bool isRealTime;
    }

    /// <summary>
    /// 简易计时器，方便快速添加延迟执行
    /// </summary>
    public static class Timer
    {
        /// <summary>
        /// 定义一个时间管理器的集合----用LinkedList属于数据结构中的 顺序存储或链式存储
        /// </summary>
        static LinkedList<DelayAction> m_delayActoinList = new LinkedList<DelayAction>();
        /// <summary>
        /// 用于存储一帧内要删除的所有计算对象
        /// </summary>
        static List<DelayAction> willRemoveActionList = new List<DelayAction>(10);
        /// <summary>
        /// 用于存储一帧内要增加的所有计算对象
        /// </summary>
        static List<DelayAction> willAddActionList = new List<DelayAction>(10);
        /// <summary>
        /// 增加延迟回调
        /// </summary>
        /// <param name="time">延迟事件</param>
        /// <param name="action">回调参数</param>
        public static DelayAction AddDelayFunc(float time, System.Action action, bool isRealTime = true)
        {
            //重写回调类
            DelayAction act = new DelayAction();
            act.isRealTime = isRealTime;
            if (act.isRealTime)
            {
                //采用真实时间计时，以时间戳为标准，到达事件+回调延迟时间
                act.endTimeStamp = TimerSystem.TimestampMillisecond + (int)(time * 1000);
            }
            else
            {
                //不采用真实时间，以Time.deltaTime累加为标准
                act.delay = time;
            }

            //回调为传入的回调
            act.action = action;
            //把回调类加入到集合
            willAddActionList.Add(act);
            return act;
        }

        public static void Remove(DelayAction ac)
        {
            if (m_delayActoinList.Contains(ac))
            {
                m_delayActoinList.Remove(ac);
            }
            if (willAddActionList.Contains(ac))
            {
                willAddActionList.Remove(ac);
            }
        }

        public static void Update()
        {
            //新的计时器的添加要延迟一帧进行
            foreach (DelayAction delayAction in willAddActionList)
            {
                m_delayActoinList.AddLast(delayAction);
            }
            willAddActionList.Clear();

            if (m_delayActoinList.Count > 0)
            {
                var dic = m_delayActoinList.GetEnumerator();

                while (dic.MoveNext())
                {
                    DelayAction delayAction = dic.Current;
                    if (delayAction.isRealTime)
                    {
                        if (TimerSystem.TimestampMillisecond >= delayAction.endTimeStamp)
                        {
                            if (delayAction.action != null)
                            {
                                delayAction.action();
                            }
                            willRemoveActionList.Add(delayAction);
                        }
                    }
                    else
                    {
                        delayAction.timePast += Time.deltaTime;
                        if (delayAction.timePast >= delayAction.delay)
                        {
                            if (delayAction.action != null)
                            {
                                delayAction.action();
                            }
                            willRemoveActionList.Add(delayAction);
                        }
                    }

                }

                //必须要在一帧内完成所有计时器的判定
                foreach (DelayAction delayAction in willRemoveActionList)
                {
                    m_delayActoinList.Remove(delayAction);
                }
                willRemoveActionList.Clear();
            }
        }
    }
}