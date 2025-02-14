public class LogicMoveToEndStatus : ILogicStatus
{
    public BattleUnitState Name { get { return BattleUnitState.MoveToBasecamp; } }

    public void StatusEnter(LogicBattleUnit current)
    {
    }

    public void StatusUpdate(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.MoveToEndStatusUpdate(current);
    }

    public void StatusQuit(LogicBattleUnit current)
    {
    }
}