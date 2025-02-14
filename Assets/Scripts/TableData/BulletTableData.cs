using GameFramework;
using System;

/// <summary>
/// 子弹属性表_Bullet：子弹类型以及相关属性
/// </summary>
[Serializable]
public class BulletTableData : TableDataBase
{
    /// <summary>
    /// 命中音效
    /// </summary>
    public int hitSound;
    /// <summary>
    /// 结束特效
    /// </summary>
    public string endEffect;
    /// <summary>
    /// 命中特效
    /// </summary>
    public string hitEffect;

}