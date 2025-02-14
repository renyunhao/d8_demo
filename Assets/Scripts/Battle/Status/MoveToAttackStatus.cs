public class MoveToAttackStatus : IStatus
{
    public BattleUnitState Name => BattleUnitState.MoveToAttack;

    public void StatusEnter(BattleUnit current)
    {
        BattleSystem.MoveToAttackStatusEnter(current);
    }

    public void StatusUpdate(BattleUnit current)
    {
    }

    public void StatusQuit(BattleUnit current)
    {
    }
}
