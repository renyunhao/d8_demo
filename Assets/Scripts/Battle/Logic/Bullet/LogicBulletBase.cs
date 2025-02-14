using FixPointUnity;
using System.Collections.Generic;
using UnityEngine;

public class LogicBulletBase
{
    public static int BulletUniqueId = 0;

    /// <summary>
    /// 子弹唯一编号，用于调试
    /// </summary>
    public int uniqueId;
    /// <summary>
    /// 发射子弹的单位索引
    /// </summary>
    public int shooterIndex;
    /// <summary>
    /// 子弹目标索引
    /// </summary>
    public int targetIndex;
    /// <summary>
    /// 子弹移动速度
    /// </summary>
    public F64 speed;
    /// <summary>
    /// 子弹伤害
    /// </summary>
    public int damage;
    /// <summary>
    /// 子弹起始位置
    /// </summary>
    public F64Vec3 startPosition;
    /// <summary>
    /// 子弹目标位置
    /// </summary>
    public F64Vec3 targetPosition;
    /// <summary>
    /// 是否为跟踪弹
    /// </summary>
    public bool canFollowTarget;
    /// <summary>
    /// 是否为飞行固定距离的子弹，注意，该值与canFollowTarget不能同时为true，逻辑有冲突
    /// </summary>
    public bool isDistanceFixed;
    /// <summary>
    /// 子弹表
    /// </summary>
    public BulletTableData tableData;

    public long totalTime;
    public long timer;
    /// <summary>
    /// 子弹生命周期是否已尽
    /// </summary>
    public bool isAlive;
    /// <summary>
    /// 子弹是否已经用过了，一个生命周期内只能用一次
    /// </summary>
    public bool isUsed;
    public HashSet<int> hitTargetList;
    public long timeDelta;

    /// <summary>
    /// 子弹是否成功命中敌人（即敌人没有闪避）
    /// </summary>
    public bool isHit;

    /// <summary>
    /// 初始化子弹各项属性
    /// </summary>
    /// <param name="id">子弹ID</param>
    /// <param name="shooterIndex">发射子弹的单位索引</param>
    /// <param name="speed">子弹速度</param>
    /// <param name="damage">子弹伤害</param>
    /// <param name="bulletStartPosition">子弹计算时间使用的起始位置</param>
    /// <param name="viewStartPosition">子弹视觉起始位置</param>
    /// <param name="targetPosition">子弹目标位置</param>
    /// <param name="hitTargetCallback">子弹命中目标回调</param>
    public virtual void Initialize()
    {
        uniqueId = BulletUniqueId++;
        isAlive = true;
        F64 initDistance = F64Vec3.DistanceFastest(targetPosition, startPosition);
        totalTime = F64.FloorToInt(initDistance / speed * 1000);
        if (isDistanceFixed)
        {
            hitTargetList = new HashSet<int>();
        }
    }

    public virtual void CustomUpdate(long timeDelta)
    {
        this.timeDelta = timeDelta;
        if (timer >= totalTime)
        {
            isAlive = false;
        }
        else
        {
            timer += timeDelta;
        }
        if (canFollowTarget)
        {
            LogicBattleUnit logicBattleUnit = LogicBattleSystem.Instance.GetLogicBattleUnit(targetIndex);
            targetPosition = logicBattleUnit.runtimeData.pos;
        }
        if (isDistanceFixed)
        {
            var bulletPos = F64Vec3.Lerp(startPosition, targetPosition, F64.FromRaw(timer) / F64.FromRaw(totalTime));
            //固定距离的子弹需要在飞行过程中进行碰撞检测来决定对哪些单位造成伤害，因为目标是不确定的，要在飞行过程中动态查寻
            var targetList = shooterIndex >= LogicBattleUnit.Attacker_Index_Offset ? LogicBattleSystem.Instance.ActiveDefenderList : LogicBattleSystem.Instance.ActiveAttackerList;

            //判断子弹是否命中很简单，就看子弹位置是否在目标体积范围内
            foreach (var target in targetList)
            {
                if (hitTargetList.Contains(target.index))
                {
                    //一支箭一次生命周期只能对一个目标伤害一次
                    continue;
                }
                F64Vec3 dir = bulletPos - target.runtimeData.pos;
                F64 distance = F64Vec3.LengthFastest(dir);
                bool hit = distance <= target.staticData.volumeRadius;
                if (hit)
                {
                    //命中目标
                    hitTargetList.Add(target.index);
                    LogicBattleSystem.Instance.LogicBulletHitTarget(this, target);
                    GameFramework.Debug.Log($"bullet {this.uniqueId} 碰撞判断 hit {hit} target {target.id} {target.index} bulletPos {bulletPos} targetPos {target.runtimeData.pos} distance {distance} volume {target.staticData.volumeRadius}");
                }
            }
        }
    }

    public virtual void Clear() 
    {
        uniqueId = 0;
        shooterIndex = 0;
        targetIndex = 0;
        speed = F64.Zero;
        damage = 0;
        targetPosition = F64Vec3.Zero;
        canFollowTarget = false;
        isDistanceFixed = false;
        hitTargetList?.Clear();

        totalTime = 0;
        timer = 0;
        isAlive = false;
        isUsed = false;
        isHit = false;
    }
}
