using FixPointUnity;
using System.Collections.Generic;
using UnityEngine;

public class LogicSkillBase
{
    public static int SkillUniqueId = 0;

    /// <summary>
    /// 唯一编号
    /// </summary>
    public int uniqueId;
    public long delayTimer;
    public long durationTimer;
    public long disappearTimer;
    public long effectTimer;
    public SkillProgress progress;
    public SkillTableData tableData;
    public F64 skillRange;
    /// <summary>
    /// 技能持续时间，注意，不要直接使用tableData.duration，因为有些技能的持续时间需要由技能本身来决定，外界还需要读取这个值
    /// </summary>
    public long duration;
    /// <summary>
    /// 技能释放时，引发技能的目标索引，注意不是指技能的作用目标，是导致这个技能触发的目标
    /// </summary>
    public int targetIndex;
    public F64Vec3 pos;
    public bool isAlive;
    /// <summary>
    /// 技能施放者
    /// </summary>
    public LogicBattleUnit releaser;
    /// <summary>
    /// 技能绑定的子弹
    /// </summary>
    public LogicBulletBase bullet;
    /// <summary>
    /// 技能目标
    /// 有些技能是在产生作用的时候，实时获取（可能不止一个）
    /// 有些技能是在创建技能的时候赋值且不再改变
    /// </summary>
    public HashSet<LogicBattleUnit> targetList = new HashSet<LogicBattleUnit>();
    /// <summary>
    /// 用于在技能生效时复制targetList内容，然后遍历，避免循环过程中修改集合异常
    /// </summary>
    public HashSet<LogicBattleUnit> targetListCopy = new HashSet<LogicBattleUnit>();
    /// <summary>
    /// 主技能
    /// </summary>
    public LogicSkillBase mainSkill;
    /// <summary>
    /// 子技能
    /// </summary>
    public HashSet<LogicSkillBase> subSkills;
    /// <summary>
    /// 当前技能结束后要触发的技能
    /// </summary>
    public List<LogicSkillBase> nextSkills;
    /// <summary>
    /// 当前技能结束后要终结的技能
    /// </summary>
    public List<LogicSkillBase> terminateSkills;

    /// <summary>
    /// 是否为增益技能
    /// </summary>
    public virtual bool IsPositive => true;

    public virtual void CustomUpdate(long timeDelta)
    {
        if (progress == SkillProgress.Start)
        {
            if (delayTimer >= (long)(tableData.delayTime * LogicBattleSystem.SECOND_TO_MILLISECOND))
            {
                ReleaseSkill();
            }
            else
            {
                delayTimer += timeDelta;
            }
        }
        else if (progress == SkillProgress.TakeEffect)
        {
            if (duration >= 0 && durationTimer >= duration)
            {
                SkillDisappear();
            }
            else
            {
                durationTimer += timeDelta;
                if (tableData.interval > 0)
                {
                    effectTimer += timeDelta;
                    if (effectTimer >= (long)(tableData.interval * LogicBattleSystem.SECOND_TO_MILLISECOND))
                    {
                        effectTimer = 0;
                        SkillTakeEffect();
                    }
                }
                SkillUpdate();
            }
        }
        else if (progress == SkillProgress.Disappear)
        {
            if (disappearTimer >= (long)(tableData.disappearTime * LogicBattleSystem.SECOND_TO_MILLISECOND))
            {
                SkillFinish();
            }
            else
            {
                disappearTimer += timeDelta;
            }
        }
    }

    protected virtual void SkillUpdate()
    {

    }

    public virtual void Initialize(bool pending = false)
    {
        skillRange = F64.FromDouble(tableData.range);
        uniqueId = SkillUniqueId++;
        duration = (long)(tableData.duration * LogicBattleSystem.SECOND_TO_MILLISECOND);
        isAlive = true;
        GameFramework.Debug.Log($"技能{tableData.id} Id {uniqueId} 释放 状态:Pending", "SkillDebug");
        if (pending)
        {
            progress = SkillProgress.Pending;
        }
        else
        {
            SkillStart(false);
        }
    }

