using FixPointUnity;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System;

[UpdateInGroup(typeof(LogicSystemGroup))]
public partial struct UnitStateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        //找出所有有目标的单位
        foreach (var (unitData, hasTarget, entity) in SystemAPI.Query<UnitDataAspect, RefRO<HasTargetComponentData>>()
            .WithNone<UnitDeadTag>()
            .WithEntityAccess())
        {
            switch (unitData.CurrentState)
            {
                case BattleUnitState.Idle:
                    break;
                case BattleUnitState.MoveToBasecamp:
                    SwitchState(unitData, BattleUnitState.Attacking);
                    break;
                case BattleUnitState.Attacking:
                    AttackingUpdate(ref state, entity, unitData, hasTarget.ValueRO, ecb);
                    break;
                case BattleUnitState.AttackWait:
                    AttackWaitUpdate(ref state, entity, unitData, hasTarget.ValueRO, ecb);
                    break;
            }
        }

        //找出所有无目标, 但需要目标的单位
        foreach (var (unitData, entity) in SystemAPI.Query<UnitDataAspect>()
            .WithNone<HasTargetComponentData>()
            .WithNone<NeedFindTargetTag>()
            .WithNone<UnitDeadTag>()
            .WithEntityAccess())
        {
            switch (unitData.CurrentState)
            {
                case BattleUnitState.Idle:
                case BattleUnitState.MoveToBasecamp:
                    //空闲与向敌方大本营移动的单位可以进行目标搜索
                    ecb.AddComponent(entity, new NeedFindTargetTag() { findTargetType = FindTargetType.AttackSingle });
                    break;
            }
        }

        //找出所有已经搜索过目标，但是仍然无目标的单位
        foreach (var (unitData, entity) in SystemAPI.Query<UnitDataAspect>()
            .WithNone<HasTargetComponentData>()
            .WithAll<NeedFindTargetTag>()
            .WithNone<UnitDeadTag>()
            .WithEntityAccess())
        {
            switch (unitData.CurrentState)
            {
                case BattleUnitState.Idle:
                    SwitchState(unitData, BattleUnitState.MoveToBasecamp);
                    break;
                case BattleUnitState.MoveToBasecamp:
                    MoveToBasecampUpdate(entity, unitData, ecb);
                    break;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void SwitchState(UnitDataAspect unitData, BattleUnitState newState)
    {
        if (newState != unitData.CurrentState)
        {
            //TODO: Old StateExit
            //switch (unitData.CurrentState)
            //{
            //}
            unitData.CurrentState = newState;
            //TODO: New StateEnter
            switch (unitData.CurrentState)
            {
                case BattleUnitState.MoveToBasecamp:
                    MoveToBasecampEnter(unitData);
                    break;
                case BattleUnitState.Attacking:
                    AttackingEnter(unitData);
                    break;
                case BattleUnitState.AttackWait:
                    AttackWaitEnter(unitData);
                    break;
            }
        }
    }

    private void AttackingEnter(UnitDataAspect unitData)
    {
        unitData.AnimationClip = CrabMonsterPBRDefault.Attack;
    }

    private void AttackingUpdate(ref SystemState state, Entity entity, UnitDataAspect unitData, HasTargetComponentData hasTarget, EntityCommandBuffer ecb)
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
                    if (state.EntityManager.Exists(target))
                    {
                        var targetData = state.EntityManager.GetAspect<UnitDataAspect>(target);
                        if (targetData.CurrentState != BattleUnitState.Dead)
                        {
                            allTargetDead = false;
                            break;
                        }
                    }
                }
                if (allTargetDead)
                {
                    unitData.AttackTimer = F64.Zero;
                    //Debug.Log($"entity {entity.Index} AttackTimer to 0 {unitData.AttackTimer} cause by allTargetDead");
                    SwitchState(unitData, BattleUnitState.MoveToBasecamp);
                    ecb.RemoveComponent<HasTargetComponentData>(entity);
                }
                else
                {
                    //造成伤害
                    foreach (var target in hasTarget.targets)
                    {
                        var targetData = state.EntityManager.GetAspect<UnitDataAspect>(target);
                        DoDamage(target, targetData, unitData, ecb);
                    }
                }
            }
        }
    }

    public void DoDamage(Entity target, UnitDataAspect targetData, UnitDataAspect attackerData, EntityCommandBuffer ecb)
    {
        targetData.HP = targetData.HP - attackerData.AttackPower;
        //Debug.Log($"DoDamage {attackerData.entity.Index} to {target.Index} damage {attackerData.AttackPower} left hp {targetData.HP}");
        if (targetData.HP <= 0)
        {
            targetData.HP = 0;
            ecb.AddComponent(target, new UnitDeadTag());
            SwitchState(targetData, BattleUnitState.Dead);
        }
    }

    private void AttackWaitEnter(UnitDataAspect unitData)
    {
        unitData.AnimationClip = CrabMonsterPBRDefault.Idle;
    }

    private void AttackWaitUpdate(ref SystemState state, Entity entity, UnitDataAspect unitData, HasTargetComponentData hasTarget, EntityCommandBuffer ecb)
    {
        unitData.AttackTimer += LogicBattleSystem.COMPUTE_DELTA_SECOND;
        if (unitData.AttackTimer >= unitData.AttackWaitTime)
        {
            unitData.AttackTimer = F64.Zero;
            //若所有目标死亡，切换状态为Idle, 同时移除HasTargetComponentData
            bool allTargetDead = true;
            foreach (var target in hasTarget.targets)
            {
                if (state.EntityManager.Exists(target))
                {
                    var targetData = state.EntityManager.GetAspect<UnitDataAspect>(target);
                    if (targetData.CurrentState != BattleUnitState.Dead)
                    {
                        allTargetDead = false;
                        break;
                    }
                }
            }
            if (allTargetDead)
            {
                SwitchState(unitData, BattleUnitState.MoveToBasecamp);
                ecb.RemoveComponent<HasTargetComponentData>(entity);
            }
            else
            {
                SwitchState(unitData, BattleUnitState.Attacking);
            }
        }
    }

    private void MoveToBasecampEnter(UnitDataAspect unitData)
    {
        var targetPos = unitData.Position;
        unitData.AnimationClip = CrabMonsterPBRDefault.Move;
        if (unitData.UnitCamp == UnitCamp.Attacker)
        {
            unitData.Rotation = quaternion.LookRotation(new float3(1, 0, 0), new float3(0, 1, 0));
        }
        else
        {
            unitData.Rotation = quaternion.LookRotation(new float3(-1, 0, 0), new float3(0, 1, 0));
        }
    }

    private void MoveToBasecampUpdate(Entity entity, UnitDataAspect unitData, EntityCommandBuffer ecb)
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
}
