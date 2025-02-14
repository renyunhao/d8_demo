using System.Collections.Generic;

/// <summary>
/// 范围伤害
/// </summary>
public class LogicSkill_AOE : LogicSkillBase
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
            if (tableData.targetSelect == (int)SkillTarget.AreaAllEnemyWithoutBasecamp &&
                item.staticData.isBasecamp)
            {
                continue;
            }
            if (releaser == LogicBattleSystem.Instance.attackerBasecamp)
            {
                LogicBattleSystem.Instance.OnVictimBloodLoss(releaser, item, (int)tableData.warshipEffectValue[0]);
            }
            else
            {
                int damage = (int)(releaser.GetSkillAttackPower(item) * tableData.effectValue[0]);
                LogicBattleSystem.Instance.OnVictimBloodLoss(releaser, item, damage);
            }
        }
    }
}
