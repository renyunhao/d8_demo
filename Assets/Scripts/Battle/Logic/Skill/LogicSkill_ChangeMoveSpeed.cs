using FixPointUnity;

/// <summary>
/// 改变移动速度
/// 这个技能的SkillValue只有1个值，就是技能触发后移动速度加成系数
/// </summary>
public class LogicSkill_ChangeMoveSpeed : LogicSkillBase
{
    public override bool IsPositive => tableData.effectValue[0] >= 0;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        F64 moveSpeedCoeff = F64.FromDouble(tableData.effectValue[0]);
        foreach (var target in targetList)
        {
            target.runtimeData.moveSpeedCoeff += moveSpeedCoeff;
        }
    }

    protected override void SkillLoseEffect()
    {
        base.SkillLoseEffect();
        F64 moveSpeedCoeff = F64.FromDouble(tableData.effectValue[0]);
        foreach (var target in targetList)
        {
            target.runtimeData.moveSpeedCoeff -= moveSpeedCoeff;
        }
    }
}
