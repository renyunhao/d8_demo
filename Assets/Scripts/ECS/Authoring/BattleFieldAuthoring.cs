using FixPointUnity;
using Unity.Entities;
using UnityEngine;

public class BattleFieldAuthoring : MonoBehaviour
{
    public Basecamp attackerBasecamp;
    public Basecamp defenderBasecamp;
}

public struct BattleFieldComponentData : IComponentData
{
    public F64Vec3 attackerSpawnPointA;
    public F64Vec3 attackerSpawnPointB;
    public F64Vec3 defenderSpawnPointA;
    public F64Vec3 defenderSpawnPointB;
}

public class BattleFieldBaker : Baker<BattleFieldAuthoring>
{
    public override void Bake(BattleFieldAuthoring authoring)
    {
        var battleFieldEntity = GetEntity(TransformUsageFlags.Dynamic);
        BattleSystem.Initialize(authoring.GetComponent<BattleField>());

        Debug.Log("new BattleFieldBaker");
        AddComponent(battleFieldEntity, new BattleFieldComponentData
        {
            attackerSpawnPointA = authoring.attackerBasecamp.battleUnitSpawnPointA.transform.position.ToF64Vec3(),
            attackerSpawnPointB = authoring.attackerBasecamp.battleUnitSpawnPointB.transform.position.ToF64Vec3(),
            defenderSpawnPointA = authoring.defenderBasecamp.battleUnitSpawnPointA.transform.position.ToF64Vec3(),
            defenderSpawnPointB = authoring.defenderBasecamp.battleUnitSpawnPointB.transform.position.ToF64Vec3(),
        });
    }
}
