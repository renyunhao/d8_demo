using FixPointUnity;
using GameFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// 战斗逻辑计算，所有的战斗计算在这里完成
/// </summary>
public partial class LogicBattleSystem
{
    public static LogicBattleSystem Instance;
    /// <summary>
    /// 战斗计算间隔，单位Ticks
    /// </summary>
    public const long COMPUTE_DELTA_TICKS = 200000;
    /// <summary>
    /// 创建单位间隔，单位Ticks
    /// </summary>
    public const long SPAWN_DELTA_TICKS = 1 * SECOND_TO_TICKS;
    /// <summary>
    /// 战斗计算间隔，单位毫秒
    /// </summary>
    public const long COMPUTE_DELTA_MILLISECOND = 20;
    /// <summary>
    /// Tick转毫秒要乘的数字
    /// </summary>
    public const long TICKS_TO_MILLISECOND = 10000;
    /// <summary>
    /// 战斗计算间隔，单位秒
    /// </summary>
    public static F64 COMPUTE_DELTA_SECOND = F64.FromDouble(0.02);
    /// <summary>
    /// 1秒对应的Ticks数
    /// </summary>
    public const long SECOND_TO_TICKS = 1000 * 1000 * 10;
    /// <summary>
    /// 1秒对应的毫秒数
    /// </summary>
    public const long SECOND_TO_MILLISECOND = 1000;
    /// <summary>
    /// 前一次计算时的Tick数
    /// </summary>
    private long previousElapsedTicks = 0;
    /// <summary>
    /// 两次计算间隔的Tick数
    /// </summary>
    private long deltaTicks = 0;
    /// <summary>
    /// 战斗计算计时器
    /// </summary>
    private long battleComputeTimer = 0;
    /// <summary>
    /// 创建单位计时器
    /// </summary>
    private long spawnTimer = 0;
    /// <summary>
    /// 战斗计时器, 单位Tick
    /// </summary>
    private long battleTimer = 0;
    /// <summary>
    /// 战斗计时器, 单位毫秒
    /// </summary>
    public long battleTimerMS = 0;
    /// <summary>
    /// 战斗计时秒表
    /// </summary>
    private Stopwatch battleStopwatch = new Stopwatch();

    /// <summary>
    /// 客户端随机（服务端与客户端随机不能混用，随机次数不一致随机结果不一致）
    /// </summary>
    public SimpleRandom clientRandom;
    /// <summary>
    /// 防守方战斗单位，Key为LogicBattleUnit的index，Value是LogicBattleUnit
    /// </summary>
    private Dictionary<int, LogicBattleUnit> defenderDic = new Dictionary<int, LogicBattleUnit>();
    /// <summary>
    /// 存活的防守方单位集合
    /// </summary>
    private HashSet<LogicBattleUnit> aliveDefenderSet = new HashSet<LogicBattleUnit>();
    /// <summary>
    /// 所有的防守方单位列表
    /// </summary>
    private List<LogicBattleUnit> allDefenderList = new List<LogicBattleUnit>();
    public LogicBattleUnit defenderBasecamp;

    /// <summary>
    /// 进攻方战斗单位，Key为LogicBattleUnit的index，Value是LogicBattleUnit
    /// </summary>
    private Dictionary<int, LogicBattleUnit> attackerDic = new Dictionary<int, LogicBattleUnit>();
    /// <summary>
    /// 存活的进攻方单位集合
    /// </summary>
    private HashSet<LogicBattleUnit> aliveAttackerSet = new HashSet<LogicBattleUnit>();
    /// <summary>
    /// 所有的进攻方单位列表
    /// </summary>
    private List<LogicBattleUnit> allAttackerList = new List<LogicBattleUnit>();
    public LogicBattleUnit attackerBasecamp;

    /// <summary>
    /// 每一帧中死亡的单位，集中进行移除处理
    /// </summary>
    private List<LogicBattleUnit> deadList = new List<LogicBattleUnit>();
    /// <summary>
    /// 额外的碰撞体（由拒马生成），只参与碰撞计算，不参与战斗
    /// </summary>
    private List<LogicBattleUnit> addonColliderList = new List<LogicBattleUnit>();

