using System;

/// <summary>
/// 数据生产线自定义类型：物品
/// </summary>
public class Article
{
    /// <summary>
    /// 物品ID
    /// </summary>
    public int id;
    /// <summary>
    /// 物品数量
    /// </summary>
    public long count;

    /// <summary>
    /// 子物品数组
    /// </summary>
    public Article[] subArticles = null;

    public Article()
    {

    }

    public Article(int id, long count)
    {
        this.id = id;
        this.count = count;
    }
}
