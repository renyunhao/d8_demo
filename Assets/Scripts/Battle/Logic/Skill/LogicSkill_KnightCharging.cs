/// <summary>
/// 骑士冲锋
/// </summary>
public class LogicSkill_KnightCharging : LogicSkillBase
{
    public override bool IsPositive => true;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        foreach (var target in targetList)
        {
            target.runtimeData.isKightCharging = true;
        }
    }

    protected override void SkillUpdate()
    {
        base.SkillUpdate();
        foreach (var target in targetList)
        {
            if (target.runtimeData.currentStatus == BattleUnitState.PerformSkill)
            {
                bool hasTarget = LogicBattleSystem.Instance.GetOnceAttackRelateByAttacker(target) != null;
                if (hasTarget)
                {
                    LogicBattleSystem.Instance.MoveToAttackStatusUpdate(target);
                }
                else
                {
                    LogicBattleSystem.Instance.MoveToEndStatusUpdate(target);
                }
            }
        }
    }

    protected override void SkillLoseEffect()
    {
        base.SkillLoseEffect();
        foreach (var target in targetList)
        {
            target.runtimeData.isKightCharging = false;
        }
    }
}
