using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleUnit
{
    [HideInInspector]
    public int attackedNumber;
    [HideInInspector]
    public bool freezed;
    [HideInInspector]
    public bool isRushing;
    [HideInInspector]
    public float attackTimer;
    [HideInInspector]
    public float waitTimer;
    [HideInInspector]
    private bool haveIdle2;
    private bool haveAttack2;
    [HideInInspector]
    public bool haveDie;

    private Tween frozenTween;
    private Tween beAttackedTween;
    public MaterialPropertyBlock MaterialPropertyBlock { get; private set; }

    public void AttackingStatusEnter(BattleUnit victim)
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    public void AttackingStatusUpdate(BattleUnit victim)
    {
    }

    public void DeadAnimationStatusEnter()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
    }

    /// <summary>
    /// 冰冻物体
    /// </summary>
    public void SkillTakeEffect_Freezed()
    {
    }

    /// <summary>
    /// 解冻物体
    /// </summary>
    public void FinishSkill_Freezed()
    {
        if (frozenTween != null)
        {
            frozenTween.Kill();
            frozenTween = null;
            MeshRenderer mesh = GetComponentInChildren<MeshRenderer>();
            mesh.GetPropertyBlock(MaterialPropertyBlock);
            MaterialPropertyBlock.SetColor("_Black", Color.black);
            mesh.SetPropertyBlock(MaterialPropertyBlock);
        }
    }

    public void PlayBeAttackedAniamtion()
    {
    }

    public void ClearAttackVariable()
    {
        attackedNumber = 0;
        if (frozenTween != null)
        {
            frozenTween.Kill();
            frozenTween = null;
        }
        if (beAttackedTween != null)
        {
            beAttackedTween.Kill();
            beAttackedTween = null;
        }
    }
}
