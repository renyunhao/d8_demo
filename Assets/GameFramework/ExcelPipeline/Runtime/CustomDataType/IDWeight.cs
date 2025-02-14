using System;
namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：由id weight构成的对象
    /// </summary>
    [Serializable]
    public class IDWeight
    {
        public int id;
        /// <summary>
        /// 权重
        /// </summary>
        public int weight;

        public IDWeight()
        {

        }

        public IDWeight(int id, int weight)
        {
            this.id = id;
            this.weight = weight;
        }
    }
}