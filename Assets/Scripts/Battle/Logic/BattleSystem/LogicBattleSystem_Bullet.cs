using FixPointUnity;
using GameFramework;
using System.Collections.Generic;

public partial class LogicBattleSystem
{
    private GenericPool<LogicBulletBase> bulletPool = new GenericPool<LogicBulletBase>();

    /// <summary>
    /// 所有的子弹列表
    /// </summary>
    private List<LogicBulletBase> bulletList = new List<LogicBulletBase>();

    private void BulletCompute()
    {
        foreach (var item in bulletList)
        {
            item.CustomUpdate(COMPUTE_DELTA_MILLISECOND);
            if (item.isAlive == false && item.isUsed == false)
            {
                LogicBulletFlyToEnd(item);
            }
        }
    }

    public LogicBulletBase GetOneLogicBullet(LogicBattleUnit attacker, LogicBattleUnit victim)
    {
        LogicBulletBase bullet = bulletPool.GetInstance();
        bullet.shooterIndex = attacker.index;
        bullet.speed = attacker.staticData.BulletSpeed;
        bullet.damage = attacker.GetGeneralAttackPower(victim);
        bullet.startPosition = attacker.runtimeData.pos;
        bullet.canFollowTarget = attacker.staticData.bulletCanFollowTarget;
        if (attacker.staticData.BulletDistance > 0)
        {
            var dir = victim.runtimeData.pos - attacker.runtimeData.pos;
            dir = F64Vec3.Normalize(dir) * attacker.staticData.BulletDistance;
            bullet.targetPosition = attacker.runtimeData.pos + dir;
            bullet.isDistanceFixed = true;
        }
        else
        {
            bullet.targetPosition = victim.runtimeData.pos;
            bullet.isDistanceFixed = false;
        }
        bullet.targetIndex = victim.index;
        bullet.Initialize();
        bulletList.Add(bullet);
        return bullet;
    }

    /// <summary>
    /// 子弹飞行结束
    /// 如果是跟踪弹，会对目标产生伤害
    /// 如果是非跟踪弹，因为没有明确的目标，可以通过技能来产生伤害
    /// </summary>
    /// <param name="bullet"></param>
    public void LogicBulletFlyToEnd(LogicBulletBase bullet)
    {
        bullet.isUsed = true;
        LogicBattleUnit attacker = GetLogicBattleUnit(bullet.shooterIndex);
        if (bullet.canFollowTarget)
        {
            LogicBattleUnit victim = GetLogicBattleUnit(bullet.targetIndex);
            if (bullet.damage > 0)
            {
                if (victim.runtimeData.currentStatus != BattleUnitState.Dead)
                {
                    int evadeValue = clientRandom.Next(1, 100);
                    if (evadeValue > victim.staticData.evade)
                    {
                        bullet.isHit = true;
                        OnVictimBloodLoss(attacker, victim, bullet.damage, true);
                    }
                    else
                    {
                        if (currentFrameOutputData == null)
                        {
                            currentFrameOutputData = outputDataFramePool.GetInstance();
                        }
                        //闪避成功，无伤，但是要产生闪避的帧数据，供表现层做动画
                        currentFrameOutputData.evadeUnits.Add(victim);
                    }
                }
            }
            else
            {
                bullet.isHit = true;
            }
        }
        else
        {
            bullet.isHit = true;
        }
        if (bullet.isHit)
        {
            //技能处理
            TryTriggerSkill_AttackHit(attacker, bullet);
        }
        //此处不回收逻辑子弹，由真实子弹回收后再触发逻辑子弹回收，避免真实子弹还没刷新，逻辑子弹就没了的现象
    }

    /// <summary>
    /// 子弹对目标产生伤害，由于指定了目标，因此不用管子弹的类型，直接进行伤害计算即可
    /// </summary>
    /// <param name="bullet"></param>
    /// <param name="logicBattleUnit"></param>
    public void LogicBulletHitTarget(LogicBulletBase bullet, LogicBattleUnit victim)
    {
        LogicBattleUnit attacker = GetLogicBattleUnit(bullet.shooterIndex);
        if (bullet.damage > 0)
        {
            if (victim.runtimeData.currentStatus != BattleUnitState.Dead)
            {
                int evadeValue = clientRandom.Next(1, 100);
                if (evadeValue > victim.staticData.evade)
                {
                    bullet.isHit = true;
                    OnVictimBloodLoss(attacker, victim, bullet.damage, true);
                }
                else
                {
                    if (currentFrameOutputData == null)
                    {
                        currentFrameOutputData = outputDataFramePool.GetInstance();
                    }
                    //闪避成功，无伤，但是要产生闪避的帧数据，供表现层做动画
                    currentFrameOutputData.evadeUnits.Add(victim);
                }
            }
        }
        //技能处理
        TryTriggerSkill_AttackHit(attacker, bullet);
    }

    /// <summary>
    /// 战斗过程中发射子弹
    /// </summary>
    /// <param name="bullet"></param>
    private void AddBullet(LogicBulletBase bullet)
    {
        if (currentFrameOutputData == null)
        {
            currentFrameOutputData = outputDataFramePool.GetInstance();
        }
        currentFrameOutputData.addBullets.Add(bullet);
    }

    /// <summary>
    /// 真正执行回收子弹操作
    /// </summary>
    /// <param name="bulletBase"></param>
    public void RecycleLogicBullet(LogicBulletBase bullet)
    {
        bullet.Clear();
        bulletPool.RecycleInstance(bullet);
        bulletList.Remove(bullet);
    }

    public void ClearBullet()
    {
        foreach (var item in bulletList)
        {
            item.Clear();
            bulletPool.RecycleInstance(item);
        }
        bulletList.Clear();
    }
}
