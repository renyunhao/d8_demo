using FixPointUnity;

/// <summary>
/// 改变攻击速度
/// 这个技能的SkillValue只有1个值，就是技能触发后攻击速度加成系数
/// </summary>
public class LogicSkill_ChangeAttackSpeed : LogicSkillBase
{
    public override bool IsPositive => tableData.effectValue[0] >= 0;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        F64 attackSpeedCoeff = F64.FromDouble(tableData.effectValue[0]);
        releaser.runtimeData.attackSpeedCoeff += attackSpeedCoeff;
        //TODO：攻击速度应该有个下限
    }

    protected override void SkillLoseEffect()
    {
        base.SkillLoseEffect();
        F64 attackSpeedCoeff = F64.FromDouble(tableData.effectValue[0]);
        releaser.runtimeData.attackSpeedCoeff -= attackSpeedCoeff;
        //TODO：攻击速度应该有个下限
    }
}
