using AnimCooker;
using FixPointUnity;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 生命值
/// </summary>
public struct HP : IComponentData
{
    public int value;
}

/// <summary>
/// 坐标
/// </summary>
public struct Position : IComponentData
{
    public F64Vec3 value;
}

/// <summary>
/// 当前状态
/// </summary>
public struct UnitStatus : IComponentData
{
    public BattleUnitState value;
}

/// <summary>
/// 攻击相关计时器，攻击前摇，攻击完成，攻击等待都使用这个计时器来判断
/// </summary>
public struct AttackTimer : IComponentData
{
    public F64 value;
}

/// <summary>
/// 攻击力
/// </summary>
public struct AttackPower : IComponentData
{
    public int value;
}

/// <summary>
/// 本次攻击是否实施
/// </summary>
public struct AttackPerformed : IComponentData
{
    public bool value;
}

public struct UnitStaticData : IComponentData
{
    /// <summary>
    /// 单位ID
    /// </summary>
    public int id;
    /// <summary>
    /// 所属阵营
    /// </summary>
    public UnitCamp unitCamp;
    /// <summary>
    /// 体积半径
    /// </summary>
    public F64 volumeRadius;
    /// <summary>
    /// 攻击范围半径
    /// </summary>
    public F64 attackRadius;
    /// <summary>
    /// 攻击动作时长
    /// </summary>
    public F64 attackTime;
    /// <summary>
    /// 攻击动作前摇时长
    /// </summary>
    public F64 attackPreTime;
    /// <summary>
    /// 攻击等待时长
    /// </summary>
    public F64 attackWaitTime;
    /// <summary>
    /// 移动速度
    /// </summary>
    public F64 moveSpeed;
}

public readonly partial struct UnitDataAspect : IAspect
{
    public readonly Entity entity;

    private readonly RefRW<LocalTransform> localTransform;
    private readonly RefRW<HP> hp;
    private readonly RefRW<Position> position;
    private readonly RefRW<UnitStatus> currentState;
    private readonly RefRW<AttackTimer> attackTimer;
    private readonly RefRW<AttackPower> attackPower;
    private readonly RefRW<AttackPerformed> attackPerformed;
    private readonly RefRW<AnimationCmdData> animationCmdData;

    private readonly RefRO<UnitStaticData> staticData;


    public int HP
    {
        get => hp.ValueRO.value;
        set => hp.ValueRW.value = value;
    }

    public F64Vec3 Position
    {
        get => position.ValueRO.value;
        set {
            position.ValueRW.value = value;
            localTransform.ValueRW.Position = value.ToVector3();
        }
    }

    public quaternion Rotation
    {
        get => localTransform.ValueRO.Rotation;
        set => localTransform.ValueRW.Rotation = value;
    }

    public BattleUnitState CurrentState
    {
        get => currentState.ValueRO.value;
        set => currentState.ValueRW.value = value;
    }

    public F64 AttackTimer
    {
        get => attackTimer.ValueRO.value;
        set => attackTimer.ValueRW.value = value;
    }

    public int AttackPower
    {
        get => attackPower.ValueRO.value;
        set => attackPower.ValueRW.value = value;
    }

    public bool IsAttackPerformed
    {
        get => attackPerformed.ValueRO.value;
        set => attackPerformed.ValueRW.value = value;
    }

    public CrabMonsterPBRDefault AnimationClip
    {
        get => (CrabMonsterPBRDefault)animationCmdData.ValueRO.ClipIndex;
        set
        {
            if (value == CrabMonsterPBRDefault.Move || value == CrabMonsterPBRDefault.Idle)
            {
                animationCmdData.ValueRW.Cmd = AnimationCmd.SetPlayForever;
            }
            else
            {
                animationCmdData.ValueRW.Cmd = AnimationCmd.PlayOnce;
            }
            animationCmdData.ValueRW.ClipIndex = (short)value;
        }
    }

    public F64 AttackTime => staticData.ValueRO.attackTime;

    public F64 AttackPreTime => staticData.ValueRO.attackPreTime;

    public F64 AttackWaitTime => staticData.ValueRO.attackWaitTime;

    public UnitCamp UnitCamp => staticData.ValueRO.unitCamp;

    public F64 MoveSpeed => staticData.ValueRO.moveSpeed;

    public F64 VolumeRadius => staticData.ValueRO.volumeRadius;

    public F64 AttackRadius => staticData.ValueRO.attackRadius;
}

public enum UnitCamp
{
    Attacker,
    Defender
}

public class BattleUnitReference : IComponentData
{
    public BattleUnit battleUnit;
}

public struct UnitDeadTag : IComponentData { }