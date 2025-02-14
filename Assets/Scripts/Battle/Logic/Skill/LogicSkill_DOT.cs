using System.Collections.Generic;

/// <summary>
/// 持续伤害
/// </summary>
public class LogicSkill_DOT : LogicSkillBase
{
    public override bool IsPositive => false;

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
        }
    }
}
