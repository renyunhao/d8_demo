public class DeadStatus : IStatus
{
    public BattleUnitState Name => BattleUnitState.Dead;

    public void StatusEnter(BattleUnit current)
    {
        BattleSystem.DeadStatusEnter(current);
    }

    public void StatusUpdate(BattleUnit current)
    {
    }

    public void StatusQuit(BattleUnit current)
    {
    }
}
