using UnityEngine;

public abstract class BulletBase : MonoBehaviour
{
    protected SpriteRenderer sprite;
    protected ParticleSystemRenderer[] particles;

    public LogicBulletBase logicBullet;

    [HideInInspector]
    public Vector2 viewStartPos;
    [HideInInspector]
    public Vector2 viewEndPos;

    private long logicTimer;

    /// <summary>
    /// 目标位置的偏移，对于跟踪弹来说，如果目标拥有随机受击点，不在初始记录一次偏移的话，每帧都跟着目标的随机受击点，会导致箭抖动
    /// </summary>
    private Vector2 viewEndPosOffset;

    private void Awake()
    {
        sprite = this.GetComponentInChildren<SpriteRenderer>();
        particles = this.GetComponentsInChildren<ParticleSystemRenderer>();
    }

    public virtual void Initialize(LogicBulletBase logicBullet, Vector2 viewStartPos, Vector2 viewEndPosOrigin, Vector2 viewEndPosOffset = default(Vector2))
    {
        this.logicBullet = logicBullet;
        this.viewStartPos = viewStartPos;
        this.viewEndPosOffset = viewEndPosOffset;
        UpdateViewEndPos(viewEndPosOrigin);

        transform.position = viewStartPos;
    }

    public virtual void CustomUpdate()
    {
        if (logicBullet.isAlive)
        {
            if (logicBullet.timer > logicTimer && logicBullet.timer < logicBullet.totalTime)
            {
                MoveToTarget();
            }
        }
    }

    public virtual void UpdateViewEndPos(Vector2 viewEndPosOrigin)
    {
        this.viewEndPos = viewEndPosOrigin + viewEndPosOffset;
    }

    protected virtual void MoveToTarget()
    {
        logicTimer = logicBullet.timer;
    }

    public void Clear()
    {
        logicBullet = null;
        viewStartPos = Vector2.zero;
        logicTimer = 0;
    }
}
