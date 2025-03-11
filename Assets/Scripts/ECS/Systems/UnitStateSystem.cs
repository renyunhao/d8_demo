/*
using FixPointUnity;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;

[UpdateInGroup(typeof(LogicSystemGroup))]
public partial struct UnitStateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var hasTargetJobHandle = new UnitHasTargetStateJob()
        {
            unitStatusLookup = SystemAPI.GetComponentLookup<UnitStatus>(),
            unitHPLookup = SystemAPI.GetComponentLookup<HP>(false),
            ecb = ecb.AsParallelWriter()
        }.ScheduleParallel(state.Dependency);

        //找出所有无目标且没有需要目标的单位
        var noTargetJobHandle = new UnitNoTargetButNeedTargetStateJob()
        {
            ecb = ecb.AsParallelWriter()
        }.ScheduleParallel(hasTargetJobHandle);

        //找出所有已经搜索过目标，但是仍然无目标的单位
        //var noTargetButSearchedJobHandle = new UnitNoTargetButAlreadySearchedJob()
        //    .ScheduleParallel(noTargetJobHandle);

        noTargetJobHandle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static void SwitchState(UnitDataAspect unitData, BattleUnitState newState)
    {
        //Debug.Log($"Entity {unitData.entity.Index} change state {unitData.CurrentState} to {newState}");
        unitData.CurrentState = newState;
    }

    private static void SwitchState(UnitStatus unitStatus, BattleUnitState newState)
    {
        //Debug.Log($"Entity {unitStatus.entity.Index} change state {unitStatus.value} to {newState}");
        unitStatus.value = newState;
    }

    [BurstCompile]
    [WithNone(typeof(UnitDeadTag))]
    private partial struct UnitHasTargetStateJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<UnitStatus> unitStatusLookup;
        public ComponentLookup<HP> unitHPLookup;
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute([ChunkIndexInQuery] int index, Entity entity, [ReadOnly] UnitDataAspect unitData, [ReadOnly] HasTargetComponentData hasTarget)
        {
            switch (unitData.CurrentState)
            {
                case BattleUnitState.Idle:
                    break;
                case BattleUnitState.MoveToBasecamp:
                    SwitchState(unitData, BattleUnitState.Attacking);
                    break;
                case BattleUnitState.Attacking:
                    {
                        unitData.AttackTimer += LogicBattleSystem.COMPUTE_DELTA_SECOND;
                        if (unitData.AttackTimer >= unitData.AttackTime)
                        {
                            unitData.IsAttackPerformed = false;
                            unitData.AttackTimer = F64.Zero;
                            SwitchState(unitData, BattleUnitState.AttackWait);
                        }
                        else
                        {
                            if (unitData.AttackTimer >= unitData.AttackPreTime && unitData.IsAttackPerformed == false)
                            {
                                unitData.IsAttackPerformed = true;
                                //若所有目标死亡，切换状态为Idle
                                bool allTargetDead = true;
                                foreach (var target in hasTarget.targets)
                                {
                                    if (unitStatusLookup.HasComponent(target))
                                    {
                                        var targetStatus = unitStatusLookup[target];
                                        if (targetStatus.value != BattleUnitState.Dead)
                                        {
                                            allTargetDead = false;
                                            break;
                                        }
                                    }
                                }
                                if (allTargetDead)
                                {
                                    unitData.AttackTimer = F64.Zero;
                                    Debug.Log($"entity {entity.Index} AttackTimer to 0 {unitData.AttackTimer} cause by allTargetDead");
                                    SwitchState(unitData, BattleUnitState.MoveToBasecamp);
                                    ecb.RemoveComponent<HasTargetComponentData>(index, entity);
                                }
                                else
                                {
                                    //造成伤害
                                    foreach (var target in hasTarget.targets)
                                    {
                                        if (unitStatusLookup.HasComponent(target))
                                        {
                                            var targetHP = unitHPLookup[target];
                                            targetHP.value = targetHP.value - unitData.AttackPower;
                                            var targetStatus = unitStatusLookup[target];
                                            //Debug.Log($"DoDamage {attackerData.entity.Index} to {target.Index} damage {attackerData.AttackPower} left hp {targetStatus.HP}");
                                            if (targetHP.value <= 0)
                                            {
                                                targetHP.value = 0;
                                                ecb.AddComponent(index, target, new UnitDeadTag());
                                                SwitchState(targetStatus, BattleUnitState.Dead);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                case BattleUnitState.AttackWait:
                    {
                        unitData.AttackTimer += LogicBattleSystem.COMPUTE_DELTA_SECOND;
                        if (unitData.AttackTimer >= unitData.AttackWaitTime)
                        {
                            unitData.AttackTimer = F64.Zero;
                            //若所有目标死亡，切换状态为Idle, 同时移除HasTargetComponentData
                            bool allTargetDead = true;
                            foreach (var target in hasTarget.targets)
                            {
                                if (unitStatusLookup.HasComponent(target))
                                {
                                    var targetStatus = unitStatusLookup[target];
                                    if (targetStatus.value != BattleUnitState.Dead)
                                    {
                                        allTargetDead = false;
                                        break;
                                    }
                                }
                            }
                            if (allTargetDead)
                            {
                                SwitchState(unitData, BattleUnitState.MoveToBasecamp);
                                ecb.RemoveComponent<HasTargetComponentData>(index, entity);
                            }
                            else
                            {
                                SwitchState(unitData, BattleUnitState.Attacking);
                            }
                        }
                    }
                    break;
            }
        }
    }

    [BurstCompile]
    [WithNone(typeof(UnitDeadTag), typeof(HasTargetComponentData), typeof(NeedFindTargetTag))]
    private partial struct UnitNoTargetButNeedTargetStateJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        public void Execute([ChunkIndexInQuery] int index, Entity entity, [ReadOnly] UnitDataAspect unitData)
        {
            switch (unitData.CurrentState)
            {
                case BattleUnitState.MoveToBasecamp:
                    //空闲与向敌方大本营移动的单位可以进行目标搜索
                    ecb.AddComponent(index, entity, new NeedFindTargetTag() { findTargetType = FindTargetType.AttackSingle });
                    break;
            }
        }
    }

    [BurstCompile]
    [WithNone(typeof(UnitDeadTag), typeof(HasTargetComponentData))]
    [WithAll(typeof(NeedFindTargetTag))]
    private partial struct UnitNoTargetButAlreadySearchedJob : IJobEntity
    {
        public void Execute(Entity entity, [ReadOnly] UnitDataAspect unitData)
        {
            switch (unitData.CurrentState)
            {
                case BattleUnitState.MoveToBasecamp:
                    {
                        F64Vec3 direction;
                        if (unitData.UnitCamp == UnitCamp.Attacker)
                        {
                            direction = F64Vec3.Right;
                        }
                        else
                        {
                            direction = F64Vec3.Left;
                        }
                        F64 distance = unitData.MoveSpeed * LogicBattleSystem.COMPUTE_DELTA_SECOND;
                        F64Vec3 offset = direction * distance;
                        unitData.Position += offset;
                    }
                    break;
            }
        }
    }
}
*/