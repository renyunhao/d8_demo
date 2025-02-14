using System;
namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：由id weight count构成的对象
    /// </summary>
    [Serializable]
    public class IDWeightCount
    {
        public int id;
        /// <summary>
        /// 权重
        /// </summary>
        public int weight;
        /// <summary>
        /// 数量
        /// </summary>
        public int count;

        public IDWeightCount()
        {

        }

        public IDWeightCount(int id, int weight, int count)
        {
            this.id = id;
            this.weight = weight;
            this.count = count;
        }
    }
}