    private GenericPool<BattleFrameOutputData> outputDataFramePool = new GenericPool<BattleFrameOutputData>();

    public bool Fighting { get; private set; }
    /// <summary>
    /// 战斗输入数据
    /// </summary>
    public BattleInputData InputData { get; private set; }
    /// <summary>
    /// 战斗输出数据
    /// </summary>
    public BattleOutputData OutputData { get; private set; }
    /// <summary>
    /// 每帧输入数据
    /// </summary>
    public Queue<BattleFrameInputData> inputDatasFrame = new Queue<BattleFrameInputData>();
    /// <summary>
    /// 每帧输出数据
    /// </summary>
    public Queue<BattleFrameOutputData> outputDatasFrame = new Queue<BattleFrameOutputData>();
    /// <summary>
    /// 当前帧输出数据
    /// </summary>
    private BattleFrameOutputData currentFrameOutputData;

    private LogicSystemGroup logicSystemGroup;
    private SystemHandle spawnUnitSystemHandle;
    private SystemHandle quadrantSystemHandle;
    private SystemHandle unitStateSystemHandle;
    private SystemHandle findTargetSystemHandle;
    private SystemHandle unitSyncSystemHandle;

    public HashSet<LogicBattleUnit> ActiveAttackerList => aliveAttackerSet;
    public HashSet<LogicBattleUnit> ActiveDefenderList => aliveDefenderSet;

    public LogicBattleSystem()
    {
        Instance = this;
        string[] names = Enum.GetNames(typeof(BattleUnitState));
        StatusMachineDic = new Dictionary<BattleUnitState, ILogicStatus>(names.Length);
        ILogicStatus idleStatus = new LogicIdleStatus();
        ILogicStatus moveToAttackStatus = new LogicMoveToAttackStatus();
        ILogicStatus moveToEndStatus = new LogicMoveToEndStatus();
        ILogicStatus attackingStatus = new LogicAttackingStatus();
        ILogicStatus attackCompletedStatus = new LogicAttackWaitStatus();
        ILogicStatus deadStatus = new LogicDeadStatus();
        ILogicStatus performSkillStatus = new LogicPerformSkillStatus();

        StatusMachineDic.Add(idleStatus.Name, idleStatus);
        StatusMachineDic.Add(moveToAttackStatus.Name, moveToAttackStatus);
        StatusMachineDic.Add(moveToEndStatus.Name, moveToEndStatus);
        StatusMachineDic.Add(attackingStatus.Name, attackingStatus);
        StatusMachineDic.Add(attackCompletedStatus.Name, attackCompletedStatus);
        StatusMachineDic.Add(deadStatus.Name, deadStatus);
        StatusMachineDic.Add(performSkillStatus.Name, performSkillStatus);

        foreach (var skillEffectType in Enum.GetValues(typeof(SkillEffect)))
        {
            skillPoolDict.Add((SkillEffect)skillEffectType, new Queue<LogicSkillBase>(10));
        }
    }

    public void Initialize(BattleInputData inputData)
    {
        InputData = inputData;
        OutputData = new BattleOutputData();
        clientRandom = new SimpleRandom(0);
        //双方的城池需要构建为一个虚拟的BattleUnit
        attackerBasecamp = GetBasecampLogicBattleUnit(true, 10000, inputData.attackerBasecamp.transform.position.ToF64Vec3());
        defenderBasecamp = GetBasecampLogicBattleUnit(false, 10000, inputData.defenderBasecamp.transform.position.ToF64Vec3());
        spawnUnitSystemHandle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<UnitSpawnSystem>();
        quadrantSystemHandle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<QuadrantSystem>();
        unitStateSystemHandle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<UnitStateSystem>();
        findTargetSystemHandle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<FindTargetSystem>();
        unitSyncSystemHandle = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<UnitSyncSystem>();
        logicSystemGroup = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<LogicSystemGroup>();
        logicSystemGroup.RemoveSystemFromUpdateList(spawnUnitSystemHandle); //关掉System的自动Update，改为手动调用Update
        logicSystemGroup.RemoveSystemFromUpdateList(quadrantSystemHandle); //关掉System的自动Update，改为手动调用Update
        logicSystemGroup.RemoveSystemFromUpdateList(unitStateSystemHandle); //关掉System的自动Update，改为手动调用Update
        logicSystemGroup.RemoveSystemFromUpdateList(findTargetSystemHandle); //关掉System的自动Update，改为手动调用Update
        logicSystemGroup.RemoveSystemFromUpdateList(unitSyncSystemHandle); //关掉System的自动Update，改为手动调用Update
    }

