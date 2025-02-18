using GameFramework;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleUnit : MonoBehaviour
{
    public BattleUnitState currentStatus;
    public BattleUnitState preivousStatus;

    public GameObject entityObject;
    private BattleUnitData data = new BattleUnitData();
    private Animator animator;
    private Transform unitTransform;

    public BattleUnitData Data => data;

    public int ID => data.id;
    /// <summary>
    /// 与当前单位对应的纯逻辑对象
    /// </summary>
    public LogicBattleUnit LogicBattleUnit { get; private set; }

    public Transform UnitTransform => unitTransform;

    /// <summary>
    /// 攻击释放点骨骼的世界坐标
    /// </summary>
    public Vector3 AttackBonePos
    {
        get
        {
            return this.transform.position;
        }
    }

    private void Awake()
    {
        unitTransform = this.transform;
        animator = entityObject.GetComponent<Animator>();
    }

    public void Initialize(int id, Vector3 pos)
    {
        data.id = id;
        this.transform.position = pos;
        currentStatus = BattleUnitState.Idle;
    }

    public void ReleaseLogicObject()
    {
        if (LogicBattleUnit != null)
        {
            this.LogicBattleUnit = null;
        }
    }

    public void BindLogicObject(LogicBattleUnit logicBattleUnit)
    {
        this.LogicBattleUnit = logicBattleUnit;
        this.name = $"{logicBattleUnit.id}_{logicBattleUnit.index}";
        if (logicBattleUnit.IsAttacker)
        {
            //切为进攻方表现
        }
        else
        {
            //切为防守方表现
        }
    }

    public Vector3 GetHitEffectPos()
    {
        return this.transform.position;
    }

    public void PlayMoveAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Move");
        }
    }

    public void IdleStatusEnter()
    {
        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }
    }

    public void PlayIdleAnimation(bool showIdle2, bool random = false)
    {
    }

    /// <summary>
    /// 尝试播放技能动画，因为不是所有的技能释放者都有技能动画
    /// </summary>
    /// <param name="skillId"></param>
    public void TryPlaySkillAnimation(int skillId)
    {
    }
}
