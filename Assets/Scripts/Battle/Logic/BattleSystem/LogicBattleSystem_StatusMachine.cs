using FixPointUnity;
using GameFramework;
using System;
using System.Collections.Generic;

public partial class LogicBattleSystem
{
    public delegate void DelegateOnDamage(LogicBattleUnit attacker, LogicBattleUnit victim, int damage, bool ordinaryAttack);

    public static event DelegateOnDamage Event_OnDamage;
    public static event Action<LogicBattleUnit> Event_OnCombatUnitDead;

    private Dictionary<BattleUnitState, ILogicStatus> StatusMachineDic;
    /// <summary>
    /// 一次攻击相关引用记录
    /// </summary>
    private Dictionary<LogicBattleUnit, LogicOnceAttackRelate> onceAttackRelateDic = new Dictionary<LogicBattleUnit, LogicOnceAttackRelate>();
    private GenericPool<LogicOnceAttackRelate> onceAttackPool = new GenericPool<LogicOnceAttackRelate>();
    /// <summary>
    /// 攻击关联数据列表的临时容器
    /// </summary>
    private List<LogicOnceAttackRelate> tempAttackRelateList = new List<LogicOnceAttackRelate>();

    /// <summary>
    /// 筛选之后得到的目标列表
    /// </summary>
    private List<LogicBattleUnit> targetList = new List<LogicBattleUnit>(9);

    #region 状态机函数入口

    public void SwitchStatus(LogicBattleUnit logicBattleUnit, BattleUnitState newStatus, bool delaySwitch = false)
    {
        if (StatusMachineDic.ContainsKey(newStatus))
        {
            //上个状态退出
            BattleUnitState oldStatus = logicBattleUnit.runtimeData.currentStatus;
            ILogicStatus currentStatus = StatusMachineDic[oldStatus];
            currentStatus.StatusQuit(logicBattleUnit);
            //下个状态进入
            logicBattleUnit.runtimeData.currentStatus = newStatus;
            currentStatus = StatusMachineDic[newStatus];
            currentStatus.StatusEnter(logicBattleUnit);
            if (currentFrameOutputData == null)
            {
                currentFrameOutputData = outputDataFramePool.GetInstance();
            }
            currentFrameOutputData.statusChangedUnits.Add((logicBattleUnit, oldStatus, newStatus));

            if (newStatus != BattleUnitState.MoveToAttack && newStatus != BattleUnitState.MoveToBasecamp)
            {
                //离开移动状态累计时间得重置为0
                logicBattleUnit.runtimeData.movedTotalTime = F64.Zero;
            }
            //if (logicBattleUnit.id == 530101)
            //    GameFramework.Debug.Log($"单位:{logicBattleUnit.id} index:{logicBattleUnit.index}切换状态！旧状态：{oldStatus}，新状态：{newStatus}");
        }
        else
        {
            GameFramework.Debug.LogError("要切换的状态未注册，请先注册该状态！状态名：" + newStatus.ToString());
        }
    }

    public bool IdleStatusUpdate(LogicBattleUnit logicBattleUnit, bool callFromIdleStatus)
    {
        if (logicBattleUnit.runtimeData.freezed)
        {
            return false;
        }
        return IdleStatusUpdate(logicBattleUnit);
    }

    /// <summary>
    /// 进攻方单位处于Idle状态时Update执行的逻辑
    /// 如果寻找目标成功，就向目标移动
    /// 没有找到目标时，从当前位置直线向关底前进（地面单位同样需要寻路），这里的直线前进指沿X轴（注意是地图方向的X轴）正方向前进
    /// </summary>
    /// <param name="logicBattleUnit"></param>
    private bool IdleStatusUpdate(LogicBattleUnit unit)
    {
        if (unit == attackerBasecamp || unit == defenderBasecamp)
        {
            return false;
        }
        if (unit.runtimeData.freezed)
        {
            return false;
        }
        if (IsAttackerBasecampDestroyed() || IsDefenderBasecampDestroyed())
        {
            return false;
        }
        var target = GetTarget(unit);
        if (target != null)
        {
            MoveToAttackTarget(unit, target);
        }
        else
        {
            GotoBasecamp(unit);
        }
        return true;
    }

    /// <summary>
    /// 通过寻路移动去攻击目标
    /// </summary>
    /// <param name="unit"></param>
    public void MoveToEndStatusUpdate(LogicBattleUnit unit)
    {
        if (unit.runtimeData.freezed)
        {
            return;
        }
        if (unit.IsArriveEnemyBasecamp())
        {
            //移动到敌方大本营后，攻击目标变为敌方大本营
            MoveToAttackTarget(unit, defenderBasecamp, false);
        }
        else
        {
            unit.Move(F64.One);
            TryTriggerSkill(unit, SkillTriggerTiming.Moving);
            //向敌方大本营移动过程中，有目标就去攻击
            var target = GetTarget(unit);
            if (target != null)
            {
                MoveToAttackTarget(unit, target, false);
            }
        }
    }

