using FixPointUnity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(LogicSystemGroup))]
partial struct CollisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAspect<UnitDataAspect>().Build();

        NativeParallelMultiHashMap<int, Entity> targetHashMap = new NativeParallelMultiHashMap<int, Entity>(query.CalculateEntityCount(), Allocator.TempJob);
        state.Dependency = new CollisionJob()
        {
            quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
        }.ScheduleParallel(query, state.Dependency);
    }

    [BurstCompile]
    private partial struct CollisionJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap;

        public void Execute(UnitDataAspect unitData)
        {
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(unitData.Position);

            F64 collideUnitCount = F64.FromInt(0);
            F64Vec3 pushDirection = F64Vec3.Zero;

            //搜索目标当前格及周围8格
            CollideCheck(hashMapKey, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey + 1, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey - 1, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey + QuadrantSystem.QuadrantZMultiplier, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey - QuadrantSystem.QuadrantZMultiplier, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey + 1 + QuadrantSystem.QuadrantZMultiplier, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey + 1 - QuadrantSystem.QuadrantZMultiplier, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey - 1 + QuadrantSystem.QuadrantZMultiplier, unitData, ref pushDirection, ref collideUnitCount);
            CollideCheck(hashMapKey - 1 - QuadrantSystem.QuadrantZMultiplier, unitData, ref pushDirection, ref collideUnitCount);

            if (pushDirection.X != 0 || pushDirection.Y != 0 || pushDirection.Z != 0)
            {
                //根据排挤向量直接移动目标坐标
                pushDirection /= collideUnitCount;
                //Debug.Log($"Entity {unitData.entity.Index} pushDirection {pushDirection.ToVector3().ToString()}");
                unitData.Position += pushDirection;
            }
        }

        private void CollideCheck(int hashMapKey, UnitDataAspect unitData, ref F64Vec3 pushDirection, ref F64 collideUnitCount)
        {
            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out var quadrantData, out var iterator))
            {
                F64Vec3 unitPosition = unitData.Position;
                do
                {
                    if (unitData.entity.Index == quadrantData.entity.Index)
                    {
                        continue;
                    }
                    if (unitData.UnitCamp != quadrantData.unitCamp)
                    {
                        continue;
                    }
                    F64 distance = F64Vec3.DistanceFastest(unitPosition, quadrantData.position);
                    F64 volumeRadius = unitData.VolumeRadius + quadrantData.volumeRadius;
                    //两个单位间的距离小于单位体积半径之和，表示他们发生了碰撞, 将反向的排挤向量叠加起来
                    if (distance <= volumeRadius)
                    {
                        //Debug.Log($"entity {unitData.entity.Index} collide with {quadrantData.entity.Index} distance {distance} volume {volumeRadius}");
                        F64 gap = volumeRadius - distance;
                        F64Vec3 direction = F64Vec3.NormalizeFastest(unitPosition - quadrantData.position);
                        pushDirection += direction * gap;
                        collideUnitCount++;
                    }
                }
                while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref iterator));
            }
        }
    }
}
