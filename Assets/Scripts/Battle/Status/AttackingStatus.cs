public class AttackingStatus : IStatus
{
    public BattleUnitState Name => BattleUnitState.Attacking;

    public void StatusEnter(BattleUnit current)
    {
        BattleSystem.AttackingStatusEnter(current);
    }

    public void StatusQuit(BattleUnit current)
    {
    }

    public void StatusUpdate(BattleUnit current)
    {
        BattleSystem.AttackingStatusUpdate(current);
    }
}
