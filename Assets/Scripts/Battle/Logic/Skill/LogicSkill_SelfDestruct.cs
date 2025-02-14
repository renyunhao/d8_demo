using System.Collections.Generic;

/// <summary>
/// 自爆
/// </summary>
public class LogicSkill_SelfDestruct : LogicSkillBase
{
    public override bool IsPositive => true;

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
