using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(LogicSystemGroup))]
public partial struct UnitSyncSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (unitData, battleUnitRef) in SystemAPI.Query<UnitDataAspect, BattleUnitReference>())
        {
            battleUnitRef.battleUnit.transform.position = unitData.Position.ToVector3();
            if (battleUnitRef.battleUnit.currentStatus != unitData.CurrentState)
            {
                BattleSystem.SwitchStatus(battleUnitRef.battleUnit, unitData.CurrentState);
                if (unitData.CurrentState == BattleUnitState.Attacking)
                {
                    var hasTarget = state.EntityManager.GetComponentData<HasTargetComponentData>(unitData.entity);
                    if (state.EntityManager.Exists(hasTarget.targets[0]))
                    {
                        var targetPos = state.EntityManager.GetAspect<UnitDataAspect>(hasTarget.targets[0]).Position.ToVector3();
                        battleUnitRef.battleUnit.UpdateDirection(targetPos);
                    }
                }
            }
        }

        foreach (var (_, battleUnitRef, entity) in SystemAPI.Query<UnitDeadTag, BattleUnitReference>().WithEntityAccess())
        {
            BattleSystem.BattleField.RemoveBattleUnit(battleUnitRef.battleUnit);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
