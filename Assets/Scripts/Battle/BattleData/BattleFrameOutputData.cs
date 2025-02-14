using System.Collections.Generic;

public class BattleFrameOutputData
{
    /// <summary>
    /// 战斗时间
    /// </summary>
    public long time;
    /// <summary>
    /// 战斗是否已经结束
    /// </summary>
    public bool isEnd;

    //战斗过程中自动增加的单位是先生成LogicBattleUnit，然后绑定到BattleUnit
    public List<LogicBattleUnit> addAttackerUnits = new List<LogicBattleUnit>();
    public List<LogicBattleUnit> addDefenderUnits = new List<LogicBattleUnit>();

    //战斗过程中状态发生变更的战斗单位
    public List<(LogicBattleUnit logicBattleUnit, BattleUnitState oldStatus, BattleUnitState newStatus)> statusChangedUnits =
        new List<(LogicBattleUnit logicBattleUnit, BattleUnitState oldStatus, BattleUnitState newStatus)>();

    //本帧内发射的子弹
    public List<LogicBulletBase> addBullets = new List<LogicBulletBase>();

    //本帧内施放的技能
    public List<LogicSkillBase> addSkills = new List<LogicSkillBase>();

    //本帧内成功闪避的单位
    public List<LogicBattleUnit> evadeUnits = new List<LogicBattleUnit>();

    /// <summary>
    /// 本帧内在Idle状态现新目标的防守单位
    /// </summary>
    public List<LogicBattleUnit> findTargetUnits = new List<LogicBattleUnit>();

    public void Clear()
    {
        isEnd = false;
        addAttackerUnits.Clear();
        addDefenderUnits.Clear();
        statusChangedUnits.Clear();
        addBullets.Clear();
        addSkills.Clear();
        evadeUnits.Clear();
        findTargetUnits.Clear();
    }
}
