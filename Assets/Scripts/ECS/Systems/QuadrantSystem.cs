using FixPointUnity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct HasTargetComponentData : IComponentData
{
    public NativeArray<Entity> targets;
}

public struct QuadrantData
{
    public Entity entity;
    public F64Vec3 position;
}

[UpdateInGroup(typeof(LogicSystemGroup))]
public partial struct QuadrantSystem : ISystem
{
    public const int QuadrantCellSize = 10;
    public const int QuadrantZMultiplier = 1000;

    public static NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap;

    public void OnCreate(ref SystemState state)
    {
        quadrantMultiHashMap = new NativeParallelMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
    }

    public void OnDestory(ref SystemState state)
    {
        quadrantMultiHashMap.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<Position>().WithNone<UnitDeadTag>().Build();

        quadrantMultiHashMap.Clear();
        if (query.CalculateEntityCount() > quadrantMultiHashMap.Capacity)
        {
            quadrantMultiHashMap.Dispose();
            quadrantMultiHashMap = new NativeParallelMultiHashMap<int, QuadrantData>(query.CalculateEntityCount(), Allocator.Persistent);
        }

        new SetQuadrantDataHashMapJob()
        {
            quadrantMultiHashMap = quadrantMultiHashMap.AsParallelWriter()
        }.ScheduleParallel();
    }

    public static int GetPositionHashMapKey(F64Vec3 position)
    {
        int coordX = F64.FloorToInt(position.X / QuadrantCellSize);
        int coordZ = F64.FloorToInt(position.Z / QuadrantCellSize);
        return coordX + coordZ * QuadrantZMultiplier;
    }

    [BurstCompile]
    private partial struct SetQuadrantDataHashMapJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, QuadrantData>.ParallelWriter quadrantMultiHashMap;
        public void Execute(UnitDataAspect unitData)
        {
            int hashMapKey = GetPositionHashMapKey(unitData.Position);
            quadrantMultiHashMap.Add(hashMapKey, new QuadrantData()
            {
                entity = unitData.entity,
                position = unitData.Position
            });
        }
    }
}
