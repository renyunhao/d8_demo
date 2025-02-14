using FixPointUnity;
using System.Collections.Generic;
using UnityEngine;

public class LogicBattleUnitRuntimeData
{
    public BattleUnitState currentStatus;
    public BattleUnitState delaySwitchStatus;
    public int hp;
    public F64Vec3 pos;
    /// <summary>
    /// 一次攻击间隔，已攻击次数
    /// </summary>
    public int attackedNumber;
    /// <summary>
    /// 攻击状态计时
    /// </summary>
    public long attackTimer;
    /// <summary>
    /// 攻击间隔计时
    /// </summary>
    public long attackIntervalTimer;
    public long attackWaitTimer;
    public long warningTimer;
    public int delaySwitchFrameCounter;
    public int delaySwitchFrame;
    public int attackPower;
    public bool isKightCharging;
    /// <summary>
    /// 移动速度加成系数
    /// </summary>
    public F64 moveSpeedCoeff = F64.One;
    /// <summary>
    /// 攻击速度加成系数
    /// </summary>
    public F64 attackSpeedCoeff = F64.One;
    /// <summary>
    /// 最大生命值加成系数
    /// </summary>
    public F64 maxHPCoeff = F64.One;
    /// <summary>
    /// 攻击力加成系数
    /// </summary>
    public F64 attackPowerCoeff = F64.One;
    /// <summary>
    /// 从进入移动状态后经过的时间，每次离开移动状态清0，当移动速度变化时，时间累计也相应的变化
    /// 即当移动速度翻倍时，每次累加的时间也翻倍，当移动速度减半时，每次累加的时间也减半
    /// </summary>
    public F64 movedTotalTime;
    /// <summary>
    /// 施放技能时间
    /// </summary>
    public long performSkillTime;
    /// <summary>
    /// 施放技能时间计时器
    /// </summary>
    public long performSkillTimer;
    /// <summary>
    /// 技能信息容器，记录所有释放过的技能
    /// </summary>
    public Dictionary<int, SkillData> skills = new Dictionary<int, SkillData>();
    /// <summary>
    /// 当前单位是否正在释放技能（有些技能释放过程需要单位一直维持施法状态）
    /// </summary>
    public bool releasingSkill;
    /// <summary>
    /// 造成当前单位被冰冻的技能列表
    /// </summary>
    public HashSet<int> skillFreezeEffects;
    /// <summary>
    /// 是否被冰冻
    /// </summary>
    public bool freezed;
    /// <summary>
    /// 移动目标点，用于Move状态的移动计算
    /// </summary>
    public F64Vec3 moveToPos;

    public void Clear()
    {
        currentStatus = BattleUnitState.Idle;
        hp = 0;
        pos = F64Vec3.Zero;
        moveSpeedCoeff = F64.One;
        attackSpeedCoeff = F64.One;
        maxHPCoeff = F64.One;
        attackPowerCoeff = F64.One;
        movedTotalTime = F64.Zero;

        attackedNumber = 0;
        attackTimer = 0;
        attackWaitTimer = 0;
        warningTimer = 0;
        attackPower = 0;
        releasingSkill = false;

        skillFreezeEffects = null;
        freezed = false;
        isKightCharging = false;
        performSkillTime = 0;
        performSkillTimer = 0;
    }

    public bool IsSkillReleased(int skillId)
    {
        return skills.ContainsKey(skillId);
    }

    public void AddReleasedSkill(int skillId)
    {
        if (skills.TryGetValue(skillId, out var skillInfo) == false)
        {
            skills.Add(skillId, new SkillData());
        }
    }

    public bool IsSkillAlive(int skillId)
    {
        if (skills.TryGetValue(skillId, out var skillInfo))
        {
            return skillInfo.isAlive;
        }
        return false;
    }

    public void AddAliveSkill(int skillId)
    {
        if (skills.TryGetValue(skillId, out var skillInfo))
        {
            skillInfo.isAlive = true;
        }
    }

    public void RemoveAliveSkill(int skillId)
    {
        if (skills.TryGetValue(skillId, out var skillInfo))
        {
            skillInfo.isAlive = false;
        }
    }
}
