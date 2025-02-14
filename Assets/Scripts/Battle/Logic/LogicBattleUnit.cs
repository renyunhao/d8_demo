using FixPointUnity;
using System.Collections.Generic;

public class LogicBattleUnit
{
    /// <summary>
    /// 进攻方单位起始索引
    /// </summary>
    public const int Attacker_Index_Offset = 1000000;
    public const int Defender_Index_Offset = 1;
    /// <summary>
    /// 战斗中判断距离的宽松值 ，只要两者间的距离小于该值，就认为已经到达目标
    /// </summary>
    private static F64 DISTANCE_TOLRANCE = F64.FromDouble(0.1);

    public static int AttackerIndex = Attacker_Index_Offset;
    public static int DefenderIndex = Defender_Index_Offset;
    public static int BeAttackedIndex_Defender = 1;

    public int index;
    public int id;
    public string name;
    public LogicBattleUnitStaticData staticData = new LogicBattleUnitStaticData();
    public LogicBattleUnitRuntimeData runtimeData = new LogicBattleUnitRuntimeData();

    /// <summary>
    /// 是不是进攻方
    /// </summary>
    public bool IsAttacker => index >= Attacker_Index_Offset;
    /// <summary>
    /// 是不是防守方
    /// </summary>
    public bool IsDefender => index < Attacker_Index_Offset;
    /// <summary>
    /// 是不是远程单位
    /// </summary>
    public bool IsLongRangeUnit => staticData.BulletId > 0;

    /// <summary>
    /// 执行移动操作
    /// </summary>
    /// <param name="attackRelate"></param>
    public void Move(F64 speedScale)
    {
        F64Vec3 targetPos;
        LogicOnceAttackRelate attackRelate = LogicBattleSystem.Instance.GetOnceAttackRelateByAttacker(this);
        if (attackRelate != null && attackRelate.mainTarget != null)
        {
            targetPos = attackRelate.mainTarget.runtimeData.pos;
        }
        else
        {
            targetPos = runtimeData.moveToPos;
        }
        UnitMove(targetPos, speedScale);
    }

    /// <summary>
    /// 单位移动
    /// </summary>
    private void UnitMove(F64Vec3 targetPos, F64 speedScale)
    {
        F64Vec3 direction = targetPos - this.runtimeData.pos;
        F64Vec3 directionNormalized = F64Vec3.NormalizeFastest(direction);
        F64 distance = this.staticData.moveSpeed * LogicBattleSystem.COMPUTE_DELTA_SECOND * this.runtimeData.moveSpeedCoeff * speedScale;
        this.runtimeData.movedTotalTime += LogicBattleSystem.COMPUTE_DELTA_SECOND * this.runtimeData.moveSpeedCoeff * speedScale;
        if (F64Vec3.Length(direction) <= distance)
        {
            this.runtimeData.pos = targetPos;
        }
        else
        {
            F64Vec3 offset = directionNormalized * distance;
            this.runtimeData.pos += offset;
        }
    }

    public void SkillTakeEffect_Freeze(int skillId)
    {
        if (runtimeData.skillFreezeEffects == null)
        {
            runtimeData.skillFreezeEffects = new HashSet<int>();
        }
        runtimeData.skillFreezeEffects.Add(skillId);
        runtimeData.freezed = true;
    }

    public void SkillFinish_Freeze(int skillId)
    {
        runtimeData.skillFreezeEffects.Remove(skillId);
        if (runtimeData.skillFreezeEffects.Count <= 0)
        {
            runtimeData.freezed = false;
        }
    }

    public void Clear()
    {
        index = 0;
        id = 0;
        name = string.Empty;

        staticData.Clear();
        runtimeData.Clear();
    }

    public int GetSkillAttackPower(LogicBattleUnit target)
    {
        int result = 0;
        int defaultValue = 0;
        if (staticData.skillPower != null)
        {
            for (int i = 0; i < staticData.skillPower.Length; i += 2)
            {
                if (staticData.skillPower[i] == 0)
                {
                    defaultValue = staticData.skillPower[i + 1];
                }
                if ((target.staticData.battleUnitType & (BattleUnitType)staticData.skillPower[i]) == target.staticData.battleUnitType)
                {
                    result = staticData.skillPower[i + 1];
                    break;
                }
            }
        }
        return result == 0 ? defaultValue : result;
    }

    public int GetGeneralAttackPower(LogicBattleUnit target)
    {
        int result = 0;
        int defaultValue = 0;
        if (staticData.generalPower != null)
        {
            for (int i = 0; i < staticData.generalPower.Length; i += 2)
            {
                if (staticData.generalPower[i] == 0)
                {
                    defaultValue = staticData.generalPower[i + 1];
                }
                if ((target.staticData.battleUnitType & (BattleUnitType)staticData.generalPower[i]) == target.staticData.battleUnitType)
                {
                    result = staticData.generalPower[i + 1];
                    break;
                }
            }
        }
        return result == 0 ? defaultValue : result;
    }

    /// <summary>
    /// 是否到达移动目的地
    /// </summary>
    /// <returns></returns>
    public bool IsArriveEnemyBasecamp()
    {
        return F64Vec3.DistanceFastest(runtimeData.pos, runtimeData.moveToPos) <= DISTANCE_TOLRANCE;
    }
}