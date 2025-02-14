using System;
using System.Collections.Generic;

namespace GameFramework
{
    public class GenericPool<T> where T : new()
    {
        /// <summary>
        /// 创建一个实例
        /// </summary>
        public event Action<T> Event_CreatePrefab;
        /// <summary>
        /// 从池子中取出的时候进行初始化操作
        /// </summary>
        public event Action<T> Event_OutPool;
        /// <summary>
        /// 返回池子的时候进行归池逻辑处理
        /// </summary>
        public event Action<T> Event_ReturnPool;

        /// <summary>
        /// 池子
        /// </summary>
        protected Queue<T> pool;

        /// <summary>
        ///  所有出池的实例集合
        /// </summary>
        protected HashSet<T> allOutPoolInstance;

        public HashSet<T> AllOutPoolInstance => allOutPoolInstance;

        /// <summary>
        /// 池子当前的容量
        /// </summary>
        public int Capacity => pool.Count;

        public GenericPool()
        {
            pool = new Queue<T>();
            allOutPoolInstance = new HashSet<T>();
        }

        public virtual T GetInstance()
        {
            T instance = default;
            if (pool.Count > 0)
            {
                instance = pool.Dequeue();
            }
            else
            {
                if (instance == null)
                {
                    instance = CreateInstance();
                    Event_CreatePrefab?.Invoke(instance);
                }
            }

            if (allOutPoolInstance.Contains(instance))
            {
                Debug.LogError("此对象已经出池：" + instance.ToString());
            }
            else
            {
                allOutPoolInstance.Add(instance);
            }

            Event_OutPool?.Invoke(instance);
            return instance;
        }

        protected virtual T CreateInstance()
        {
            return new T();
        }

        public virtual void RecycleInstance(T instance)
        {
            if (allOutPoolInstance.Contains(instance) == false)
            {
                Debug.LogError("要回收的对象不属于当前池管理，可能是二次回收：" + instance.ToString());
                return;
            }
            else
            {
                allOutPoolInstance.Remove(instance);
            }
            pool.Enqueue(instance);
            Event_ReturnPool?.Invoke(instance);
        }

        public virtual void RecycleAllInstance()
        {
            foreach (var item in allOutPoolInstance)
            {
                pool.Enqueue(item);
                Event_ReturnPool?.Invoke(item);
            }
            allOutPoolInstance.Clear();
        }

        public virtual void EnsureCapacity(int capacity)
        {
            while (pool.Count < capacity)
            {
                var instance = CreateInstance();
                pool.Enqueue(instance);
            }
        }
    }
}