    public virtual void SkillStart(bool pending = true)
    {
        if (bullet == null)
        {
            if (tableData.rangeCenter == (int)SkillRangeCenter.Releaser)
            {
                pos = releaser.runtimeData.pos;
            }
            else if (tableData.rangeCenter == (int)SkillRangeCenter.Target)
            {
                if (tableData.targetSelect == (int)SkillTarget.AttackTarget)
                {
                    var attackReleate = LogicBattleSystem.Instance.GetOnceAttackRelateByAttacker(releaser);
                    if (attackReleate != null)
                    {
                        pos = attackReleate.mainTarget.runtimeData.pos;
                    }
                    else
                    {
                        pos = releaser.runtimeData.pos;
                    }
                }
                else
                {
                    GameFramework.Debug.LogError($"技能{tableData.id} rangeCenter填写以目标为中心，但是目标选择targetSelect不是AttackTarget");
                }
            }
        }
        else
        {
            pos = bullet.targetPosition;
        }
        progress = SkillProgress.Start;
        GameFramework.Debug.Log($"技能{tableData.id} Id {uniqueId} 启动 状态:Start", "SkillDebug");
        //如果技能有动画技能持续时间，则释放者要进入施放技能状态
        if (tableData.animationDuration > 0)
        {
            releaser.runtimeData.performSkillTime = (long)(tableData.animationDuration * LogicBattleSystem.SECOND_TO_MILLISECOND);
            LogicBattleSystem.Instance.SwitchStatus(releaser, BattleUnitState.PerformSkill);
        }
    }

    protected virtual void ReleaseSkill()
    {
        progress = SkillProgress.TakeEffect;
        SkillTakeEffect();
        if (duration == 0)
        {
            SkillDisappear();
        }
    }

