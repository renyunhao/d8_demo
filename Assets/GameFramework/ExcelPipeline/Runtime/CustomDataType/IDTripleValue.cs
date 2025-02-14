using System;
namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：由id value构成的对象
    /// </summary>
    [Serializable]
    public class IDTripleValue
    {
        public int id;
        /// <summary>
        /// 数值
        /// </summary>
        public IDIntValue value;

        public IDTripleValue()
        {

        }

        public IDTripleValue(int id, IDIntValue value)
        {
            this.id = id;
            this.value = value;
        }
    }
}