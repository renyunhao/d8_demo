using System;
namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：由id value构成的对象
    /// </summary>
    [Serializable]
    public class IDIntValue
    {
        public int id;
        /// <summary>
        /// 数值
        /// </summary>
        public int value;

        public IDIntValue()
        {

        }

        public IDIntValue(int id, int value)
        {
            this.id = id;
            this.value = value;
        }
    }
}