    /// <summary>
    /// 技能生效，产生作用
    /// </summary>
    protected virtual void SkillTakeEffect()
    {
        GameFramework.Debug.Log($"技能{tableData.id} Id {uniqueId} 生效 状态:TakeEffect", "SkillDebug");
        LogicBattleSystem.Instance.FillSkillTargetList(this.releaser, this, this.bullet);
        //如果是主技能
        if (tableData.mainSkill)
        {
            if (tableData.effectValue != null && tableData.effectValue.Length > 1)
            {
                if (this.subSkills == null)
                {
                    this.subSkills = new HashSet<LogicSkillBase>();
                }
                //释放子技能
                foreach (var subSkillId in tableData.effectValue)
                {
                    //通常来说，子技能的触发时机和主技能是一样
                    //如果不一样，就需要在下面的逻辑中特殊处理
                    SkillTableData tableData = null;// TableDataMgr.GetSingleSkillTableData((int)subSkillId);
                    if (tableData.triggerTiming == this.tableData.triggerTiming)
                    {
                        var subSkill = LogicBattleSystem.Instance.ReleaseSkill(tableData, releaser, bullet);
                        subSkill.mainSkill = this;
                        this.subSkills.Add(subSkill);
                    }
                    else if (tableData.triggerTiming == (int)SkillTriggerTiming.PreSkillEnd)
                    {
                        //有些技能的触发时机是前置技能结束，这个前置技能是指同属于一个技能的其他子技能
                        //这个技能在生成的时候并不开启，而是等待，前置技能结束的时候开启
                        int preSkillId = (int)tableData.triggerTimingValue;
                        foreach (var skill in subSkills)
                        {
                            if (skill.tableData.id == preSkillId)
                            {
                                var subSkill = LogicBattleSystem.Instance.ReleasePendingSkill(tableData, releaser);
                                subSkill.mainSkill = this;
                                if (skill.nextSkills == null)
                                {
                                    skill.nextSkills = new List<LogicSkillBase>(1);
                                }
                                skill.nextSkills.Add(subSkill);
                                this.subSkills.Add(subSkill);
                                break;
                            }
                        }
                    }
                    else
                    {
                        var subSkill = LogicBattleSystem.Instance.ReleasePendingSkill(tableData, releaser);
                        subSkill.mainSkill = this;
                        this.subSkills.Add(subSkill);
                    }
                }

                //配置结束时机为其他技能结束时的技能
                if (this.tableData.endTiming == (int)SkillEndTiming.SkillEnd)
                {
                    int endSkillId = (int)this.tableData.endTimingValue;
                    foreach (var otherSkill in subSkills)
                    {
                        if (otherSkill.tableData.id == endSkillId)
                        {
                            if (otherSkill.terminateSkills == null)
                            {
                                otherSkill.terminateSkills = new List<LogicSkillBase>(1);
                            }
                            otherSkill.terminateSkills.Add(this);
                            break;
                        }
                    }
                }

                foreach (var skill in subSkills)
                {
                    if (skill.tableData.endTiming == (int)SkillEndTiming.SkillEnd)
                    {
                        int endSkillId = (int)skill.tableData.endTimingValue;
                        foreach (var otherSkill in subSkills)
                        {
                            if (otherSkill.tableData.id == endSkillId)
                            {
                                if (otherSkill.terminateSkills == null)
                                {
                                    otherSkill.terminateSkills = new List<LogicSkillBase>(1);
                                }
                                otherSkill.terminateSkills.Add(skill);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"主技能：{tableData.id} 没有配置子技能或只配置了一个子技能");
            }
        }
    }

    /// <summary>
    /// 技能开始消失
    /// </summary>
    protected virtual void SkillDisappear()
    {
        if (progress < SkillProgress.Disappear)
        {
            SkillLoseEffect();
            progress = SkillProgress.Disappear;
            GameFramework.Debug.Log($"技能{tableData.id} Id {uniqueId} 开始消失 状态:Disappear", "SkillDebug");
            if (disappearTimer >= (long)(tableData.disappearTime * LogicBattleSystem.SECOND_TO_MILLISECOND))
            {
                SkillFinish();
            }
        }
    }

    /// <summary>
    /// 移除技能效果
    /// </summary>
    protected virtual void SkillLoseEffect()
    {
    }

    /// <summary>
    /// 由外部调用，主动结束技能，只针对那些持续时间填了负数的技能
    /// </summary>
    public virtual void EndSkill()
    {
        if (disappearTimer < (long)(tableData.disappearTime * LogicBattleSystem.SECOND_TO_MILLISECOND))
        {
            SkillDisappear();
            SkillFinish();
        }
        else
        {
            SkillDisappear();
        }
    }

    protected virtual void SkillFinish()
    {
        if (progress < SkillProgress.Completed)
        {
            progress = SkillProgress.Completed;
            GameFramework.Debug.Log($"技能{tableData.id} Id {uniqueId} 结束 状态:Completed", "SkillDebug");
            releaser.runtimeData.releasingSkill = false;
            isAlive = false;
            if (nextSkills != null)
            {
                foreach (var skill in nextSkills)
                {
                    skill.SkillStart(true);
                    LogicBattleSystem.Instance.RecordSkill(skill);
                }
            }
            if (terminateSkills != null)
            {
                foreach (var skill in terminateSkills)
                {
                    skill.EndSkill();
                }
            }
        }
    }

    public virtual void Clear()
    {
        uniqueId = 0;
        delayTimer = 0;
        durationTimer = 0;
        disappearTimer = 0;
        effectTimer = 0;
        progress = SkillProgress.Pending;
        releaser = null;
        targetList.Clear();
        mainSkill = null;
        nextSkills?.Clear();
        terminateSkills?.Clear();
        subSkills?.Clear();
    }
}
