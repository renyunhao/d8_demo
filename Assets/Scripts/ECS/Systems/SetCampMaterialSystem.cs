using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Rendering;

public partial struct CampMaterialApplyed : IComponentData { }

partial class SetCampMaterialSystem : SystemBase
{
    private Dictionary<int, Dictionary<Material, BatchMaterialID>> materialMapping;
    private EntitiesGraphicsSystem hybridRendererSystem;

    protected override void OnCreate()
    {
        Debug.Log("SetCampMaterialSystem OnCreate");
        materialMapping = new Dictionary<int, Dictionary<Material, BatchMaterialID>>();
    }

    protected override void OnUpdate()
    {
        hybridRendererSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        foreach (var prefabData in SystemAPI.Query<BattleUnitPrefabData>())
        {
            if (materialMapping.TryGetValue(prefabData.id, out var dict) == false)
            {
                dict = new Dictionary<Material, BatchMaterialID>();
                materialMapping[prefabData.id] = dict;
                dict[prefabData.attackerMaterial] = hybridRendererSystem.RegisterMaterial(prefabData.attackerMaterial);
                dict[prefabData.defenderMaterial] = hybridRendererSystem.RegisterMaterial(prefabData.defenderMaterial);
            }
        }

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (staticData, childs, entity) in SystemAPI.Query<UnitStaticData, DynamicBuffer<LinkedEntityGroup>>().WithNone<CampMaterialApplyed>().WithEntityAccess())
        {
            foreach (var child in childs)
            {
                if (EntityManager.HasComponent<MaterialMeshInfo>(child.Value))
                {
                    foreach (var prefabData in SystemAPI.Query<BattleUnitPrefabData>())
                    {
                        if (prefabData.id == staticData.id)
                        {
                            var mmi = EntityManager.GetComponentData<MaterialMeshInfo>(child.Value);
                            var material = staticData.unitCamp == UnitCamp.Attacker ? prefabData.attackerMaterial : prefabData.defenderMaterial;
                            mmi.MaterialID = materialMapping[staticData.id][material];
                            ecb.SetComponent(child.Value, mmi);
                            ecb.AddComponent(entity, new CampMaterialApplyed());
                            break;
                        }
                    }
                }
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
