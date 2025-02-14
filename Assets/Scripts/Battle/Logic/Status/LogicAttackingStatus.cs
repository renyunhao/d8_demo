public class LogicAttackingStatus : ILogicStatus
{
    public BattleUnitState Name => BattleUnitState.Attacking;

    public void StatusEnter(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.AttackingStatusEnter(current);
    }

    public void StatusUpdate(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.AttackingStatusUpdate(current);
    }

    public void StatusQuit(LogicBattleUnit current)
    {
    }
}
