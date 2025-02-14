using System;

namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：范围
    /// </summary>
    [Serializable]
    public class RangeRandom
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public float min;
        /// <summary>
        /// 最大值
        /// </summary>
        public float max;

        public float GetValue()
        {
            return UnityEngine.Random.Range(min, max);
        }
    }
}