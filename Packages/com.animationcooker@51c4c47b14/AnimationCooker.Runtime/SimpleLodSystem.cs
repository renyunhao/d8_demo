// This system launches a job at a specified interval that handles LOD switching.
// It will loop through every entity in the scene that has a SimpleLodData component
// and set its mesh and material.
// It also sets each entity's SimpleLodSkinData value, which can optionally be used
// by other systems to know which LOD index is in use.

using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace AnimCooker
{
    [RequireMatchingQueriesForUpdate]
    public partial struct SimpleLodSystem : ISystem
    {
        UpdateTimer m_timer;

        // a query must be used because of a unity bug that won't allow
        // EnabledRefRW<MaterialMeshInfo> mmiEnabledRef and ref MaterialMeshInfo mmi
        // to be used at the same time if [WithPresent] is in the query.
        EntityQuery m_lodQuery;

        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            m_lodQuery = SystemAPI.QueryBuilder().WithPresentRW<MaterialMeshInfo>().WithAll<SimpleLodSkinData, SimpleLodData, LocalToWorld, SimpleLodInfoData>().Build();
            m_timer = new UpdateTimer(999);
            state.RequireForUpdate<CamData>();
            state.RequireForUpdate<SimpleLodOptsData>();
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            if (m_timer.GetInterval() >= 888) { m_timer.SetInterval(SystemAPI.GetSingleton<SimpleLodOptsData>().TimerInterval); }
            if (m_timer.IsNotReady(SystemAPI.Time.DeltaTime)) { return; }
            SimpleLodJob job = new SimpleLodJob();
            job.Cam = SystemAPI.GetSingleton<CamData>();
            state.Dependency = job.ScheduleParallel(m_lodQuery, state.Dependency);
            //state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }


    //[WithPresent(typeof(MaterialMeshInfo))]
    [BurstCompile]
    public partial struct SimpleLodJob : IJobEntity
    {
        public CamData Cam;

        public void Execute(EnabledRefRW<MaterialMeshInfo> mmiEnabledRef, ref MaterialMeshInfo mmi, ref SimpleLodSkinData skin, in SimpleLodData lod, in LocalToWorld ltw, in SimpleLodInfoData lodInf)
        {
            float dist = lodInf.WorldSpaceSize / (2f * lodInf.ScreenRelativeTransitionHeight0 * math.tan(Cam.FovRad * 0.5f));
            float actualDistSq = math.distancesq(Cam.Pos, ltw.Position);
            if (actualDistSq < (dist * dist)) {
                mmiEnabledRef.ValueRW = true;
                mmi.MaterialID = lod.MatId0;
                mmi.MeshID = lod.MeshId0;
                skin.SkinIndex = 0;
                return;
            }
            dist = lodInf.WorldSpaceSize / (2f * lodInf.ScreenRelativeTransitionHeight1 * math.tan(Cam.FovRad * 0.5f));
            if (actualDistSq < (dist * dist)) {
                mmiEnabledRef.ValueRW = true;
                mmi.MaterialID = lod.MatId1;
                mmi.MeshID = lod.MeshId1;
                skin.SkinIndex = 1;
                return;
            }
            dist = lodInf.WorldSpaceSize / (2f * lodInf.ScreenRelativeTransitionHeight2 * math.tan(Cam.FovRad * 0.5f));
            if (actualDistSq < (dist * dist)) {
                mmiEnabledRef.ValueRW = true;
                mmi.MaterialID = lod.MatId2;
                mmi.MeshID = lod.MeshId2;
                skin.SkinIndex = 2;
                return;
            }

            mmiEnabledRef.ValueRW = false;
            mmi.MaterialID = lod.MatId2;
            mmi.MeshID = lod.MeshId2;
            mmi.MaterialID = new BatchMaterialID { value = uint.MaxValue };

            // THIS BROKE AFTER UNITY 6
            //mmi.MaterialID = new BatchMaterialID { value = 888 }; // invalid number hides the material
            //mmi.MeshID = new BatchMeshID { value = 888 }; // invalid number hides the mesh
        }
    }
} // namespace


// DISTANCE MODE
//[BurstCompile]
//public void Execute(ref MaterialMeshInfo mmi, in SimpleLodData lod, in LocalToWorld ltw, in LodDistanceData lodDist)
//{
//    float actualDistSq = math.distancesq(Cam.Pos, ltw.Position);
//    if (actualDistSq < lodDist.Dist0Sq) {
//        mmi.MaterialID = lod.MatId0;
//        mmi.MeshID = lod.MeshId0;
//        return;
//    }
//    if (actualDistSq < lodDist.Dist1Sq) {
//        mmi.MaterialID = lod.MatId1;
//        mmi.MeshID = lod.MeshId1;
//        return;
//    }
//    if (actualDistSq < lodDist.Dist2Sq) {
//        mmi.MaterialID = lod.MatId2;
//        mmi.MeshID = lod.MeshId2;
//        return;
//    }

//    mmi.MaterialID = new BatchMaterialID { value = 888 }; // invalid number hides the material
//    mmi.MeshID = new BatchMeshID { value = 888 }; // invalid number hides the mesh
//}