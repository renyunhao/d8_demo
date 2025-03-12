using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(LogicSystemGroup))]
public partial struct UnitSyncSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (_, entity) in SystemAPI.Query<UnitDeadTag>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
