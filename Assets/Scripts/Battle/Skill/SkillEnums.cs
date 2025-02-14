/// <summary>
/// 技能目标
/// </summary>
public enum SkillTarget
{
    /// <summary>
    /// 范围内所有敌方
    /// </summary>
    AllEnemy = 1,
    /// <summary>
    /// 范围内所有友方
    /// </summary>
    AllAlly = 2,
    /// <summary>
    /// 自身
    /// </summary>
    Self = 3,
    /// <summary>
    /// 普攻打击目标
    /// </summary>
    AttackTarget = 4,
    /// <summary>
    /// 技能范围内所有敌军,排除大本营
    /// </summary>
    AreaAllEnemyWithoutBasecamp = 5
}

/// <summary>
/// 技能阶段
/// </summary>
public enum SkillProgress
{
    /// <summary>
    /// 待启动
    /// </summary>
    Pending,
    /// <summary>
    /// 技能启动
    /// </summary>
    Start,
    /// <summary>
    /// 生效期
    /// </summary>
    TakeEffect,
    /// <summary>
    /// 消失
    /// </summary>
    Disappear,
    /// <summary>
    /// 完成
    /// </summary>
    Completed,
}

/// <summary>
/// 技能触发时机
/// </summary>
public enum SkillTriggerTiming
{
    /// <summary>
    /// 死亡
    /// </summary>
    Dead = 1,
    /// <summary>
    /// 被攻击时
    /// </summary>
    BeAttacked = 2,
    /// <summary>
    /// 攻击时
    /// </summary>
    Attacking = 3,
    /// <summary>
    /// 攻击命中时
    /// </summary>
    AttackHit = 4,
    /// <summary>
    /// 间隔时间触发
    /// </summary>
    AutoTiming = 5,
    /// <summary>
    /// 移动一段时间触发
    /// </summary>
    Moving = 6,
    /// <summary>
    /// 对应前置技能释放结束
    /// </summary>
    PreSkillEnd = 7,
    /// <summary>
    /// 发现目标时
    /// </summary>
    FindTarget = 8,
    /// <summary>
    /// 将要攻击（就是目标在攻击范围内，准备切换到攻击状态的那一个瞬间）
    /// </summary>
    WillAttacking = 9,
    /// <summary>
    /// 距离小于某个值触发
    /// </summary>
    DistanceBelow = 10,
}

/// <summary>
/// 技能结束时机
/// </summary>
public enum SkillEndTiming
{
    /// <summary>
    /// 持续时间结束
    /// </summary>
    DurationEnd = 0,
    /// <summary>
    /// 攻击后结束
    /// </summary>
    AfterAttack = 1,
    /// <summary>
    /// 将要攻击前
    /// </summary>
    WillAttack = 2,
    /// <summary>
    /// 某个技能结束时（只能是同属于同一个技能释放者，同一主技能下的其他子技能
    /// </summary>
    SkillEnd = 3,
}

/// <summary>
/// 技能效果类型（对应Skill表的）
/// </summary>
public enum SkillEffect
{
    None = 0,
    /// <summary>
    /// 范围伤害
    /// </summary>
    AOE = 1,
    /// <summary>
    /// 持续伤害
    /// </summary>
    DOT = 2,
    /// <summary>
    /// 分裂
    /// </summary>
    Split = 3,
    /// <summary>
    /// 冰冻
    /// </summary>
    Freeze = 4,
    /// <summary>
    /// 缚地（把空中单位捕获到地面上）
    /// </summary>
    Capture = 5,
    /// <summary>
    /// 召唤(单次召唤指定数量，无上限)
    /// </summary>
    Summon = 6,
    /// <summary>
    /// 有上限召唤（单次召唤一个，超出上限不再召唤）
    /// </summary>
    SummonWithLimit = 7,
    /// <summary>
    /// 反叛
    /// </summary>
    Rebel = 8,
    /// <summary>
    /// 灼烧（固定伤害+目标已损失生命值百分比）
    /// </summary>
    Burn = 9,
    /// <summary>
    /// 冲锋（向目标移动，时间根据两者之间距离决定）
    /// </summary>
    Rushing = 10,
    /// <summary>
    /// 改变移速
    /// </summary>
    ChangeMoveSpeed = 11,
    /// <summary>
    /// 改变攻速
    /// </summary>
    ChangeAttackSpeed = 12,
    /// <summary>
    /// 改变生命值上限
    /// </summary>
    ChangeMaxHP = 13,
    /// <summary>
    /// 改变攻击力
    /// </summary>
    ChangeAttackPower = 14,
    /// <summary>
    /// 骑士冲锋
    /// </summary>
    KnightCharging = 15,
    /// <summary>
    /// 分身
    /// </summary>
    Clone,
    /// <summary>
    /// 复活
    /// </summary>
    Relive,
    /// <summary>
    /// 自爆
    /// </summary>
    SelfDestruct,
    /// <summary>
    /// 连锁闪电
    /// </summary>
    ChainLightning
}

/// <summary>
/// 技能范围中心
/// </summary>
public enum SkillRangeCenter
{
    /// <summary>
    /// 以技能释放者为中心
    /// </summary>
    Releaser = 0,
    /// <summary>
    /// 以技能目标为中心
    /// </summary>
    Target = 1
}
