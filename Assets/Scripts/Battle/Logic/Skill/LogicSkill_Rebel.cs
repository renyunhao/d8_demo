using System.Collections.Generic;

/// <summary>
/// 冰冻
/// </summary>
public class LogicSkill_Rebel : LogicSkillBase
{
    public override bool IsPositive => false;

    private List<LogicBattleUnit> effectBattleUnit = new List<LogicBattleUnit>();

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        foreach (var item in targetList)
        {
            if (item.runtimeData.currentStatus == BattleUnitState.Dead)
            {
                continue;
            }
            int damage = (int)(releaser.GetSkillAttackPower(item) * tableData.effectValue[0]);
            LogicBattleSystem.Instance.OnVictimBloodLoss(releaser, item, damage);
            item.SkillTakeEffect_Freeze(uniqueId);
            effectBattleUnit.Add(item);
        }
    }

    protected override void SkillLoseEffect()
    {
        base.SkillLoseEffect();
        foreach (var item in effectBattleUnit)
        {
            item.SkillFinish_Freeze(uniqueId);
        }
        effectBattleUnit.Clear();
    }

    public override void Clear()
    {
        base.Clear();
        effectBattleUnit.Clear();
    }
}
