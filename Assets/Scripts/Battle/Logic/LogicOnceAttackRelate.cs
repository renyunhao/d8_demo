using System.Collections.Generic;

/// <summary>
/// 一次攻击相关
/// </summary>
public class LogicOnceAttackRelate
{
    public LogicBattleUnit attacker;
    public LogicBattleUnit mainTarget;

    public List<LogicBattleUnit> secondaryTarget = new List<LogicBattleUnit>();

    public void Clear()
    {
        attacker = null;
        mainTarget = null;
        secondaryTarget.Clear();
    }
}