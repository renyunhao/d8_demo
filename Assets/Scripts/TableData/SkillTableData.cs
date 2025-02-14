using GameFramework;
using System;

/// <summary>
/// 技能属性表_Skill：技能相关属性
/// </summary>
[Serializable]
public class SkillTableData : TableDataBase
{
    /// <summary>
    /// 技能效果类别
    /// </summary>
    public int effect;
    /// <summary>
    /// 主技能
    /// </summary>
    public bool mainSkill;
    /// <summary>
    /// 持续时间
    /// </summary>
    public double duration;
    /// <summary>
    /// 作用间隔
    /// </summary>
    public double interval;
    /// <summary>
    /// 延迟时间
    /// </summary>
    public double delayTime;
    /// <summary>
    /// 消失时间
    /// </summary>
    public double disappearTime;
    /// <summary>
    /// 复合技能效果
    /// </summary>
    public int[] effectGroup;
    /// <summary>
    /// 目标视觉效果
    /// </summary>
    public bool targetVisualVFX;
    /// <summary>
    /// 技能效果数值
    /// </summary>
    public double[] effectValue;
    /// <summary>
    /// 战舰技能效果数值
    /// </summary>
    public double[] warshipEffectValue;
    /// <summary>
    /// 目标选择
    /// </summary>
    public int targetSelect;
    /// <summary>
    /// 目标类型
    /// </summary>
    public int targetType;
    /// <summary>
    /// 目标数量
    /// </summary>
    public int targetNumber;
    /// <summary>
    /// 技能范围中心
    /// </summary>
    public int rangeCenter;
    /// <summary>
    /// 技能范围
    /// </summary>
    public double range;
    /// <summary>
    /// 触发时机
    /// </summary>
    public int triggerTiming;
    /// <summary>
    /// 触发条件参数
    /// </summary>
    public double triggerTimingValue;
    /// <summary>
    /// 结束时机
    /// </summary>
    public int endTiming;
    /// <summary>
    /// 结束时机参数
    /// </summary>
    public double endTimingValue;
    /// <summary>
    /// 是否重复触发
    /// </summary>
    public bool retriggerable;
    /// <summary>
    /// 技能CD
    /// </summary>
    public double cd;
    /// <summary>
    /// 是否替换
    /// </summary>
    public bool retriggerableReplace;
    /// <summary>
    /// 技能动画时长
    /// </summary>
    public double animationDuration;
    /// <summary>
    /// 释放音效
    /// </summary>
    public int releaseSound;
    /// <summary>
    /// 延迟音效
    /// </summary>
    public int delaySound;

}