using System;
namespace GameFramework
{
    /// <summary>
    /// 数据生产线自定义类型：由id value构成的对象
    /// </summary>
    [Serializable]
    public class IDFloatValue
    {
        public int id;
        /// <summary>
        /// 数值
        /// </summary>
        public float value;

        public IDFloatValue()
        {

        }

        public IDFloatValue(int id, float value)
        {
            this.id = id;
            this.value = value;
        }
    }
}