    private void MoveToAttackTarget(LogicBattleUnit attacker, LogicBattleUnit target, bool delaySwitch = true)
    {
        if (attacker.staticData.isFly)
        {
            RecordMainAttackRelate(attacker, target);
        }
        else
        {
            RecordMainAttackRelate(attacker, target);
        }

        TryTriggerSkill(attacker, SkillTriggerTiming.FindTarget);

        if (attacker.runtimeData.currentStatus != BattleUnitState.MoveToAttack && attacker.runtimeData.currentStatus != BattleUnitState.PerformSkill)
        {
            SwitchStatus(attacker, BattleUnitState.MoveToAttack, delaySwitch);
        }
    }

    public void MoveToAttackStatusEnter(LogicBattleUnit logicBattleUnit)
    {
        LogicOnceAttackRelate attackRelate = GetOnceAttackRelateByAttacker(logicBattleUnit);
        attackRelate?.secondaryTarget.Clear();
    }

    public void MoveToAttackStatusUpdate(LogicBattleUnit logicBattleUnit)
    {
        if (logicBattleUnit.runtimeData.freezed)
        {
            return;
        }
        LogicOnceAttackRelate attackRelate = GetOnceAttackRelateByAttacker(logicBattleUnit);
        if (attackRelate == null || attackRelate.mainTarget.runtimeData.currentStatus == BattleUnitState.Dead)
        {
            bool switchToOtherStatus = IdleStatusUpdate(logicBattleUnit, false);
            if (switchToOtherStatus == false)
            {
                SwitchStatus(logicBattleUnit, BattleUnitState.Idle);
            }
        }
        else
        {
            F64 maxDistance = logicBattleUnit.staticData.MaxAttackDistance;
            F64 minDistance = logicBattleUnit.staticData.minAttackDistance;
            int angle = logicBattleUnit.staticData.attackAngle;
            F64 volumeRadius = attackRelate.mainTarget.staticData.volumeRadius;
            F64Vec3 attackPos = logicBattleUnit.runtimeData.pos;
            F64Vec3 targetPos = attackRelate.mainTarget.runtimeData.pos;
            //进攻方
            if (logicBattleUnit.IsAttacker)
            {
                if (IsInRange(maxDistance, minDistance, attackPos, volumeRadius, targetPos, angle))
                {
                    SwitchStatus(logicBattleUnit, BattleUnitState.Attacking);
                    if (logicBattleUnit.runtimeData.isKightCharging)
                    {
                        TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.WillAttacking);
                    }
                    TryEndSkill(logicBattleUnit, SkillEndTiming.WillAttack);
                }
                else
                {
                    logicBattleUnit.Move(F64.One);
                    TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.Moving);
                    TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.DistanceBelow);

                    //移动过程中，永远选择最近的目标
                    LogicBattleUnit newTarget = null;
                    if (logicBattleUnit.IsAttacker)
                    {
                        newTarget = GetTarget(logicBattleUnit);
                    }
                    else
                    {
                        newTarget = GetTarget(logicBattleUnit);
                    }
                    if (newTarget != null && newTarget != attackRelate.mainTarget)
                    {
                        MoveToAttackTarget(logicBattleUnit, newTarget, false);
                    }
                }
            }
            //防守方
            else
            {
                if (IsInRange(maxDistance, minDistance, attackPos, volumeRadius, targetPos, angle))
                {
                    SwitchStatus(logicBattleUnit, BattleUnitState.Attacking);
                    if (logicBattleUnit.runtimeData.isKightCharging)
                    {
                        TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.WillAttacking);
                    }
                    TryEndSkill(logicBattleUnit, SkillEndTiming.WillAttack);
                }
                else
                {
                    logicBattleUnit.Move(F64.One);
                    TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.Moving);
                    TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.DistanceBelow);

                    //移动过程中，永远选择最近的目标
                    var newTarget = GetTarget(logicBattleUnit);
                    if (newTarget != null && newTarget != attackRelate.mainTarget)
                    {
                        MoveToAttackTarget(logicBattleUnit, newTarget);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 向大本营移动
    /// </summary>
    /// <param name="unit"></param>
    private void GotoBasecamp(LogicBattleUnit unit)
    {
        var basecampPos = unit.IsAttacker ? defenderBasecamp.runtimeData.pos : attackerBasecamp.runtimeData.pos;
        //向大本营移动时要直线前进，因此只修改X轴
        unit.runtimeData.moveToPos = unit.runtimeData.pos;
        unit.runtimeData.moveToPos.X = basecampPos.X;  

        SwitchStatus(unit, BattleUnitState.MoveToBasecamp);
    }

    public void AttackingStatusEnter(LogicBattleUnit logicBattleUnit)
    {
        logicBattleUnit.runtimeData.attackTimer = 0;
        logicBattleUnit.runtimeData.attackedNumber = 0;
        logicBattleUnit.runtimeData.attackIntervalTimer = logicBattleUnit.staticData.attackInterval;
    }

    public void AttackingStatusUpdate(LogicBattleUnit logicBattleUnit)
    {
        if (logicBattleUnit.runtimeData.freezed)
        {
            return;
        }
        logicBattleUnit.runtimeData.attackTimer += COMPUTE_DELTA_MILLISECOND;
        //攻击状态结束
        if (logicBattleUnit.runtimeData.attackTimer >= logicBattleUnit.staticData.AttackTime)
        {
            SwitchStatus(logicBattleUnit, BattleUnitState.AttackWait);
        }
        else
        {
            //攻击前摇阶段过去之后开始攻击
            if (logicBattleUnit.runtimeData.attackTimer >= logicBattleUnit.staticData.AttackFowardTime)
            {
                logicBattleUnit.runtimeData.attackIntervalTimer += COMPUTE_DELTA_MILLISECOND;
                bool attackEnable = logicBattleUnit.runtimeData.attackIntervalTimer >= logicBattleUnit.staticData.attackInterval && logicBattleUnit.runtimeData.attackedNumber < logicBattleUnit.staticData.attackNumber;

                if (attackEnable)
                {
                    LogicOnceAttackRelate attackRelate = GetOnceAttackRelateByAttacker(logicBattleUnit);
                    if (attackRelate == null || attackRelate.mainTarget.runtimeData.currentStatus == BattleUnitState.Dead)
                    {
                        //如果做出了攻击动作后，目标已经死亡，就尝试切换目标，如果没有目标，本次攻击就击空
                        var list = GetEnemyUnitWithinRange_Sort(logicBattleUnit.runtimeData.pos, logicBattleUnit.staticData.MaxAttackDistance, logicBattleUnit.staticData.minAttackDistance, logicBattleUnit.staticData.targetType, logicBattleUnit.IsAttacker);
                        if (list.Count <= 0)
                        {
                            return;
                        }
                        attackRelate = RecordMainAttackRelate(logicBattleUnit, list[0]);
                    }

                    //重新计时
                    logicBattleUnit.runtimeData.attackIntervalTimer = 0;
                    logicBattleUnit.runtimeData.attackedNumber++;
                    logicBattleUnit.runtimeData.attackPower = F64.FloorToInt(logicBattleUnit.GetGeneralAttackPower(attackRelate.mainTarget) * logicBattleUnit.runtimeData.attackPowerCoeff);
                    //远程子弹
                    if (logicBattleUnit.staticData.BulletId != 0)
                    {
                        LongRangeAttack(logicBattleUnit, attackRelate);
                    }
                    //近战普攻
                    else
                    {
                        ShortRangeAttack(logicBattleUnit, attackRelate);
                    }
                    TryEndSkill(logicBattleUnit, SkillEndTiming.AfterAttack);
                    TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.Attacking);
                }
            }
        }
    }

    private void LongRangeAttack(LogicBattleUnit logicBattleUnit, LogicOnceAttackRelate attackRelate)
    {
        //远程可以攻击多个目标，此处需修正次要目标
        CorrectTheTarget(logicBattleUnit);
        var bullet = GetOneLogicBullet(logicBattleUnit, attackRelate.mainTarget);
        AddBullet(bullet);

        foreach (var item in attackRelate.secondaryTarget)
        {
            logicBattleUnit.runtimeData.attackPower = F64.FloorToInt(logicBattleUnit.GetGeneralAttackPower(item) * logicBattleUnit.runtimeData.attackPowerCoeff);
            bullet = GetOneLogicBullet(logicBattleUnit, item);
            AddBullet(bullet);
        }
    }

    private void ShortRangeAttack(LogicBattleUnit logicBattleUnit, LogicOnceAttackRelate attackRelate)
    {
        //有必要计算普攻
        if (logicBattleUnit.runtimeData.attackPower > 0)
        {
            F64 maxDistance = logicBattleUnit.staticData.MaxAttackDistance;
            F64 minDistance = logicBattleUnit.staticData.minAttackDistance;
            int angle = logicBattleUnit.staticData.attackAngle;
            F64Vec3 attckPos = logicBattleUnit.runtimeData.pos;
            F64 volumeRadius = attackRelate.mainTarget.staticData.volumeRadius;
            F64Vec3 targetPos = attackRelate.mainTarget.runtimeData.pos;
            if (IsInRange(maxDistance, minDistance, attckPos, volumeRadius, targetPos, angle))
            {
                int evadeValue = clientRandom.Next(1, 100);
                if (evadeValue > attackRelate.mainTarget.staticData.evade)
                {
                    OnVictimBloodLoss(logicBattleUnit, attackRelate.mainTarget, logicBattleUnit.runtimeData.attackPower, true);
                }
                else
                {
                    if (currentFrameOutputData == null)
                    {
                        currentFrameOutputData = outputDataFramePool.GetInstance();
                    }
                    //闪避成功，无伤，但是要产生闪避的帧数据，供表现层做动画
                    currentFrameOutputData.evadeUnits.Add(attackRelate.mainTarget);
                }
            }
        }
    }

    public void HighFrequencyAttackStatusEnter(LogicBattleUnit logicBattleUnit)
    {
        logicBattleUnit.runtimeData.attackTimer = 0;
        logicBattleUnit.runtimeData.attackedNumber = 0;
    }

    public void HighFrequencyAttackStatusUpdate(LogicBattleUnit logicBattleUnit)
    {
    }

    public void HighFrequencyAttackStatusQuit(LogicBattleUnit logicBattleUnit)
    {
        logicBattleUnit.runtimeData.attackTimer = 0;
        logicBattleUnit.runtimeData.attackedNumber = 0;
    }

    public void AttackWaitStatusEnter(LogicBattleUnit logicBattleUnit)
    {
        logicBattleUnit.runtimeData.attackWaitTimer = 0;
    }

    public void AttackWaitStatusUpdate(LogicBattleUnit logicBattleUnit)
    {
        if (logicBattleUnit.runtimeData.freezed)
        {
            return;
        }
        logicBattleUnit.runtimeData.attackWaitTimer += COMPUTE_DELTA_MILLISECOND;

        if (logicBattleUnit.runtimeData.attackWaitTimer >= logicBattleUnit.staticData.AttackWaitTime)
        {
            LogicOnceAttackRelate attackRelate = GetOnceAttackRelateByAttacker(logicBattleUnit);
            if (attackRelate == null)
            {
                bool switchToOtherStatus = IdleStatusUpdate(logicBattleUnit, false);
                if (switchToOtherStatus == false)
                {
                    SwitchStatus(logicBattleUnit, BattleUnitState.Idle, true);
                }
            }
            else
            {
                LogicBattleUnit victim = attackRelate.mainTarget;
                if (victim.runtimeData.currentStatus == BattleUnitState.Dead)
                {
                    bool switchToOtherStatus = IdleStatusUpdate(logicBattleUnit, false);
                    if (switchToOtherStatus == false)
                    {
                        SwitchStatus(logicBattleUnit, BattleUnitState.Idle, true);
                    }
                }
                else
                {
                    //重新选择距离最近的目标
                    LogicBattleUnit newTarget = null;
                    if (logicBattleUnit.IsAttacker)
                    {
                        newTarget = GetTarget(logicBattleUnit);
                    }
                    else
                    {
                        newTarget = GetTarget(logicBattleUnit);
                    }
                    if (newTarget != null && newTarget != victim)
                    {
                        victim = newTarget;
                        RecordMainAttackRelate(logicBattleUnit, victim);
                    }
                    F64 maxDistance = logicBattleUnit.staticData.MaxAttackDistance;
                    F64 minDistance = logicBattleUnit.staticData.minAttackDistance;
                    F64Vec3 attackPos = logicBattleUnit.runtimeData.pos;
                    int angle = logicBattleUnit.staticData.attackAngle;
                    F64 volumeRadius = victim.staticData.volumeRadius;
                    F64Vec3 targetPos = victim.runtimeData.pos;
                    //进攻方
                    if (logicBattleUnit.IsAttacker)
                    {
                        if (IsInRange(maxDistance, minDistance, attackPos, volumeRadius, targetPos, angle))
                        {
                            SwitchStatus(logicBattleUnit, BattleUnitState.Attacking);
                        }
                        else
                        {
                            MoveToAttackTarget(logicBattleUnit, victim, false);
                        }
                    }
                    //防守方
                    else
                    {
                        if (IsInRange(maxDistance, minDistance, attackPos, volumeRadius, targetPos, angle))
                        {
                            SwitchStatus(logicBattleUnit, BattleUnitState.Attacking);
                        }
                        else
                        {
                            MoveToAttackTarget(logicBattleUnit, victim, false);
                        }
                    }
                }
            }
        }
    }

    public void WarningStatusEnter(LogicBattleUnit logicBattleUnit)
    {
        logicBattleUnit.runtimeData.warningTimer = 0;
    }

    public void WarningStatusUpdate(LogicBattleUnit logicBattleUnit)
    {
        logicBattleUnit.runtimeData.warningTimer += COMPUTE_DELTA_MILLISECOND;
        if (logicBattleUnit.runtimeData.warningTimer >= logicBattleUnit.staticData.triggerTime)
        {
            SwitchStatus(logicBattleUnit, BattleUnitState.Attacking);
        }
    }

    internal void PerformSkillStatusEnter(LogicBattleUnit logicBattleUnit)
    {
        logicBattleUnit.runtimeData.performSkillTimer = 0;
    }

    internal void PerformSkillStatusUpdate(LogicBattleUnit logicBattleUnit)
    {
        if (logicBattleUnit.runtimeData.freezed)
        {
            return;
        }
        logicBattleUnit.runtimeData.performSkillTimer += COMPUTE_DELTA_MILLISECOND;
        if (logicBattleUnit.runtimeData.performSkillTimer >= logicBattleUnit.runtimeData.performSkillTime)
        {
            SwitchStatus(logicBattleUnit, BattleUnitState.Idle, false);
        }
    }

    #endregion

    #region 目标选择

    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="unit"></param>
    private LogicBattleUnit GetTarget(LogicBattleUnit unit)
    {
        return null;
    }

    #endregion

    /// <summary>
    /// 根据攻击者获取攻击关联信息
    /// </summary>
    /// <param name="attacker"></param>
    /// <returns></returns>
    public LogicOnceAttackRelate GetOnceAttackRelateByAttacker(LogicBattleUnit attacker)
    {
        onceAttackRelateDic.TryGetValue(attacker, out LogicOnceAttackRelate result);
        return result;
    }

    /// <summary>
    /// 根据被攻击者victim获取所有以victim为主要攻击目标的攻击关联信息
    /// </summary>
    /// <param name="victim"></param>
    /// <returns></returns>
    public List<LogicOnceAttackRelate> GetAllAttackRelateByVictim(LogicBattleUnit victim)
    {
        tempAttackRelateList.Clear();
        foreach (var item in onceAttackRelateDic)
        {
            if (item.Value.mainTarget == victim)
            {
                tempAttackRelateList.Add(item.Value);
            }
        }
        return tempAttackRelateList;
    }

    /// <summary>
    /// 记录主要目标的攻击关联信息
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public LogicOnceAttackRelate RecordMainAttackRelate(LogicBattleUnit attacker, LogicBattleUnit target)
    {
        if (onceAttackRelateDic.ContainsKey(attacker) == false)
        {
            LogicOnceAttackRelate result = onceAttackPool.GetInstance();
            result.attacker = attacker;
            result.mainTarget = target;
            onceAttackRelateDic.Add(attacker, result);
            return result;
        }
        else
        {
            LogicOnceAttackRelate result = onceAttackRelateDic[attacker];
            result.mainTarget = target;
            return result;
        }
    }

    /// <summary>
    /// 获取目标范围内的所有符合攻击方目标类型的敌对单位，结果会按距离从近到远排序
    /// </summary>
    /// <param name="position">指定位置</param>
    /// <param name="maxRange">最大范围</param>
    /// <param name="minRange">最小范围</param>
    /// <param name="logicBattleUnit">攻击方</param>
    /// <returns></returns>
    public List<LogicBattleUnit> GetEnemyUnitWithinRange_Sort(F64Vec3 position, F64 maxRange, F64 minRange, int targetType, bool isAttacker, int attackAngle = 0)
    {
        GetEnemyUnitWithinRange(position, maxRange, minRange, targetType, isAttacker, attackAngle);
        targetList.Sort((a, b) => CompareDistance(a, b, position));
        return targetList;
    }

    /// <summary>
    /// 获取全地图范围内符合攻击方目标类型的敌对单位
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="isAttacker"></param>
    /// <returns></returns>
    public List<LogicBattleUnit> GetEnemyUnitInMap(int targetType, bool isAttacker)
    {
        targetList.Clear();
        if (isAttacker)
        {
            foreach (var item in aliveDefenderSet)
            {
                if (item.runtimeData.currentStatus == BattleUnitState.Dead)
                {
                    continue;
                }
                //目标类型要符合
                bool isValidTarget = (targetType & item.staticData.unitType) == item.staticData.unitType;
                //站在要塞里的不可作为目标
                isValidTarget &= !item.staticData.isOnBasecamp;

                if (isValidTarget)
                {
                    targetList.Add(item);
                }
            }
        }
        else
        {
            foreach (var item in aliveAttackerSet)
            {
                if (item.runtimeData.currentStatus == BattleUnitState.Dead)
                {
                    continue;
                }
                bool isValidTarget = (targetType & item.staticData.unitType) == item.staticData.unitType;

                if (isValidTarget)
                {
                    targetList.Add(item);
                }
            }
        }
        return targetList;
    }

    /// <summary>
    /// 获取目标范围内的所有符合攻击方目标类型的敌对单位
    /// </summary>
    /// <param name="position">指定位置</param>
    /// <param name="maxRange">最大范围</param>
    /// <param name="minRange">最小范围</param>
    /// <param name="logicBattleUnit">攻击方</param>
    /// <returns></returns>
    public List<LogicBattleUnit> GetEnemyUnitWithinRange(F64Vec3 position, F64 maxRange, F64 minRange, int targetType, bool isAttacker, int angle = 0)
    {
        targetList.Clear();
        if (isAttacker)
        {
            foreach (var item in aliveDefenderSet)
            {
                if (item.runtimeData.currentStatus == BattleUnitState.Dead)
                {
                    continue;
                }
                //目标类型要符合
                bool isValidTarget = (targetType & item.staticData.unitType) == item.staticData.unitType;
                //站在要塞里的不可作为目标
                isValidTarget &= !item.staticData.isOnBasecamp;

                if (isValidTarget)
                {
                    if (IsInRange(maxRange, minRange, position, item.staticData.volumeRadius, item.runtimeData.pos, angle))
                    {
                        targetList.Add(item);
                    }
                }
            }
        }
        else
        {
            foreach (var item in aliveAttackerSet)
            {
                if (item.runtimeData.currentStatus == BattleUnitState.Dead)
                {
                    continue;
                }
                bool isValidTarget = (targetType & item.staticData.unitType) == item.staticData.unitType;

                if (isValidTarget)
                {
                    if (IsInRange(maxRange, minRange, position, item.staticData.volumeRadius, item.runtimeData.pos, angle))
                    {
                        targetList.Add(item);
                    }
                }
            }
        }
        return targetList;
    }

    /// <summary>
    /// 获取目标范围内的所有友方单位
    /// </summary>
    /// <param name="position"></param>
    /// <param name="range"></param>
    /// <param name="logicBattleUnit"></param>
    /// <returns></returns>
    public List<LogicBattleUnit> GetAllyUnitWithinRange(F64Vec3 position, F64 range, int targetType, bool isAttacker)
    {
        targetList.Clear();
        if (isAttacker)
        {
            foreach (var item in aliveAttackerSet)
            {
                bool isValidTarget = (targetType & item.staticData.unitType) == item.staticData.unitType;
                if (isValidTarget)
                {
                    if (IsInRange(range, F64.Zero, position, item.staticData.volumeRadius, item.runtimeData.pos, item.staticData.attackAngle))
                    {
                        targetList.Add(item);
                    }
                }
            }
        }
        else
        {
            foreach (var item in aliveDefenderSet)
            {
                bool isValidTarget = (targetType & item.staticData.unitType) == item.staticData.unitType;
                if (isValidTarget)
                {
                    if (IsInRange(range, F64.Zero, position, item.staticData.volumeRadius, item.runtimeData.pos, item.staticData.attackAngle))
                    {
                        targetList.Add(item);
                    }
                }
            }
        }
        return targetList;
    }

    /// <summary>
    /// 移除目标为指定单位，效果为指定类型的所有技能，增益和减益全包括
    /// </summary>
    /// <param name="target">目标单位</param>
    /// <param name="skillEffectType">技能效果类型</param>
    public void RemoveAllSkillOfEffect(LogicBattleUnit target, SkillEffect skillEffectType)
    {
        for (int i = 0; i < logicSkillList.Count; i++)
        {
            var skill = logicSkillList[i];
            if (skill.tableData.effect == (int)skillEffectType &&
                skill.targetList.Contains(target))
            {
                skill.EndSkill();
                logicSkillList.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// 移除目标为指定单位，效果为指定类型的所有增益技能
    /// </summary>
    /// <param name="target"></param>
    /// <param name="skillEffectType"></param>
    public void RemoveAllPositiveSkillOfEffect(LogicBattleUnit target, SkillEffect skillEffectType)
    {
        for (int i = 0; i < logicSkillList.Count; i++)
        {
            var skill = logicSkillList[i];
            if (skill.tableData.effect == (int)skillEffectType &&
                skill.targetList.Contains(target) &&
                skill.IsPositive)
            {
                skill.EndSkill();
                logicSkillList.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// 移除目标为指定单位，效果为指定类型的所有减益技能
    /// </summary>
    /// <param name="target"></param>
    /// <param name="skillEffectType"></param>
    public void RemoveAllNegativeSkillOfEffect(LogicBattleUnit target, SkillEffect skillEffectType)
    {
        for (int i = 0; i < logicSkillList.Count; i++)
        {
            var skill = logicSkillList[i];
            if (skill.tableData.effect == (int)skillEffectType &&
                skill.targetList.Contains(target) &&
                skill.IsPositive == false)
            {
                skill.EndSkill();
                logicSkillList.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// 移除目标为指定单位的所有某类技能，增益和减益全包括
    /// </summary>
    /// <param name="target">目标单位</param>
    /// <param name="skillType">技能效果类型</param>
    public void RemoveAllSkill(LogicBattleUnit target, Type skillType)
    {
        for (int i = 0; i < logicSkillList.Count; i++)
        {
            var skill = logicSkillList[i];
            if (skill.GetType() == skillType &&
                skill.targetList.Contains(target))
            {
                skill.EndSkill();
                logicSkillList.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// 移除目标为指定单位的所有某类增益技能
    /// </summary>
    /// <param name="target"></param>
    /// <param name="skillEffectType"></param>
    public void RemoveAllPositiveSkill(LogicBattleUnit target, Type skillType)
    {
        for (int i = 0; i < logicSkillList.Count; i++)
        {
            var skill = logicSkillList[i];
            if (skill.GetType() == skillType &&
                skill.targetList.Contains(target) &&
                skill.IsPositive)
            {
                skill.EndSkill();
                logicSkillList.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// 移除目标为指定单位的所有某类减益技能
    /// </summary>
    /// <param name="target"></param>
    /// <param name="skillEffectType"></param>
    public void RemoveAllNegativeSkill(LogicBattleUnit target, Type skillType)
    {
        for (int i = 0; i < logicSkillList.Count; i++)
        {
            var skill = logicSkillList[i];
            if (skill.GetType() == skillType &&
                skill.targetList.Contains(target) &&
                skill.IsPositive == false)
            {
                skill.EndSkill();
                logicSkillList.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// 比较距离方法
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="targetPosition"></param>
    /// <returns></returns>
    public int CompareDistance(LogicBattleUnit a, LogicBattleUnit b, F64Vec3 targetPosition)
    {
        F64Vec3 targetA = a.runtimeData.pos - targetPosition;
        F64Vec3 targetB = b.runtimeData.pos - targetPosition;
        F64 distanceA = targetA.X * targetA.X + targetA.Y * targetA.Y;
        F64 distanceB = targetB.X * targetB.X + targetB.Y * targetB.Y;
        if (distanceA > distanceB)
        {
            return 1;
        }
        else if (distanceA < distanceB)
        {
            return -1;
        }
        else
        {
            return a.index > b.index ? 1 : -1;
        }
    }

    /// <summary>
    /// 修正次要攻击目标，因为当单位真正发动攻击伤害时，之前选定的次要目标可能已经死亡了，此时就需要重新选定目标
    /// </summary>
    private void CorrectTheTarget(LogicBattleUnit attacker)
    {
        F64 maxDistance = attacker.staticData.MaxAttackDistance;
        F64 minDistance = attacker.staticData.minAttackDistance;
        int angle = attacker.staticData.attackAngle;
        F64Vec3 attackPos = attacker.runtimeData.pos;
        //不符合条件的次要目标移除
        LogicOnceAttackRelate attackRelate = GetOnceAttackRelateByAttacker(attacker);
        for (int i = 0; i < attackRelate.secondaryTarget.Count; i++)
        {
            F64 volume = attackRelate.secondaryTarget[i].staticData.volumeRadius;
            F64Vec3 victimPos = attackRelate.secondaryTarget[i].runtimeData.pos;
            if (attackRelate.secondaryTarget[i].runtimeData.currentStatus == BattleUnitState.Dead ||
                attackRelate.secondaryTarget[i] == attackRelate.mainTarget ||
                IsInRange(maxDistance, minDistance, attackPos, volume, victimPos, angle) == false)
            {
                attackRelate.secondaryTarget.RemoveAt(i);
                i--;
            }
        }
        //补充次要攻击目标
        if (attackRelate.secondaryTarget.Count < attacker.staticData.targetCount - 1)
        {
            List<LogicBattleUnit> targetList = GetEnemyUnitWithinRange_Sort(attackPos, maxDistance, minDistance, attacker.staticData.targetType, attacker.IsAttacker);
            foreach (var item in targetList)
            {
                if (attackRelate.secondaryTarget.Count < attacker.staticData.targetCount - 1)
                {
                    if (attackRelate.mainTarget != item &&
                        attackRelate.secondaryTarget.Contains(item) == false)
                    {
                        attackRelate.secondaryTarget.Add(item);
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 次要目标排序方法
    /// </summary>
    /// <param name="attack"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private int CompareTheSecondTarget(LogicBattleUnit attack, LogicBattleUnit a, LogicBattleUnit b)
    {
        bool a_Priority = IsPriorityTarget(attack.staticData.attackPriority, (int)a.staticData.battleUnitType);
        bool b_Priority = IsPriorityTarget(attack.staticData.attackPriority, (int)b.staticData.battleUnitType);
        int result;
        if (a_Priority == true && b_Priority == true)
        {
            result = CompareDistance(a, b, attack.runtimeData.pos);
        }
        else if (a_Priority == true && b_Priority == false)
        {
            result = -1;
        }
        else if (a_Priority == false && b_Priority == true)
        {
            result = 1;
        }
        else
        {
            if (a.staticData.battleUnitType < b.staticData.battleUnitType)
            {
                result = -1;
            }
            else if (a.staticData.battleUnitType == b.staticData.battleUnitType)
            {
                result = CompareDistance(a, b, attack.runtimeData.pos);
            }
            else
            {
                result = 1;
            }
        }
        return result;
    }

    /// <summary>
    /// 受害者掉血
    /// </summary>
    public void OnVictimBloodLoss(LogicBattleUnit attacker, LogicBattleUnit victim, int damage, bool ordinaryAttack = false)
    {
        if (victim.runtimeData.currentStatus != BattleUnitState.Dead)
        {
            //闪避失败，被命中
            Event_OnDamage?.Invoke(attacker, victim, damage, ordinaryAttack);
            victim.runtimeData.hp -= damage;
            victim.runtimeData.hp = victim.runtimeData.hp <= 0 ? 0 : victim.runtimeData.hp;
            bool isDead = victim.runtimeData.hp <= 0;
            if (isDead)
            {
                OnVictimDead(victim);
            }
            else
            {
                TryTriggerSkill(victim, SkillTriggerTiming.BeAttacked);
            }
        }
    }

    /// <summary>
    /// 受害者死亡
    /// </summary>
    /// <param name="victim"></param>
    public void OnVictimDead(LogicBattleUnit logicBattleUnit)
    {
        if (logicBattleUnit.staticData.isBasecamp)
        {
            OutputData.isAttackerWin = logicBattleUnit.IsDefender;
        }
        Event_OnCombatUnitDead?.Invoke(logicBattleUnit);
        SwitchStatus(logicBattleUnit, BattleUnitState.Dead);
        TryTriggerSkill(logicBattleUnit, SkillTriggerTiming.Dead);
        RemoveMainAttackRelate(logicBattleUnit);

        //如果死亡单位是要塞，那么上面站的两个弓箭手也一起死亡
        if (logicBattleUnit.staticData.isBasecamp)
        {
            foreach (var unit in aliveDefenderSet)
            {
                if (unit.staticData.isOnBasecamp)
                {
                    SwitchStatus(unit, BattleUnitState.Dead);
                }
            }
        }
    }

    /// <summary>
    /// 移除死亡单位
    /// </summary>
    /// <param name="logicBattleUnit"></param>
    public void RemoveDeadUnit(LogicBattleUnit logicBattleUnit)
    {
        //死亡时从阵容列表中移除
        if (defenderDic.ContainsKey(logicBattleUnit.index))
        {
            aliveDefenderSet.Remove(logicBattleUnit);
        }
        else if (attackerDic.ContainsKey(logicBattleUnit.index))
        {
            aliveAttackerSet.Remove(logicBattleUnit);
        }
        else
        {
            GameFramework.Debug.LogError("未在现有容器中找到要移除的死亡单位！");
        }
    }

    /// <summary>
    /// 单位死亡时，移除攻击关系的一些数据
    /// </summary>
    /// <param name="victim"></param>
    public void RemoveMainAttackRelate(LogicBattleUnit victim)
    {
        //单位死亡时，找出所以它为攻击目标的攻击关系数据，将这些数据移除
        var list = GetAllAttackRelateByVictim(victim);
        foreach (var attackRelate in list)
        {
            onceAttackRelateDic.Remove(attackRelate.attacker);
            attackRelate.Clear();
            onceAttackPool.RecycleInstance(attackRelate);
        }

        //单位死亡时，将它作为攻击方的一些攻击关系数据进行修改，它所攻击的目标
        onceAttackRelateDic.TryGetValue(victim, out LogicOnceAttackRelate result);
        if (result != null)
        {
            onceAttackRelateDic.Remove(victim);
            result.Clear();
            onceAttackPool.RecycleInstance(result);
        }
    }

    /// <summary>
    /// 判断给定的目标类型是不是优先攻击类型
    /// </summary>
    /// <param name="attackerPriority"></param>
    /// <param name="victimType"></param>
    /// <returns></returns>
    private bool IsPriorityTarget(int attackerPriority, int victimType)
    {
        return (attackerPriority & victimType) == victimType;
    }

    /// <summary>
    /// 清除所有的攻击关联信息
    /// </summary>
    private void ClearOnceAttackRelate()
    {
        foreach (var item in onceAttackRelateDic)
        {
            item.Value.Clear();
            onceAttackPool.RecycleInstance(item.Value);
        }
        onceAttackRelateDic.Clear();
    }
}
