using GameFramework;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleSystem
{
    private static Dictionary<int, GameObjectPool> bulletPool = new Dictionary<int, GameObjectPool>();
    private static List<BulletBase> usingBulletList = new List<BulletBase>();

    private static void UpdateBullet() 
    {
        for (int i = 0; i < usingBulletList.Count; i++)
        {
            var bullet = usingBulletList[i];
            if (bullet.logicBullet.isAlive)
            {
                if (bullet.logicBullet.canFollowTarget)
                {
                    Vector3 bulletEndPosOrigin = bullet.logicBullet.targetPosition.ToVector3();
                    bulletEndPosOrigin.y /= 2;
                    bullet.UpdateViewEndPos(bulletEndPosOrigin);
                }
                bullet.CustomUpdate();
            }
            else
            {
                string endEffectName = bullet.logicBullet.tableData.endEffect;
                if (string.IsNullOrEmpty(endEffectName) == false)
                {
                    //VFXMgr.PlayBulletHitTargetEffect(endEffectName, bullet.viewEndPos);
                }
                LogicBattleUnit victim = logicBattleSystem.GetLogicBattleUnit(bullet.logicBullet.targetIndex);
                if (bullet.logicBullet.isHit && bullet.logicBullet.canFollowTarget && victim.runtimeData.currentStatus != BattleUnitState.Dead)
                {
                    PlayBulletHitEffect(bullet);
                    PlayBulletHitSoundEffect(bullet);
                }
                RecycleBullet(bullet);
                i--;
            }
        }
    }

    private static void ApplyBulletFrameData(BattleFrameOutputData frameData)
    {
        foreach (LogicBulletBase bulletBase in frameData.addBullets)
        {
            if (bulletBase.isAlive)
            {
                //找到发子弹的BattleUnit，因为attacker与defender的index分开了，所以是唯一的
                BattleUnit attacker = GetBattleUnitByIndex(bulletBase.shooterIndex);
                FireBullet(attacker, bulletBase);
            }
            else
            {
                logicBattleSystem.RecycleLogicBullet(bulletBase);
            }
        }
    }

    public static BulletBase FireBullet(BattleUnit attacker, LogicBulletBase logicBullet)
    {
        BulletBase bullet = GetOneBullet(logicBullet.tableData.id);
        Vector3 bulletEndPos;
        Vector3 bulletEndPosOffset;
        Vector3 bulletEndPosOrigin = logicBullet.targetPosition.ToVector3();
        bulletEndPosOrigin.y /= 2;
        float bulletDistance = (float)attacker.LogicBattleUnit.staticData.BulletDistance.Float;
        if (bulletDistance > 0)
        {
            //穿透箭不能随机选择受击位置，因为其方向是固定的，且有穿透碰撞，不能在表现层做随机
            var startPos = attacker.AttackBonePos;
            Vector3 dir = (logicBullet.targetPosition - logicBullet.startPosition).ToVector3();
            dir.y /= 2;
            dir = dir.normalized * bulletDistance;
            bulletEndPos = startPos + dir;
            bulletEndPosOffset = bulletEndPos - logicBullet.targetPosition.ToVector3();
        }
        else
        {
            var unit = GetBattleUnitByIndex(logicBullet.targetIndex);
            if (unit != null)
            {
                bulletEndPos = unit.GetHitEffectPos();
            }
            else
            {
                bulletEndPos = battleUnitHitEffectPosDict[logicBullet.targetIndex];
            }
            bulletEndPosOffset = bulletEndPos - bulletEndPosOrigin;
        }
        bullet.Initialize(logicBullet, attacker.AttackBonePos, bulletEndPosOrigin, bulletEndPosOffset);
        return bullet;
    }

    private static BulletBase GetOneBullet(int id)
    {
        if (bulletPool.ContainsKey(id) == false)
        {
            GameObjectPool pool = new GameObjectPool(AssetSystem.Load<GameObject>($"Bullet_{id}"), GameNode.PoolRoot);
            bulletPool.Add(id, pool);
        }
        BulletBase bullet = bulletPool[id].GetInstance().GetComponent<BulletBase>();
        usingBulletList.Add(bullet);
        return bullet;
    }

    public static void RecycleBullet(BulletBase bullet)
    {
        int id = bullet.logicBullet.tableData.id;
        logicBattleSystem.RecycleLogicBullet(bullet.logicBullet);
        bulletPool[id].RecycleInstance(bullet.gameObject);

        bullet.Clear();
        usingBulletList.Remove(bullet);
    }

    private static void PlayBulletHitEffect(BulletBase bullet)
    {
        string name = bullet.logicBullet.tableData.hitEffect;
        if (string.IsNullOrEmpty(name) == false)
        {
            //VFXMgr.PlayBulletHitTargetEffect(name, bullet.viewEndPos);
        }
    }

    private static void PlayBulletHitSoundEffect(BulletBase bullet)
    {
        int sound = bullet.logicBullet.tableData.hitSound;
        if (sound > 0)
        {
            //SoundMgr.Instance.Play(sound);
        }
    }

    private static void ClearBullet()
    {
        foreach (var item in usingBulletList)
        {
            int id = item.logicBullet.tableData.id;
            bulletPool[id].RecycleInstance(item.gameObject);
            item.Clear();
        }
        usingBulletList.Clear();
    }
}
