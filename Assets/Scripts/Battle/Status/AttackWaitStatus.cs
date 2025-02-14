public class AttackWaitStatus : IStatus
{
    public BattleUnitState Name => BattleUnitState.AttackWait;

    public void StatusEnter(BattleUnit current)
    {
        BattleSystem.AttackWaitStatusEnter(current);
    }

    public void StatusQuit(BattleUnit current)
    {
    }

    public void StatusUpdate(BattleUnit current)
    {
        BattleSystem.AttackWaitStatusUpdate(current);
    }
}
