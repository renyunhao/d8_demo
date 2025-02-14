public interface ILogicStatus
{
    BattleUnitState Name { get; }
    void StatusEnter(LogicBattleUnit current);
    void StatusUpdate(LogicBattleUnit current);
    void StatusQuit(LogicBattleUnit current);
}