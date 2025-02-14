using FixPointUnity;
using GameFramework;
using System;
using System.Collections.Generic;

public partial class LogicBattleSystem
{
    /// <summary>
    /// 生成单位的时间间隔，单位毫秒
    /// </summary>
    private const int SpawnBattleUnitDelta = 1000;
    /// <summary>
    /// 每次生成一列时的最大容量
    /// </summary>
    private const int ColumnCapactiy = 50;

    private GenericPool<LogicBattleUnit> logicBattleUnitPool = new GenericPool<LogicBattleUnit>();

    private Dictionary<int, int> attackerBattleUnitSpawnDict = new Dictionary<int, int>();
    private Dictionary<int, int> defenderBattleUnitSpawnDict = new Dictionary<int, int>();

    public LogicBattleUnit GetBasecampLogicBattleUnit(bool isAttacker, int maxHP, F64Vec3 pos)
    {
        LogicBattleUnit logicBattleUnit = logicBattleUnitPool.GetInstance();

        logicBattleUnit.staticData.maxHP = maxHP;
        logicBattleUnit.staticData.isBasecamp = true;
        logicBattleUnit.runtimeData.currentStatus = BattleUnitState.Idle;
        logicBattleUnit.runtimeData.hp = logicBattleUnit.staticData.maxHP;
        logicBattleUnit.runtimeData.pos = pos;
        if (isAttacker)
        {
            logicBattleUnit.index = LogicBattleUnit.AttackerIndex;
            LogicBattleUnit.AttackerIndex++;
        }
        else
        {
            logicBattleUnit.index = LogicBattleUnit.DefenderIndex;
            LogicBattleUnit.DefenderIndex++;
        }
        logicBattleUnit.name = $"{logicBattleUnit.id}_{logicBattleUnit.index}";
        return logicBattleUnit;
    }

    public LogicBattleUnit GetOneLogicBattleUnit(int id, bool isAttacker)
    {
        LogicBattleUnit logicBattleUnit = logicBattleUnitPool.GetInstance();

        logicBattleUnit.id = id;
        logicBattleUnit.runtimeData.currentStatus = BattleUnitState.Idle;
        logicBattleUnit.runtimeData.hp = logicBattleUnit.staticData.maxHP;
        logicBattleUnit.staticData.moveSpeed =  F64.FromInt(4);
        if (isAttacker)
        {
            logicBattleUnit.index = LogicBattleUnit.AttackerIndex;
            LogicBattleUnit.AttackerIndex++;
        }
        else
        {
            logicBattleUnit.index = LogicBattleUnit.DefenderIndex;
            LogicBattleUnit.DefenderIndex++;
        }
        logicBattleUnit.name = $"{logicBattleUnit.id}_{logicBattleUnit.index}";
        return logicBattleUnit;
    }

    private void SpawnBattleUnit()
    {
        spawnTimer += COMPUTE_DELTA_TICKS;
        if (spawnTimer < SPAWN_DELTA_TICKS)
        {
            return;
        }

        spawnTimer -= SPAWN_DELTA_TICKS;

        foreach (var kvp in attackerBattleUnitSpawnDict)
        {
            int id = kvp.Key;
            int restCount = kvp.Value;
            for (int i = 0; i < ColumnCapactiy; i++)
            {
                LogicBattleUnit logicBattleUnit = GetOneLogicBattleUnit(id, true);
                float lerpValue = 0;
                if (restCount > ColumnCapactiy)
                {
                    //当一次生成数量能填满整列时，坐标按顺序生成
                    lerpValue = (float)i / ColumnCapactiy;
                }
                else
                {
                    //当一次生成数量不满整列时，居中生成，避免偏向一边不好看
                    float offset = (ColumnCapactiy - restCount) / 2f;
                    lerpValue = (float)i / ColumnCapactiy + offset;
                }
                logicBattleUnit.runtimeData.pos = InputData.attackerBasecamp.GetSpawnPosition(lerpValue);
                AddCombatUnit(logicBattleUnit, true, true);
            }
        }
        foreach (var kvp in defenderBattleUnitSpawnDict)
        {
            int id = kvp.Key;
            int restCount = kvp.Value;
            for (int i = 0; i < ColumnCapactiy; i++)
            {
                LogicBattleUnit logicBattleUnit = GetOneLogicBattleUnit(id, false);
                float lerpValue = 0;
                if (restCount > ColumnCapactiy)
                {
                    //当一次生成数量能填满整列时，坐标按顺序生成
                    lerpValue = (float)i / ColumnCapactiy;
                }
                else
                {
                    //当一次生成数量不满整列时，居中生成，避免偏向一边不好看
                    float offset = (ColumnCapactiy - restCount) / 2f;
                    lerpValue = (float)i / ColumnCapactiy + offset;
                }
                logicBattleUnit.runtimeData.pos = InputData.defenderBasecamp.GetSpawnPosition(lerpValue);
                AddCombatUnit(logicBattleUnit, true, true);
            }
        }
    }

