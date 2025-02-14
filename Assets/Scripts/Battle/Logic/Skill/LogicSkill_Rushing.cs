using FixPointUnity;

/// <summary>
/// 冲锋
/// </summary>
public class LogicSkill_Rushing : LogicSkillBase
{
    public override bool IsPositive => true;
    private F64Vec3 rushingStartPos;
    /// <summary>
    /// 冲锋的目标位置，在目标还存活时每帧更新，不使用
    /// 在目标死亡后，使用该值做为冲锋的结束位置
    /// </summary>
    private F64Vec3 rushingEndPos;

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        //通过冲锋技能移动释放者视野范围那么长的距离需要的时间
        long maxTime = (long)(tableData.effectValue[0] * LogicBattleSystem.SECOND_TO_MILLISECOND);
        LogicOnceAttackRelate attackRelate = LogicBattleSystem.Instance.GetOnceAttackRelateByAttacker(releaser);
        LogicBattleUnit target = attackRelate.mainTarget;
        F64 distance = F64Vec3.Distance(releaser.runtimeData.pos, target.runtimeData.pos);
        F64 percent = distance / releaser.staticData.guardRadius;
        duration = (long)(percent.Double * maxTime);
        releaser.runtimeData.performSkillTime = duration;
        rushingStartPos = releaser.runtimeData.pos;
    }

    protected override void SkillUpdate()
    {
        base.SkillUpdate();

        F64 percent = F64.FromRaw(durationTimer) / F64.FromRaw(duration);
        LogicOnceAttackRelate attackRelate = LogicBattleSystem.Instance.GetOnceAttackRelateByAttacker(releaser);
        if (attackRelate != null && attackRelate.mainTarget.runtimeData.currentStatus != BattleUnitState.Dead)
        {
            LogicBattleUnit target = attackRelate.mainTarget;
            rushingEndPos = target.runtimeData.pos;
            //注意，冲锋结束位置要考虑目标的体积
            F64Vec3 dir = F64Vec3.NormalizeFastest(rushingStartPos - rushingEndPos) * (target.staticData.volumeRadius + releaser.staticData.volumeRadius);
            rushingEndPos += dir;
        }
        releaser.runtimeData.pos = F64Vec3.Lerp(rushingStartPos, rushingEndPos, percent);
    }
}
