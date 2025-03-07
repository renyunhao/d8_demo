using GameFramework;
using UnityEngine;

public partial class BattleUnit : MonoBehaviour
{
    public BattleUnitState currentStatus;
    public BattleUnitState preivousStatus;

    public GameObject entityObject;
    private BattleUnitData data = new BattleUnitData();
    private Animator animator;
    private Transform unitTransform;
    private bool isAttacker;

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

    public bool IsAttacker => isAttacker;

    private void Awake()
    {
        unitTransform = this.transform;
        animator = entityObject.GetComponent<Animator>();
    }

    public void Initialize(int id, Vector3 pos, bool isAttacker)
    {
        data.id = id;
        this.isAttacker = isAttacker;
        this.transform.position = pos;
        currentStatus = BattleUnitState.Idle;
        if (isAttacker)
        {
            unitTransform.GetComponentInChildren<SkinnedMeshRenderer>().material = AssetSystem.Load<Material>("Red");
        }
        else
        {
            unitTransform.GetComponentInChildren<SkinnedMeshRenderer>().material = AssetSystem.Load<Material>("Blue");
        }
    }

    public void ReleaseLogicObject()
    {
        if (LogicBattleUnit != null)
        {
            this.LogicBattleUnit = null;
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

    /// <summary>
    /// 尝试播放技能动画，因为不是所有的技能释放者都有技能动画
    /// </summary>
    /// <param name="skillId"></param>
    public void TryPlaySkillAnimation(int skillId)
    {
    }

    /// <summary>
    /// 切换朝向，朝向是由当前单位与目标位置决定的，而不是由上一帧与这一帧的位置决定的
    /// 这样可以避免前进过程中由于其他效果影响位置反复前进后退引起的疯狂摇摆
    /// </summary>
    /// <param name="targetPos"></param>
    public void UpdateDirection(Vector3 targetPos)
    {
        this.entityObject.transform.forward = targetPos - this.entityObject.transform.position;
    }
}
