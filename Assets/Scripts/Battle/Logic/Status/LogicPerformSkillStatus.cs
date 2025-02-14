public class LogicPerformSkillStatus : ILogicStatus
{
    public BattleUnitState Name => BattleUnitState.PerformSkill;

    public void StatusEnter(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.PerformSkillStatusEnter(current);
    }

    public void StatusUpdate(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.PerformSkillStatusUpdate(current);
    }

    public void StatusQuit(LogicBattleUnit current)
    {
    }
}
