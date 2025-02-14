public class MoveToBasecampStatus : IStatus
{
    public BattleUnitState Name => BattleUnitState.MoveToBasecamp;

    public void StatusEnter(BattleUnit current)
    {
        current.PlayMoveAnimation();
    }

    public void StatusUpdate(BattleUnit current)
    {
    }

    public void StatusQuit(BattleUnit current)
    {
    }
}
