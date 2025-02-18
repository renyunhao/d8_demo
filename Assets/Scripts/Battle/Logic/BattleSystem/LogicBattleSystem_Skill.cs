using FixPointUnity;
using System.Collections.Generic;
using UnityEngine;

public partial class LogicBattleSystem
{
    private readonly List<LogicSkillBase> logicSkillList = new List<LogicSkillBase>();

    private readonly Dictionary<SkillEffect, Queue<LogicSkillBase>> skillPoolDict = new Dictionary<SkillEffect, Queue<LogicSkillBase>>(20);

    private void SkillCompute()
    {
        for (int i = 0; i < logicSkillList.Count; i++)
        {
            logicSkillList[i].CustomUpdate(COMPUTE_DELTA_MILLISECOND);
        }
    }

    /// <summary>
    /// 记录本帧释放的技能
    /// </summary>
    /// <param name="skill"></param>
    public void RecordSkill(LogicSkillBase skill)
    {
        if (currentFrameOutputData == null)
        {
            currentFrameOutputData = outputDataFramePool.GetInstance();
        }
        currentFrameOutputData.addSkills.Add(skill);
    }

    /// <summary>
    /// 尝试触发技能
    /// </summary>
    /// <param name="trigger">技能释放者</param>
    /// <param name="skillTriggerTiming">技能触发时机</param>
    public void TryTriggerSkill(LogicBattleUnit trigger, SkillTriggerTiming skillTriggerTiming)
    {
        if (trigger.staticData.skillList != null)
        {
            foreach (var skillId in trigger.staticData.skillList)
            {
                SkillTableData skillData = null;// TableDataMgr.GetSingleSkillTableData(skillId);
                //触发时机
                if (skillData.triggerTiming == (int)skillTriggerTiming)
                {
                    //技能不可重复触发，技能已释放过，此种情况技能不触发
                    if (skillData.retriggerable == false &&
                        trigger.runtimeData.IsSkillReleased(skillId))
                    {
                        continue;
                    }

                    //技能可重复触发，但不能覆盖，且技能生效中，此种情况技能不触发
                    if (skillData.retriggerable &&
                        skillData.retriggerableReplace == false &&
                        trigger.runtimeData.IsSkillAlive(skillId))
                    {
                        continue;
                    }

                    //技能如果有cd，且cd时间未到，不能触发
                    if (trigger.runtimeData.skills.TryGetValue(skillId, out var skillInfo))
                    {
                        if (skillInfo.cdTimer < (long)(skillData.cd * SECOND_TO_MILLISECOND))
                        {
                            continue;
                        }
                    }

                    if (skillTriggerTiming == SkillTriggerTiming.Moving)
                    {
                        //判断移动时间是否足够
                        F64 requiredTime = F64.FromDouble(skillData.triggerTimingValue);
                        if (trigger.runtimeData.movedTotalTime >= requiredTime)
                        {
                            ReleaseSkill(skillData, trigger);
                        }
                    }
                    else
                    {
                        ReleaseSkill(skillData, trigger);
                    }
                }
            }

            //这里只检查那些放出来，但是还处于Pending状态的技能，看是否满足它的触发条件
            foreach (var skill in logicSkillList)
            {
                if (skill.releaser != trigger || skill.progress != SkillProgress.Pending)
                {
                    continue;
                }
                
                if (skill.tableData.triggerTiming == (int)skillTriggerTiming)
                {
                    if (skillTriggerTiming == SkillTriggerTiming.DistanceBelow)
                    {
                        //这个触发时机还要检查距离是否满足
                        LogicOnceAttackRelate attackRelate = GetOnceAttackRelateByAttacker(trigger);
                        if (attackRelate != null && attackRelate.mainTarget.runtimeData.currentStatus != BattleUnitState.Dead)
                        {
                            var distance = F64Vec3.Distance(attackRelate.mainTarget.runtimeData.pos, trigger.runtimeData.pos);
                            if (distance <= F64.FromDouble(skill.tableData.effectValue[0]))
                            {
                                skill.SkillStart(true);
                                RecordSkill(skill);
                            }
                        }
                    }
                    else
                    {
                        skill.SkillStart(true);
                        RecordSkill(skill);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 尝试结束技能
    /// </summary>
    /// <param name=""></param>
    public void TryEndSkill(LogicBattleUnit trigger, SkillEndTiming endTiming)
    {
        foreach (var logicSkill in logicSkillList)
        {
            if (logicSkill.isAlive &&
                logicSkill.tableData.endTiming == (int)endTiming &&
                trigger == logicSkill.releaser)
            {
                logicSkill.EndSkill();
            }
        }
    }

    /// <summary>
    /// 尝试释放子弹命中时触发的技能
    /// </summary>
    /// <param name="trigger">技能释放者</param>
    /// <param name="bullet">子弹对象</param>
    public void TryTriggerSkill_AttackHit(LogicBattleUnit trigger, LogicBulletBase bullet)
    {
        if (trigger.staticData.skillList != null)
        {
            foreach (var skillId in trigger.staticData.skillList)
            {
                SkillTableData skillData = null;// TableDataMgr.GetSingleSkillTableData(skillId);
                if (skillData.triggerTiming == (int)SkillTriggerTiming.AttackHit)
                {
                    //技能不可重复触发，技能已释放过，此种情况技能不触发
                    if (skillData.retriggerable == false &&
                        trigger.runtimeData.IsSkillReleased(skillId))
                    {
                        continue;
                    }

                    //技能可重复触发，但不能覆盖，且技能生效中，此种情况技能不触发
                    if (skillData.retriggerable &&
                        skillData.retriggerableReplace == false &&
                        trigger.runtimeData.IsSkillAlive(skillId))
                    {
                        continue;
                    }
                    ReleaseSkill(skillData, trigger, bullet);
                }
            }
        }
    }

    public LogicSkillBase ReleasePendingSkill(SkillTableData skillData, LogicBattleUnit trigger)
    {
        return ReleaseSkill(skillData, trigger, null, true);
    }

    /// <summary>
    /// 释放技能，技能的部分数据要使用子弹的数据来初始化
    /// </summary>
    /// <param name="skillData"></param>
    public LogicSkillBase ReleaseSkill(SkillTableData skillData, LogicBattleUnit trigger, LogicBulletBase bullet = null, bool initSkillPending = false)
    {
        trigger.runtimeData.releasingSkill = true;
        LogicSkillBase logicSkill = GetSkillInstance((SkillEffect)skillData.effect);
        logicSkill.tableData = skillData;
        logicSkill.releaser = trigger;
        logicSkill.bullet = bullet;
        if (bullet == null)
        {
            logicSkill.targetIndex = trigger.index;
            logicSkill.pos = trigger.runtimeData.pos;
        }
        else
        {
            logicSkill.targetIndex = bullet.targetIndex;
            logicSkill.pos = bullet.targetPosition;
        }

        //技能可重复触发且覆盖，要找出已经存在的同类技能，并移除
        if (skillData.retriggerable && skillData.retriggerableReplace)
        {
            foreach (var target in logicSkill.targetList)
            {
                RemoveAllSkill(target, logicSkill.GetType());
            }
        }

        logicSkill.Initialize(initSkillPending);
        logicSkillList.Add(logicSkill);
        if (initSkillPending == false)
        {
            RecordSkill(logicSkill);
        }
        return logicSkill;
    }

    public void FillSkillTargetList(LogicBattleUnit trigger, LogicSkillBase logicSkill, LogicBulletBase bullet = null)
    {
        logicSkill.targetList.Clear();
        switch ((SkillTarget)logicSkill.tableData.targetSelect)
        {
            case SkillTarget.AllEnemy:
                logicSkill.targetList.UnionWith(GetEnemyUnitWithinRange(logicSkill.pos, logicSkill.skillRange, F64.Zero, logicSkill.tableData.targetType, trigger.IsAttacker));
                break;
            case SkillTarget.AllAlly:
                logicSkill.targetList.UnionWith(GetAllyUnitWithinRange(logicSkill.pos, logicSkill.skillRange, logicSkill.tableData.targetType, trigger.IsAttacker));
                break;
            case SkillTarget.Self:
                logicSkill.targetList.Add(trigger);
                break;
            case SkillTarget.AttackTarget:
                if (bullet != null)
                {
                    logicSkill.targetList.Add(GetLogicBattleUnit(bullet.targetIndex));
                }
                else
                {
                    LogicOnceAttackRelate attackRelate = GetOnceAttackRelateByAttacker(trigger);
                    if (attackRelate != null && attackRelate.mainTarget.runtimeData.currentStatus != BattleUnitState.Dead)
                    {
                        logicSkill.targetList.Add(attackRelate.mainTarget);
                        logicSkill.targetList.UnionWith(attackRelate.secondaryTarget);

                        if (logicSkill.tableData.rangeCenter == (int)SkillRangeCenter.Target)
                        {
                            var extraTargets = GetEnemyUnitWithinRange(attackRelate.mainTarget.runtimeData.pos, logicSkill.skillRange, F64.Zero, logicSkill.tableData.targetType, logicSkill.releaser.IsAttacker);
                            logicSkill.targetList.UnionWith(extraTargets);
                        }
                    }

                    if (logicSkill.tableData.rangeCenter == (int)SkillRangeCenter.Releaser)
                    {
                        var extraTargets = GetEnemyUnitWithinRange(logicSkill.releaser.runtimeData.pos, logicSkill.skillRange, F64.Zero, logicSkill.tableData.targetType, logicSkill.releaser.IsAttacker);
                        logicSkill.targetList.UnionWith(extraTargets);
                    }
                }
                break;
        }
    }

    private bool HasUnFinishedSkillEffect()
    {
        foreach (var item in logicSkillList)
        {
            //瞬间或有持续时间的技能会阻止战斗结算，不定时长（duration为负数）的技能不会
            if (item.isAlive && item.duration >= 0)
            {
                return true;
            }
        }
        return false;
    }

    private LogicSkillBase GetSkillInstance(SkillEffect skillEffect)
    {
        var skillPool = skillPoolDict[skillEffect];
        if (skillPool.Count > 0)
        {
            return skillPool.Dequeue();
        }
        else
        {
            switch (skillEffect)
            {
                case SkillEffect.AOE:
                    return new LogicSkill_AOE();
                case SkillEffect.DOT:
                    return new LogicSkill_DOT();
                case SkillEffect.Split:
                    return new LogicSkill_Split();
                case SkillEffect.Freeze:
                    return new LogicSkill_Freeze();
                case SkillEffect.Capture:
                    return new LogicSkill_Capture();
                case SkillEffect.Summon:
                    return new LogicSkillBase();
                case SkillEffect.SummonWithLimit:
                    return new LogicSkillBase();
                case SkillEffect.Rebel:
                    return new LogicSkill_Rebel();
                case SkillEffect.Burn:
                    return new LogicSkill_Burn();
                case SkillEffect.Rushing:
                    return new LogicSkill_Rushing();
                case SkillEffect.ChangeMoveSpeed:
                    return new LogicSkill_ChangeMoveSpeed();
                case SkillEffect.ChangeAttackSpeed:
                    return new LogicSkill_ChangeAttackSpeed();
                case SkillEffect.ChangeMaxHP:
                    return new LogicSkill_ChangeMaxHP();
                case SkillEffect.ChangeAttackPower:
                    return new LogicSkill_ChangeAttackPower();
                case SkillEffect.Clone:
                    return new LogicSkill_Clone();
                case SkillEffect.Relive:
                    return new LogicSkill_Relive();
                case SkillEffect.SelfDestruct:
                    return new LogicSkill_SelfDestruct();
                case SkillEffect.ChainLightning:
                    return new LogicSkill_ChainLightning();
                case SkillEffect.KnightCharging:
                    return new LogicSkill_KnightCharging();
                default:
                    return new LogicSkillBase();
            }
        }
    }

    public void RecycleLogicSkill(LogicSkillBase logicSkill)
    {
        int skillEffect = logicSkill.tableData.effect;
        logicSkill.Clear();
        logicSkillList.Remove(logicSkill);
        RecycleLogicSkill(logicSkill, skillEffect);
    }

    private void RecycleLogicSkill(LogicSkillBase logicSkill, int skillEffect)
    {
        var skillPool = skillPoolDict[(SkillEffect)skillEffect];
        skillPool.Enqueue(logicSkill);
    }

    public void ClearSkill()
    {
        foreach (var item in logicSkillList)
        {
            int skillEffect = item.tableData.effect;
            item.Clear();
            RecycleLogicSkill(item, skillEffect);
        }
        logicSkillList.Clear();
    }

    public void SkillCDUpdate(LogicBattleUnit unit)
    {
        if (unit.runtimeData.skills == null)
        {
            return;
        }
        foreach (var skillInfo in unit.runtimeData.skills.Values)
        {
            skillInfo.cdTimer += COMPUTE_DELTA_MILLISECOND;
        }
    }
}
