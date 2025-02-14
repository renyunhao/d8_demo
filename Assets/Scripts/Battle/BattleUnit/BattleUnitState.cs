public enum BattleUnitState
{
    Idle,
    /// <summary>
    /// 移动到关底
    /// </summary>
    MoveToBasecamp,
    /// <summary>
    /// 移动攻击，向选定的目标移动
    /// </summary>
    MoveToAttack,
    /// <summary>
    /// 一次攻击
    /// </summary>
    Attacking,
    /// <summary>
    /// 攻击完成
    /// </summary>
    AttackWait,
    /// <summary>
    /// 死亡
    /// </summary>
    Dead,
    /// <summary>
    /// 施放技能状态
    /// </summary>
    PerformSkill,
}