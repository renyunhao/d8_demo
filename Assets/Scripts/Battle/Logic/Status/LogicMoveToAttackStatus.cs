public class LogicMoveToAttackStatus : ILogicStatus
{
    public BattleUnitState Name { get { return BattleUnitState.MoveToAttack; } }

    public void StatusEnter(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.MoveToAttackStatusEnter(current);
    }

    public void StatusUpdate(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.MoveToAttackStatusUpdate(current);
    }

    public void StatusQuit(LogicBattleUnit current)
    {
    }
}
