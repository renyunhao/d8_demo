public class LogicDeadStatus : ILogicStatus
{
    public BattleUnitState Name { get { return BattleUnitState.Dead; } }

    public void StatusEnter(LogicBattleUnit current)
    {
    }

    public void StatusUpdate(LogicBattleUnit current)
    {
    }

    public void StatusQuit(LogicBattleUnit current)
    {

    }
}