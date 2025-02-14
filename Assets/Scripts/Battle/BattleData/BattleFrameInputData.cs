using FixPointUnity;
using System.Collections.Generic;

public class BattleFrameInputData
{
    /// <summary>
    /// 战斗时间
    /// </summary>
    public long time;
    /// <summary>
    /// 玩家主动结束战斗
    /// </summary>
    public bool isEnd;
    /// <summary>
    /// 添加战斗单位的数据
    /// </summary>
    public AddBattleUnitData addBattleUnitData;
}

/// <summary>
/// 添加战斗单位的数据
/// </summary>
public class AddBattleUnitData
{
    /// <summary>
    /// 阵营ID，用于判断是进攻方还是防守方
    /// </summary>
    public int campID;
    /// <summary>
    /// 士兵ID
    /// </summary>
    public int soldierID;

    /// <summary>
    /// 数量
    /// </summary> 
    public int count;
}
