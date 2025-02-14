using UnityEngine;

public class SkillBase : MonoBehaviour
{
    public enum SkillProgress
    {
        None,
        Start,
        Duration,
        End,
        Complete
    }

    protected const string AnimationName_SkillStart = "skill_start";
    protected const string AnimationName_SkillDuration = "skill_duration";
    protected const string AnimationName_SkillEnd = "skill_end";

    public SkillProgress progress;
    public LogicSkillBase logicSkill;

    protected double totaltime;
    protected bool haveStart;
    protected double startTime;
    protected bool haveDuration;
    protected double durationTime;
    protected bool haveEnd;
    protected double endTime;

    protected double skillTime;

    public virtual void StartSkill(int order)
    {

        totaltime = logicSkill.tableData.delayTime + logicSkill.duration + logicSkill.tableData.disappearTime;
        durationTime = totaltime - startTime - endTime;
        transform.position = logicSkill.pos.ToVector3();
        if (haveStart)
        {
            SkillStart();
        }
        else if (haveDuration)
        {
            SkillDuration();
        }
        else if (haveEnd)
        {
            SkillEnd();
        }
        else
        {
            SkillComplete();
        }
    }

    public virtual void CustomUpdate()
    {
        skillTime += Time.deltaTime;
        if (progress == SkillProgress.Start)
        {
            SkillStartUpdate();
        }
        else if (progress == SkillProgress.Duration)
        {
            SkillDurationUpdate();
        }
        else if (progress == SkillProgress.End)
        {
            SkillEndUpdate();
        }
    }

    protected virtual void SkillStart()
    {
        skillTime = 0;
        progress = SkillProgress.Start;
    }

    protected virtual void SkillStartUpdate()
    {
        if (skillTime >= startTime)
        {
            if (haveDuration)
            {
                SkillDuration();
            }
            else if (haveEnd)
            {
                SkillEnd();
            }
            else
            {
                SkillComplete();
            }
            //FightingSystem.PlaySkillReleasingSound(this);
        }
    }

    protected virtual void SkillDuration()
    {
        skillTime = 0;
        progress = SkillProgress.Duration;
    }

    protected virtual void SkillDurationUpdate()
    {
        if (skillTime >= durationTime)
        {
            if (haveEnd)
            {
                SkillEnd();
            }
            else
            {
                SkillComplete();
            }
        }
    }

    protected virtual void SkillEnd()
    {
        skillTime = 0;
        progress = SkillProgress.End;
    }

    protected virtual void SkillEndUpdate()
    {
        if (skillTime >= endTime)
        {
            SkillComplete();
        }
    }

    protected virtual void SkillComplete()
    {
        progress = SkillProgress.Complete;
    }

    /// <summary>
    /// 技能清理
    /// </summary>
    public virtual void Clear()
    {
        progress = SkillProgress.None;
        logicSkill = null;
    }
}