    #region 战斗阶段

    public void StartFight()
    {
        battleStopwatch.Restart();
        Fighting = true;
    }

    public void Update()
    {
        int timeScale = InputData.timeScale;
        long currentElapsedTicks = battleStopwatch.ElapsedTicks;
        deltaTicks = currentElapsedTicks - previousElapsedTicks;
        previousElapsedTicks = currentElapsedTicks;
        battleComputeTimer += deltaTicks;

        //两次Update之间的间隔时间可能超过一次COMPUTE_DELTA_TICKS，够一次，计算一次
        bool needUpdate = false;
        while (battleComputeTimer > COMPUTE_DELTA_TICKS)
        {
            battleComputeTimer -= COMPUTE_DELTA_TICKS;
            needUpdate = true;
        }
        if (needUpdate) {
            for (int i = 0; i < timeScale; i++)
            {
                currentFrameOutputData = outputDataFramePool.GetInstance();
                //处理每帧输入数据
                ProcessInputData();
                //检查战斗是否可结束
                CheckBattleEnd();
                if (OutputData.isEnd == false)
                {
                    SpawnBattleUnit();
                    var world = World.DefaultGameObjectInjectionWorld.Unmanaged;
                    spawnUnitSystemHandle.Update(world);
                    quadrantSystemHandle.Update(world);
                    unitStateSystemHandle.Update(world);
                    findTargetSystemHandle.Update(world);
                    unitSyncSystemHandle.Update(world);
                    BattleUnitCompute();
                    BulletCompute();
                    SkillCompute();
                }
                outputDatasFrame.Enqueue(currentFrameOutputData);
                battleTimer += COMPUTE_DELTA_TICKS;
                battleTimerMS = battleTimer / TICKS_TO_MILLISECOND;
            }
        }
    }

    private void CheckBattleEnd()
    {
        if (IsAttackerBasecampDestroyed())
        {
            if (HasUnFinishedSkillEffect())
            {
                ResetAllCombatUnit();
            }
            else
            {
                OutputData.isEnd = true;
            }
        }
        if (IsDefenderBasecampDestroyed())
        {
            if (HasUnFinishedSkillEffect())
            {
                ResetAllCombatUnit();
            }
            else
            {
                OutputData.isEnd = true;
            }
        }
        if (OutputData.isEnd)
        {
            BattleEnd();
        }
    }

    /// <summary>
    /// 进攻方团灭
    /// </summary>
    /// <returns></returns>
    private bool IsAttackerBasecampDestroyed()
    {
        return attackerBasecamp.runtimeData.currentStatus == BattleUnitState.Dead;
    }

    /// <summary>
    /// 防守方团灭
    /// </summary>
    /// <returns></returns>
    private bool IsDefenderBasecampDestroyed()
    {
        return defenderBasecamp.runtimeData.currentStatus == BattleUnitState.Dead;
    }

    /// <summary>
    /// 战斗结束
    /// </summary>
    private void BattleEnd()
    {
        if (currentFrameOutputData == null)
        {
            currentFrameOutputData = outputDataFramePool.GetInstance();
        }
        currentFrameOutputData.isEnd = true;
        ResetAllCombatUnit();
        UnityEngine.Debug.Log("战斗结束");
    }

