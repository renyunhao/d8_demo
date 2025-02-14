using System;

/// <summary>
/// 数据生产线自定义类型：物品+权重
/// </summary>
[Serializable]
public class ItemRate
{
    /// <summary>
    /// 物品ID
    /// </summary>
    public int id;
    /// <summary>
    /// 权重
    /// </summary>
    public int weights;

    public ItemRate()
    {

    }

    public ItemRate(int id, int weights)
    {
        this.id = id;
        this.weights = weights;
    }
}
