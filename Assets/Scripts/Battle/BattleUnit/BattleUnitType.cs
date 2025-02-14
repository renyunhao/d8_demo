/// <summary>
/// 战斗单位枚举
/// </summary>
public enum BattleUnitType
{
    /// <summary>
    /// 非战斗单位
    /// </summary>
    None = 0,
    /// <summary>
    /// 英雄武将
    /// </summary>
    Hero = 1,
    /// <summary>
    /// 步兵
    /// </summary>
    Footman = 2,
    /// <summary>
    /// 弓兵
    /// </summary>
    Archer = 4,
    /// <summary>
    /// 骑兵
    /// </summary>
    Cavalary = 8,
    /// <summary>
    /// 气球
    /// </summary>
    Balloon = 16,
    /// <summary>
    /// 攻城车
    /// </summary>
    Siege = 32,
}