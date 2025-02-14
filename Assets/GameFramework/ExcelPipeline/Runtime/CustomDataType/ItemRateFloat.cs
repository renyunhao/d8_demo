public class ItemRateFloat
{
    /// <summary>
    /// 物品ID
    /// </summary>
    public int id;
    /// <summary>
    /// 随机概率
    /// </summary>
    public float rate;

    public ItemRateFloat()
    {

    }

    public ItemRateFloat(int id, float _rate)
    {
        this.id = id;
        this.rate = _rate;
    }
}
