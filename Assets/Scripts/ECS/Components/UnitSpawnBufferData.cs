using Unity.Entities;

public struct UnitSpawnBufferData : IBufferElementData
{
    public bool isAttacker;
    public int id;
    public int count;
}
