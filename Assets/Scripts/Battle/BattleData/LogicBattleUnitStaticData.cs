using FixPointUnity;
using GameFramework;

public class LogicBattleUnitStaticData
{
    public BattleUnitType battleUnitType;
    public int maxHP;
    /// <summary>
    /// 体积半径
    /// </summary>
    public F64 volumeRadius;
    /// <summary>
    /// 视野半径
    /// </summary>
    public F64 guardRadius;
    /// <summary>
    /// 移动速度
    /// </summary>
    public F64 moveSpeed;
    /// <summary>
    /// 技能攻击力
    /// </summary>
    public int[] skillPower;
    /// <summary>
    /// 普攻攻击力
    /// </summary>
    public int[] generalPower;
    /// <summary>
    /// 攻击优先级
    /// </summary>
    public int attackPriority;
    /// <summary>
    /// 目标数量
    /// </summary>
    public int targetCount;
    /// <summary>
    /// 目标类型(空中，地面)
    /// </summary>
    public int targetType;
    /// <summary>
    /// 战斗单位类型（空中，地面）
    /// </summary>
    public int unitType;
    /// <summary>
    /// 闪避率
    /// </summary>
    public int evade;
    /// <summary>
    /// 技能列表
    /// </summary>
    public int[] skillList;
    /// <summary>
    /// 陷阱触发时间
    /// </summary>
    public long triggerTime;
    /// <summary>
    /// 陷阱触发范围
    /// </summary>
    public F64 triggerRange;
    /// <summary>
    /// 质量，决定碰撞时双方的位移
    /// </summary>
    public int mass;
    /// <summary>
    /// 站在大本营上，会攻击敌人，不会移动，不会被攻击，和大本营共存亡
    /// </summary>
    public bool isOnBasecamp;
    /// <summary>
    /// 是否为飞行生物（飞行生物不需要寻路，不受墙阻碍，始终向目标直线前进）
    /// </summary>
    public bool isFly;

    #region 没有二套字段的攻击参数
    /// <summary>
    /// 最小攻击半径
    /// </summary>
    public F64 minAttackDistance;
    /// <summary>
    /// 攻击范围角度，0表示整个360度，非0值就表示其度数
    /// </summary>
    public int attackAngle;
    /// <summary>
    /// 攻击间隔
    /// </summary>
    public long attackInterval;
    /// <summary>
    /// 攻击次数
    /// </summary>
    public int attackNumber;
    /// <summary>
    /// 子弹是否跟随目标
    /// </summary>
    public bool bulletCanFollowTarget;
    #endregion

    #region 攻击参数
    /// <summary>
    /// 最大攻击半径
    /// </summary>
    public F64 maxAttackDistance;
    /// <summary>
    /// 攻击状态时间
    /// </summary>
    public long attackTime;
    /// <summary>
    /// 攻击动画时间
    /// </summary>
    public long attackAnimationTime;
    /// <summary>
    /// 攻击前摇时间
    /// </summary>
    public long attackFowardTime;
    /// <summary>
    /// 攻击后摇时间
    /// </summary>
    public long attackWaitTime;
    /// <summary>
    /// 子弹id
    /// </summary>
    public int bulletId;
    /// <summary>
    /// 子弹速度
    /// </summary>
    public F64 bulletSpeed;
    /// <summary>
    /// 子弹飞行距离，如果值为0，以目标位置为准，值大于0，则以值为准
    /// </summary>
    public F64 bulletDistance;
    #endregion

    #region 攻击参数
    /// <summary>
    /// 最大攻击半径
    /// </summary>
    public F64 MaxAttackDistance => maxAttackDistance;
    /// <summary>
    /// 攻击状态时间
    /// </summary>
    public long AttackTime => attackTime;
    /// <summary>
    /// 攻击动画时间
    /// </summary>
    public long AttackAnimationTime => attackAnimationTime;
    /// <summary>
    /// 攻击前摇时间
    /// </summary>
    public long AttackFowardTime => attackFowardTime;
    /// <summary>
    /// 攻击后摇时间
    /// </summary>
    public long AttackWaitTime => attackWaitTime;
    /// <summary>
    /// 子弹id
    /// </summary>
    public int BulletId => bulletId;
    /// <summary>
    /// 子弹速度
    /// </summary>
    public F64 BulletSpeed => bulletSpeed;
    /// <summary>
    /// 子弹飞行距离，如果值为0，以目标位置为准，值大于0，则以值为准
    /// </summary>
    public F64 BulletDistance => bulletDistance;
    #endregion

    public bool isBasecamp;
    public bool isWall;

    public void Clear()
    {
        battleUnitType = BattleUnitType.None;
        maxHP = 0;
        volumeRadius = F64.Zero;
        guardRadius = F64.Zero;
        moveSpeed = F64.Zero;
        generalPower = null;
        skillPower = null;
        attackPriority = 0;
        minAttackDistance = F64.Zero;
        targetCount = 0;
        skillList = null;
        bulletCanFollowTarget = false;
        triggerTime = 0;
        triggerRange = F64.Zero;
        isOnBasecamp = false;

        maxAttackDistance = F64.Zero;
        attackTime = 0;
        attackAnimationTime = 0;
        attackWaitTime = 0;
        attackFowardTime = 0;
        bulletId = 0;
        bulletSpeed = F64.Zero;
        bulletDistance = F64.Zero;

        isBasecamp = false;
        isWall = false;
    }
}
