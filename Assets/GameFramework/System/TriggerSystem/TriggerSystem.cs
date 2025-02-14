using System;
using System.Collections.Generic;
using System.Linq;

namespace GameFramework.Module.TriggerSystem
{
    /// <summary>
    /// 触发器系统
    /// 主要负责触发器和计数器的绑定和更新逻辑。
    /// </summary>
    public static class TriggerSystem
    {
        public static void BindTrigger<T>(T subject, ICollection<ITrigger> triggers, Action<T> triggerObserver)
        {
            foreach (ITrigger trigger in triggers)
            {
                trigger.BindTrigger(new BeTriggeredProxy<T>(subject, triggerObserver));
            }
        }

        private class BeTriggeredProxy<T> : IBeTriggered
        {
            private readonly T _subject;
            private readonly Action<T> _triggerObserver;

            public BeTriggeredProxy(T subject, Action<T> triggerObserver)
            {
                _subject = subject;
                _triggerObserver = triggerObserver;
            }

            public void Trigger()
            {
                _triggerObserver.Invoke(_subject);
            }
        }

        public static void BindCounter<T>(T subject, ICollection<ICounter> counters,
            Action<T, ICounter, int, int> countObserver, List<int> recoverValues = null)
        {
            List<ICounter> counterList = counters.ToList();
            for (int i = 0; i < counterList.Count; i++)
            {
                BeCountedProxy<T> beCountedProxy;
                if (recoverValues == null)
                    beCountedProxy = new BeCountedProxy<T>(subject, countObserver, counterList[i]);
                else
                    beCountedProxy = new BeCountedProxy<T>(subject, countObserver, counterList[i], recoverValues[i]);
                counterList[i].BindCounter(beCountedProxy);
            }
        }

        private class BeCountedProxy<T> : IBeCounted
        {
            private readonly T _subject;
            private int _count;
            private readonly ICounter _counter;
            private readonly Action<T, ICounter, int, int> _countObserver;

            public BeCountedProxy(T subject, Action<T, ICounter, int, int> countObserver, ICounter counter, int count = 0)
            {
                _subject = subject;
                _countObserver = countObserver;
                _counter = counter;
                _count = count;
            }

            public void ModifyCount(int offset)
            {
                _count += offset;
                _countObserver.Invoke(_subject, _counter, _count, offset);
            }

            public void SetCount(int value)
            {
                int offset = value - _count;
                _count = value;
                _countObserver.Invoke(_subject, _counter, _count, offset);
            }
        }
    }
}