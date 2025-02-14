/// <summary>
/// 非常简单的使用线性同余法的随机器
/// </summary>
public class SimpleRandom
{
    readonly int a = 1664525;
    readonly int c = 1013904223;
    readonly long m = 1L << 32;

    public long Previous { get; private set; }

    /// <summary>
    /// 使用种子初始化
    /// </summary>
    /// <param name="seed">种子</param>
    public SimpleRandom(uint seed)
    {
        Previous = seed;
    }

    /// <summary>
    /// 强制设定前一次的结果，将便下一次结果与输入值相关
    /// </summary>
    /// <param name="previous"></param>
    public virtual void SetPrevious(uint previous)
    {
        this.Previous = previous;
    }

    /// <summary>
    /// 获取下一个整型值
    /// </summary>
    /// <returns></returns>
    public virtual uint Next()
    {
        uint num = (uint)((a * Previous + c) % m);
        Previous = num;
        return num;
    }

    /// <summary>
    /// 获取下一个随机int值
    /// </summary>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    /// <returns></returns>
    public virtual int Next(int minValue, int maxValue)
    {
        if (minValue == maxValue)
        {
            return minValue;
        }
        else if (minValue < maxValue)
        {
            return (int)((Next() * 1.0 / m) * (maxValue - minValue + 1) + minValue);
        }
        else
        {
            return (int)((Next() * 1.0 / m) * (maxValue - minValue + 1) + maxValue);
        }
    }
}
