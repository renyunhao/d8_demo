/// <summary>
/// 复活
/// </summary>
public class LogicSkill_Relive : LogicSkillBase
{
    public override bool IsPositive => true;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        int targetHp = (int)(tableData.effectValue[0] * releaser.staticData.maxHP);
        releaser.runtimeData.hp = targetHp;
        releaser.runtimeData.currentStatus = BattleUnitState.Idle;
        if (releaser.runtimeData.freezed)
        {
            releaser.runtimeData.freezed = false;
            releaser.runtimeData.skillFreezeEffects.Clear();
        }
        LogicBattleSystem.Instance.AddCombatUnit(releaser, false, true);
    }
}
