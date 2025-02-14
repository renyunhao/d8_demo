using System;
namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：由id count构成的对象
    /// </summary>
    [Serializable]
    public class IDCount
    {
        public int id;
        /// <summary>
        /// 数量
        /// </summary>
        public int count;

        public IDCount()
        {

        }

        public IDCount(int id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }
}