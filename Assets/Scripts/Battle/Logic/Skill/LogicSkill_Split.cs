using FixPointUnity;

/// <summary>
/// 分裂，自身变成其他角色，自身不在了
/// </summary>
public class LogicSkill_Split : LogicSkillBase
{
    public override bool IsPositive => true;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        int id = (int)tableData.effectValue[1];
        int count = (int)tableData.effectValue[0];
        for (int i = 0; i < count; i++)
        {
            LogicBattleUnit logicBattleUnit = LogicBattleSystem.Instance.GetOneLogicBattleUnit(id, releaser.IsAttacker);
            F64Vec3 targetPos = LogicBattleSystem.Instance.RandomOnePosition(releaser.runtimeData.pos, F64.FromInt(3));
            logicBattleUnit.runtimeData.pos = targetPos;
            LogicBattleSystem.Instance.AddCombatUnit(logicBattleUnit, true, true);
        }
    }
}
