using FixPointUnity;

/// <summary>
/// 改变攻击力
/// 这个技能的SkillValue只有1个值，就是技能触发后攻击力加成系数
/// </summary>
public class LogicSkill_ChangeAttackPower : LogicSkillBase
{
    public override bool IsPositive => tableData.effectValue[0] >= 0;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        F64 attackPowerCoeff = F64.FromDouble(tableData.effectValue[0]);
        releaser.runtimeData.attackPowerCoeff += attackPowerCoeff;
    }

    protected override void SkillLoseEffect()
    {
        base.SkillLoseEffect();
        F64 attackPowerCoeff = F64.FromDouble(tableData.effectValue[0]);
        releaser.runtimeData.attackPowerCoeff -= attackPowerCoeff;
    }
}
