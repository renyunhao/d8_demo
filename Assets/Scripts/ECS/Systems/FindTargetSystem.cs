using FixPointUnity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public enum FindTargetType
{
    /// <summary>
    /// 单体攻击
    /// </summary>
    AttackSingle,
    /// <summary>
    /// 群体攻击，攻击范围内所有单位
    /// </summary>
    AttackMultiple,
    /// <summary>
    /// 指定范围
    /// </summary>
    SpecifyRange
}

public partial struct NeedFindTargetTag : IComponentData 
{
    public FindTargetType findTargetType;
    public F64Vec3 center;
    public F64 range;
}

[UpdateInGroup(typeof(LogicSystemGroup))]
public partial struct FindTargetSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<NeedFindTargetTag>().Build();

        NativeParallelMultiHashMap<int, Entity> targetHashMap = new NativeParallelMultiHashMap<int, Entity>(query.CalculateEntityCount(), Allocator.TempJob);
        var burstJobHandle = new FindTargetBurstJob()
        {
            quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
            targetHashMap = targetHashMap.AsParallelWriter(),
        }.ScheduleParallel(state.Dependency);

        burstJobHandle.Complete();

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var entity in query.ToEntityArray(Allocator.Temp))
        {
            int entityCount = targetHashMap.CountValuesForKey(entity.Index);
            if (entityCount > 0)
            {
                NativeArray<Entity> targets = new NativeArray<Entity>(entityCount, Allocator.Persistent);
                int index = 0;
                if (targetHashMap.TryGetFirstValue(entity.Index, out var target, out var iterator))
                {
                    do
                    {
                        targets[index] = target;
                    }
                    while (targetHashMap.TryGetNextValue(out target, ref iterator));
                }
                ecb.AddComponent(entity, new HasTargetComponentData() { targets = targets });
                ecb.RemoveComponent<NeedFindTargetTag>(entity);
            }
        }

        targetHashMap.Dispose();
        ecb.Playback(state.EntityManager);
    }

    [BurstCompile]
    private partial struct FindTargetBurstJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
        public NativeParallelMultiHashMap<int, Entity>.ParallelWriter targetHashMap;

        public void Execute([ReadOnly] UnitDataAspect unitData, [ReadOnly] NeedFindTargetTag needFindTargetTag)
        {
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(unitData.Position);

            Entity closestEntity = Entity.Null;
            F64 minDistance = F64.MaxValue;

            //搜索目标当前格及周围8格
            FindTarget(hashMapKey, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey + 1, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey - 1, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey + QuadrantSystem.QuadrantZMultiplier, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey - QuadrantSystem.QuadrantZMultiplier, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey + 1 + QuadrantSystem.QuadrantZMultiplier, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey + 1 - QuadrantSystem.QuadrantZMultiplier, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey - 1 + QuadrantSystem.QuadrantZMultiplier, unitData, needFindTargetTag, ref closestEntity, ref minDistance);
            FindTarget(hashMapKey - 1 - QuadrantSystem.QuadrantZMultiplier, unitData, needFindTargetTag, ref closestEntity, ref minDistance);

            if (needFindTargetTag.findTargetType == FindTargetType.AttackSingle)
            {
                if (minDistance <= unitData.AttackRadius)
                {
                    targetHashMap.Add(unitData.entity.Index, closestEntity);
                }
            }
        }

        private void FindTarget(int hashMapKey, UnitDataAspect unitData, NeedFindTargetTag needFindTargetTag, ref Entity closestEntity, ref F64 minDistance)
        {
            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out var quadrantData, out var iterator))
            {
                F64Vec3 unitPosition = unitData.Position;
                do
                {
                    if (unitData.UnitCamp == quadrantData.unitCamp)
                    {
                        continue;
                    }

                    if (closestEntity == null)
                    {
                        closestEntity = quadrantData.entity;
                        minDistance = F64Vec3.DistanceFastest(unitPosition, quadrantData.position);
                    }
                    else
                    {
                        F64 newDistance = F64Vec3.DistanceFastest(unitPosition, quadrantData.position);
                        if (newDistance < minDistance)
                        {
                            closestEntity = quadrantData.entity;
                            minDistance = newDistance;
                        }

                        if (needFindTargetTag.findTargetType == FindTargetType.AttackMultiple)
                        {
                            //若当前单位为群攻，只要目标距离小于攻击范围，就算作目标
                            if (newDistance <= unitData.AttackRadius)
                            {
                                targetHashMap.Add(unitData.entity.Index, closestEntity);
                            }
                        }
                    }
                }
                while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref iterator));
            }
        }
    }
}
