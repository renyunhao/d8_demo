using System;
using Unity.Entities;
using UnityEngine;

public class BattleUnitPrefabAuthoring : MonoBehaviour
{
    public GameObject prefab;
    public Material attackerMaterial;
    public Material defenderMaterial;
}

public class BattleUnitPrefabData : IComponentData
{
    public int id;
    public Entity entityPrefab;
    public GameObject prefab;
    public Material attackerMaterial;
    public Material defenderMaterial;
}

public class BattleUnitPrefabBaker : Baker<BattleUnitPrefabAuthoring>
{
    public override void Bake(BattleUnitPrefabAuthoring authoring)
    {
        var prefabContainerEntity = GetEntity(TransformUsageFlags.Dynamic);
        int subIndex = authoring.prefab.name.IndexOf("_");
        int id = Convert.ToInt32(authoring.prefab.name.Substring(subIndex + 1));
        AddComponentObject(prefabContainerEntity, new BattleUnitPrefabData
        {
            id = id,
            entityPrefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            prefab = authoring.prefab,
            attackerMaterial = authoring.attackerMaterial,
            defenderMaterial = authoring.defenderMaterial
        });
    }
}
