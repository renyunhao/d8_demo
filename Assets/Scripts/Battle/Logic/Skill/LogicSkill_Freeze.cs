using System.Collections.Generic;

/// <summary>
/// 冰冻
/// </summary>
public class LogicSkill_Freeze : LogicSkillBase
{
    public override bool IsPositive => false;

    private List<LogicBattleUnit> effectBattleUnit = new List<LogicBattleUnit>();

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        foreach (var target in targetList)
        {
            if (target.runtimeData.currentStatus == BattleUnitState.Dead)
            {
                continue;
            }
            int damage = (int)(releaser.GetSkillAttackPower(target) * tableData.effectValue[0]);
            LogicBattleSystem.Instance.OnVictimBloodLoss(releaser, target, damage);
            //受到冰冻伤害后，还未死亡的角色播放冰冻效果，且移除一些增益
            target.SkillTakeEffect_Freeze(uniqueId);
            effectBattleUnit.Add(target);
            //冰冻技能会清除目标身上所有移动速度增益技能和冲锋技能
            LogicBattleSystem.Instance.RemoveAllPositiveSkillOfEffect(target, SkillEffect.ChangeMoveSpeed);
            LogicBattleSystem.Instance.RemoveAllSkill(target, typeof(LogicSkill_Rushing));
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
