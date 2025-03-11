using FixPointUnity;
using Unity.Entities;
using UnityEngine;
using Debug = UnityEngine.Debug;

public readonly partial struct SpawnUnitAspect : IAspect
{
    public readonly RefRO<BattleFieldComponentData> battleFieldData;
    public readonly DynamicBuffer<UnitSpawnBufferData> spawnDatas;
}

[UpdateInGroup(typeof(LogicSystemGroup))]
public partial struct UnitSpawnSystem : ISystem
{
    /// <summary>
    /// 每次生成一列时的最大容量
    /// </summary>
    private const int ColumnCapactiy = 50;
    /// <summary>
    /// 每次生成间隔
    /// </summary>
    private const float SpawnDelta = 2;

    /// <summary>
    /// 创建单位计时器
    /// </summary>
    private float spawnTimer;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitSpawnBufferData>();
        state.RequireForUpdate<BattleFieldComponentData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var archeType = state.EntityManager.CreateArchetype(
            typeof(HP),
            typeof(Position),
            typeof(UnitStatus),
            typeof(AttackTimer),
            typeof(AttackPower),
            typeof(AttackPerformed),
            typeof(UnitStaticData),
            typeof(BattleUnitReference));

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var aspect in SystemAPI.Query<SpawnUnitAspect>())
        {
            foreach (var data in aspect.spawnDatas)
            {
                int id = data.id;
                int count = data.count;
                if (count == 0)
                {
                    return;
                }

                foreach (var prefabData in SystemAPI.Query<BattleUnitPrefabData>())
                {
                    if (prefabData.id == id)
                    {
                        Debug.Log($"Create unit id {id} count {count}");

                        for (int index = 0; index < count; index++)
                        {
                            var entity = ecb.CreateEntity(archeType);
                            F64 lerpValue;
                            if (count > ColumnCapactiy)
                            {
                                //当一次生成数量能填满整列时，坐标按顺序生成
                                lerpValue = F64.FromInt(index) / F64.FromInt(ColumnCapactiy);
                            }
                            else
                            {
                                //当一次生成数量不满整列时，居中生成，避免偏向一边不好看
                                F64 offset = F64.FromInt(ColumnCapactiy - count) / F64.FromInt(2);
                                lerpValue = (index + offset) / ColumnCapactiy;
                            }

                            var staticData = new UnitStaticData();
                            var hp = new HP();
                            var position = new Position();
                            var status = new UnitStatus();
                            var attackPower = new AttackPower();

                            if (data.isAttacker)
                            {
                                staticData.unitCamp = UnitCamp.Attacker;
                                position.value = F64Vec3.Lerp(aspect.battleFieldData.ValueRO.attackerSpawnPointA, aspect.battleFieldData.ValueRO.attackerSpawnPointB, lerpValue);
                            }
                            else
                            {
                                staticData.unitCamp = UnitCamp.Defender;
                                position.value = F64Vec3.Lerp(aspect.battleFieldData.ValueRO.defenderSpawnPointB, aspect.battleFieldData.ValueRO.defenderSpawnPointA, lerpValue);
                            }

                            position.value.X /= 3;

                            var battleUnit = Object.Instantiate(prefabData.prefab).GetComponent<BattleUnit>();
                            //var battleUnit = BattleSystem.BattleField.SpawnBattleUnit(data.isAttacker, id, position.value);
                            battleUnit.Initialize(id, position.value.ToVector3(), data.isAttacker);

                            hp.value = 3;
                            attackPower.value = 1;
                            status.value = BattleUnitState.MoveToBasecamp;
                            staticData.moveSpeed = F64.FromInt(5);
                            staticData.volumeRadius = F64.FromFloat(0.5f);
                            staticData.attackRadius = F64.FromInt(1);
                            staticData.attackTime = F64.FromFloat(0.667f);
                            staticData.attackPreTime = F64.FromFloat(0.3f);
                            staticData.attackWaitTime = F64.FromInt(1);

                            ecb.SetComponent(entity, hp);
                            ecb.SetComponent(entity, position);
                            ecb.SetComponent(entity, status);
                            ecb.SetComponent(entity, attackPower);
                            ecb.SetComponent(entity, staticData);
                            ecb.SetComponent(entity, new BattleUnitReference() { battleUnit = battleUnit });
                        }
                        break;
                    }
                }
            }

            aspect.spawnDatas.Clear();
        }
    }
}
