public interface IStatus
{
    BattleUnitState Name { get; }

    void StatusEnter(BattleUnit current);

    void StatusUpdate(BattleUnit current);

    void StatusQuit(BattleUnit current);
}