    private void BattleUnitCompute()
    {
        foreach (var unit in allAttackerList)
        {
            BattleUnitState name = unit.runtimeData.currentStatus;
            if (name != BattleUnitState.Dead)
            {
                StatusMachineDic[name].StatusUpdate(unit);
                SkillCDUpdate(unit);
                //CollideUpdate(unit);
            }
            else
            {
                deadList.Add(unit);
            }
        }

        foreach (var unit in allDefenderList)
        {
            BattleUnitState name = unit.runtimeData.currentStatus;
            if (name != BattleUnitState.Dead)
            {
                StatusMachineDic[name].StatusUpdate(unit);
                SkillCDUpdate(unit);
                //CollideUpdate(unit);
            }
            else
            {
                deadList.Add(unit);
            }
        }

        foreach (var unit in deadList)
        {
            RemoveDeadUnit(unit);
        }
        deadList.Clear();
    }

    /// <summary>
    /// 添加战斗单位
    /// </summary>
    /// <param name="logicBattleUnit">数据类</param>
    /// <param name="newCombatUnit">是否是新添加的（复活技能）</param>
    /// <param name="addBattleUnit">是否需要添加BattleUnit（先有实体，后又数据）</param>
    public void AddCombatUnit(LogicBattleUnit logicBattleUnit, bool newCombatUnit, bool addBattleUnit)
    {
        if (logicBattleUnit.IsAttacker)
        {
            AddAttacker(logicBattleUnit, newCombatUnit, addBattleUnit);
        }
        else
        {
            AddDefender(logicBattleUnit, newCombatUnit, addBattleUnit);
        }
    }

    private void AddAttacker(LogicBattleUnit logicBattleUnit, bool newCombatUnit, bool addBattleUnit)
    {
        aliveAttackerSet.Add(logicBattleUnit);
        if (newCombatUnit)
        {
            allAttackerList.Add(logicBattleUnit);
            attackerDic.Add(logicBattleUnit.index, logicBattleUnit);
        }
        if (addBattleUnit)
        {
            currentFrameOutputData.addAttackerUnits.Add(logicBattleUnit);
        }
    }

    private void AddDefender(LogicBattleUnit logicBattleUnit, bool newCombatUnit, bool addBattleUnit)
    {
        aliveDefenderSet.Add(logicBattleUnit);
        if (newCombatUnit)
        {
            allDefenderList.Add(logicBattleUnit);
            defenderDic.Add(logicBattleUnit.index, logicBattleUnit);
        }
        if (addBattleUnit)
        {
            currentFrameOutputData.addDefenderUnits.Add(logicBattleUnit);
        }
    }

    public LogicBattleUnit GetLogicBattleUnit(int index)
    {
        bool isAttacker = index >= LogicBattleUnit.Attacker_Index_Offset;
        if (isAttacker)
        {
            return attackerDic[index];
        }
        else
        {
            return defenderDic[index];
        }
    }

