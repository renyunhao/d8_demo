using FixPointUnity;

/// <summary>
/// 改变生命值上限
/// 这个技能的SkillValue只有1个值，就是技能触发后生命值上限加成系数
/// </summary>
public class LogicSkill_ChangeMaxHP : LogicSkillBase
{
    public override bool IsPositive => tableData.effectValue[0] >= 0;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        F64 maxHPCoeff = F64.FromDouble(tableData.effectValue[0]);
        releaser.runtimeData.maxHPCoeff += maxHPCoeff;
        //TODO:改变生命值上限时，是否要按比例改变当前生命值
    }

    protected override void SkillLoseEffect()
    {
        base.SkillLoseEffect();
        F64 maxHPCoeff = F64.FromDouble(tableData.effectValue[0]);
        releaser.runtimeData.maxHPCoeff -= maxHPCoeff;
        //TODO:改变生命值上限时，是否要按比例改变当前生命值
    }
}
