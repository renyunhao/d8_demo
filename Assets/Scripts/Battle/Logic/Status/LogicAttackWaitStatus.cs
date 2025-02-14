public class LogicAttackWaitStatus : ILogicStatus
{
    public BattleUnitState Name => BattleUnitState.AttackWait;

    public void StatusEnter(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.AttackWaitStatusEnter(current);
    }

    public void StatusUpdate(LogicBattleUnit current)
    {
        LogicBattleSystem.Instance.AttackWaitStatusUpdate(current);
    }

    public void StatusQuit(LogicBattleUnit current)
    {

    }
}
