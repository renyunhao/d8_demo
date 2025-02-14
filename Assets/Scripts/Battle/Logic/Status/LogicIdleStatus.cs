public class LogicIdleStatus : ILogicStatus
{
    public BattleUnitState Name { get { return BattleUnitState.Idle; } }

    public void StatusEnter(LogicBattleUnit current)
    {

    }

    public void StatusUpdate(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.IdleStatusUpdate(current, true);
    }

    public void StatusQuit(LogicBattleUnit current)
    {

    }
}