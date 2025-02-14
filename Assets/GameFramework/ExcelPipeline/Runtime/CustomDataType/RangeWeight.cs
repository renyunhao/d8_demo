using System;
namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：由range weight构成的对象
    /// </summary>
    [Serializable]
    public class RangeWeight
    {
        public RangeRandom range;
        /// <summary>
        /// 权重
        /// </summary>
        public int weight;

        public RangeWeight()
        {

        }

        public RangeWeight(float min, float max, int weight)
        {
            this.range = new RangeRandom();
            this.range.min = min;
            this.range.max = max;
            this.weight = weight;
        }
    }
}