    private void ClearLogicBattleUnit()
    {
        foreach (var item in attackerDic)
        {
            logicBattleUnitPool.RecycleInstance(item.Value);
            item.Value.Clear();
        }
        attackerDic.Clear();
        allAttackerList.Clear();
        aliveAttackerSet.Clear();

        foreach (var item in defenderDic)
        {
            logicBattleUnitPool.RecycleInstance(item.Value);
            item.Value.Clear();
        }
        defenderDic.Clear();
        allDefenderList.Clear();
        aliveDefenderSet.Clear();

        foreach (var item in addonColliderList)
        {
            logicBattleUnitPool.RecycleInstance(item);
            item.Clear();
        }
        addonColliderList.Clear();
        LogicBattleUnit.AttackerIndex = LogicBattleUnit.Attacker_Index_Offset;
        LogicBattleUnit.DefenderIndex = LogicBattleUnit.Defender_Index_Offset;
    }

    /// <summary>
    /// 碰撞检测，算法是，每个角色都有体积半径 volumeRadius，以角色所在位置为圆心，体积半径为碰撞圆半径
    /// 每帧检查与之重叠的目标，根据这些目标的方位，计算出一个被挤动的方向，往这个方向进行位移
    /// </summary>
    private void CollideUpdate(LogicBattleUnit logicBattleUnit)
    {
        if (logicBattleUnit.runtimeData.currentStatus == BattleUnitState.PerformSkill)
        {
            return;
        }
        F64Vec3 squeezeDir = F64Vec3.Zero;
        int selfMass = logicBattleUnit.staticData.mass;

        foreach (var target in aliveAttackerSet)
        {
            if (target != logicBattleUnit && target.staticData.unitType == logicBattleUnit.staticData.unitType)
            {
                F64Vec3 dir = logicBattleUnit.runtimeData.pos - target.runtimeData.pos;
                F64 distance = F64Vec3.LengthFastest(dir);
                F64 radiusSum = target.staticData.volumeRadius + logicBattleUnit.staticData.volumeRadius;
                F64 overlap = radiusSum - distance;
                if (overlap > 0)
                {
                    F64Vec3 moveVector = F64Vec3.NormalizeFastest(dir) * overlap;
                    int targetMass = target.staticData.mass;

                    if (targetMass > selfMass)
                    {
                        //目标单位质量大等于当前单位，当前单位被挤动，目标单位    不动
                        squeezeDir += moveVector;
                    }
                    else if (targetMass == selfMass)
                    {
                        //目标单位质量等于当前单位，双方各退一步
                        squeezeDir += moveVector / F64.FromInt(2);

                        target.runtimeData.pos += -moveVector / F64.FromInt(2);
                    }
                    else
                    {
                        //目标单位质量小于当前单位，目标单位被挤动，当前单位不叠加挤动方向
                        target.runtimeData.pos += -moveVector;
                    }
                }
            }
        }

        foreach (var target in aliveDefenderSet)
        {
            if (target != logicBattleUnit && target.staticData.unitType == logicBattleUnit.staticData.unitType)
            {
                F64Vec3 dir = logicBattleUnit.runtimeData.pos - target.runtimeData.pos;
                F64 distance = F64Vec3.LengthFastest(dir);
                F64 radiusSum = (F64)(target.staticData.volumeRadius + logicBattleUnit.staticData.volumeRadius);
                F64 overlap = radiusSum - distance;
                if (overlap > 0)
                {
                    F64Vec3 moveVector = F64Vec3.NormalizeFastest(dir) * overlap;
                    int targetMass = target.staticData.mass;

                    if (targetMass > selfMass)
                    {
                        //目标单位质量大于当前单位，当前单位被挤动，目标单位不动
                        squeezeDir += moveVector;
                    }
                    else if (targetMass == selfMass)
                    {
                        //目标单位质量等于当前单位，双方各退一步
                        squeezeDir += moveVector / F64.FromInt(2);
                        target.runtimeData.pos += -moveVector / F64.FromInt(2);
                    }
                    else
                    {
                        //目标单位质量小于当前单位，目标单位被挤动，当前单位不叠加挤动方向
                        target.runtimeData.pos += -moveVector;
                    }
                }
            }
        }

        logicBattleUnit.runtimeData.pos += squeezeDir;
    }
}
