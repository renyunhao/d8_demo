using FixPointUnity;

/// <summary>
/// 分身，得到和自身一样的角色，自身还在
/// </summary>
public class LogicSkill_Clone : LogicSkillBase
{
    public override bool IsPositive => true;

    public override void SkillStart(bool pending = true)
    {
        base.SkillStart(pending);
        LogicBattleSystem.Instance.OnVictimDead(releaser);
    }

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        int count = int.Parse(tableData.effectValue[1].ToString());

        for (int i = 0; i < count; i++)
        {
            LogicBattleUnit logicBattleUnit = LogicBattleSystem.Instance.GetOneLogicBattleUnit(releaser.id, releaser.IsAttacker);
            F64Vec3 targetPos = LogicBattleSystem.Instance.RandomOnePosition(releaser.runtimeData.pos, F64.FromDouble(1.5f));
            logicBattleUnit.runtimeData.pos = targetPos;
            logicBattleUnit.runtimeData.hp = (int)(releaser.staticData.maxHP * tableData.effectValue[0]);
            LogicBattleSystem.Instance.AddCombatUnit(logicBattleUnit, true, true);
        }
    }
}
