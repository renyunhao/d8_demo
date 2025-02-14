using System.Collections.Generic;
using UnityEngine;

public class BattleInputData
{
    /// <summary>
    /// 战斗时间缩放倍数
    /// </summary>
    public static readonly int[] TimeScaleArray = new int[] { 1, 2, 4 };
    /// <summary>
    /// 时间缩放索引
    /// </summary>
    public int timeScaleIndex;
    /// <summary>
    /// 战斗运行速度
    /// </summary>
    public int timeScale;
    /// <summary>
    /// 进攻方阵营ID
    /// </summary>
    public int attackerCampID;
    /// <summary>
    /// 防守方阵营ID
    /// </summary>
    public int defenderCampID;
    /// <summary>
    /// 进攻方大本营
    /// </summary>
    public Basecamp attackerBasecamp;
    /// <summary>
    /// 防守方名字
    /// </summary>
    public string attackerName;
    /// <summary>
    /// 防守方头像
    /// </summary>
    public string attackerAvatar;
    /// <summary>
    /// 进攻方大本营
    /// </summary>
    public Basecamp defenderBasecamp;
    /// <summary>
    /// 防守方名字
    /// </summary>
    public string defenderName;
    /// <summary>
    /// 防守方头像
    /// </summary>
    public string defenderAvatar;
}