    private void ResetAllCombatUnit()
    {
        //战斗结束时，将所有存活的单位状态重置为Idle，否则有些单位将一些停留在攻击状态，重复播放动画
        foreach (var unit in aliveAttackerSet)
        {
            if (unit.runtimeData.currentStatus != BattleUnitState.Idle && unit.runtimeData.hp > 0)
            {
                SwitchStatus(unit, BattleUnitState.Idle);
            }
        }

        foreach (var unit in aliveDefenderSet)
        {
            if (unit.runtimeData.currentStatus != BattleUnitState.Idle && unit.runtimeData.hp > 0)
            {
                SwitchStatus(unit, BattleUnitState.Idle);
            }
        }
    }

    #endregion

    #region 清理

    public void Clear()
    {
        previousElapsedTicks = 0;
        battleComputeTimer = 0;
        battleTimer = 0;
        battleTimerMS = 0;
        battleStopwatch.Stop();
        Fighting = false;

        ClearLogicBattleUnit();
        ClearOnceAttackRelate();
        ClearBullet();
        ClearSkill();
        ClearBattleFrameData();
    }

    private void ClearBattleFrameData()
    {
        foreach (var item in outputDatasFrame)
        {
            RecycleBattleFrameData(item);
        }
        outputDatasFrame.Clear();
    }

    #endregion

    /// <summary>
    /// 处理每帧输入数据
    /// </summary>
    private void ProcessInputData()
    {
        while (inputDatasFrame.Count > 0)
        {
            BattleFrameInputData frameData = inputDatasFrame.Peek();
            if (frameData.time == battleTimerMS)
            {
                inputDatasFrame.Dequeue();
                OutputData.isEnd = frameData.isEnd;
                if (frameData.addBattleUnitData?.campID == InputData.attackerCampID)
                {
                    attackerBattleUnitSpawnDict.TryGetValue(frameData.addBattleUnitData.soldierID, out int existCount);
                    attackerBattleUnitSpawnDict[frameData.addBattleUnitData.soldierID] = existCount + frameData.addBattleUnitData.count;
                }
                else if (frameData.addBattleUnitData?.campID == InputData.defenderCampID)
                {
                    defenderBattleUnitSpawnDict.TryGetValue(frameData.addBattleUnitData.soldierID, out int existCount);
                    defenderBattleUnitSpawnDict[frameData.addBattleUnitData.soldierID] = existCount + frameData.addBattleUnitData.count;
                }
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// 设定战斗速度
    /// </summary>
    /// <param name="timeScale"></param>
    public void ModifyBattleTimescale()
    {
        InputData.timeScaleIndex++;
        InputData.timeScale = BattleInputData.TimeScaleArray[InputData.timeScaleIndex % BattleInputData.TimeScaleArray.Length];
    }

    /// <summary>
    /// 判断目标是否在范围内
    /// 注意：
    /// 1.判断时要考虑被攻击方的体积
    /// 2.有些单位的攻击范围不是圆,而是扇形
    /// </summary>
    /// <param name="maxRange">最大范围</param>
    /// <param name="attackerPos"></param>
    /// <param name="victimVolume"></param>
    /// <param name="victimPos"></param>
    /// <returns></returns>
    public bool IsInRange(F64 maxRange, F64 minRange, F64Vec3 attackerPos, F64 victimVolume, F64Vec3 victimPos, int angle)
    {
        F64 distance = F64Vec3.Distance(attackerPos, victimPos);
        bool inCircleRange = distance >= minRange && distance <= (maxRange + victimVolume);
        return inCircleRange;
    }

    public void RecycleBattleFrameData(BattleFrameOutputData battleFrameData)
    {
        battleFrameData.Clear();
        outputDataFramePool.RecycleInstance(battleFrameData);
    }

    public F64Vec3 RandomOnePosition(F64Vec3 position, F64 range)
    {
        int angle = clientRandom.Next(0, 360);
        F64 radian = F64.Pi / 180 * angle;
        int r = clientRandom.Next(0, (int)(range.Double * 1000));
        F64 x = F64.FromDouble(r * F64.Cos(radian).Double * 0.001f);
        F64 z = F64.FromDouble(r * F64.Sin(radian).Double * 0.001f);
        position.X += x;
        position.Z += z;
        return position;
    }

    public void AddInputDataPerFrame(BattleFrameInputData frameData)
    {
        inputDatasFrame.Enqueue(frameData);
    }
}
