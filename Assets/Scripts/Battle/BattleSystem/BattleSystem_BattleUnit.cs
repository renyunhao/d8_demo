using FixPointUnity;
using GameFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleSystem
{
    /// <summary>
    /// 战斗单位的受击点位置字典，Key是对应的LogicBattleUnit.index，
    /// 之所以不直接从BattleUnit中读取，而是先存到字典中再读取使用，
    /// 是因为有些情况下，要用的时候，BattleUnit已经不存在了
    /// </summary>
    private static Dictionary<int, Vector3> battleUnitHitEffectPosDict = new Dictionary<int, Vector3>();

    #region 更新BattleUnit

    private static void ApplyStatusFrameData(BattleFrameOutputData frameData)
    {
        foreach (var (logicBattleUnit, _, newStatus) in frameData.statusChangedUnits)
        {
            BattleUnit battleUnit = GetBattleUnitByIndex(logicBattleUnit.index);
            if (battleUnit != null)
            {
                var oldStatus = battleUnit.currentStatus;
                if (oldStatus != newStatus)
                {
                    SwitchStatus(battleUnit, newStatus);
                }
            }
        }
    }

    private static void ApplyAddBattleUnitFrameData(BattleFrameOutputData frameData)
    {
    }

    private static void UpdateAttacker()
    {
        for (int i = 0; i < attackerList.Count; i++)
        {
            BattleUnit battleUnit = attackerList[i];
            int index = battleUnit.LogicBattleUnit.index;
            ApplyBattleLogic(battleUnit);
            if (battleUnit.currentStatus == BattleUnitState.Dead)
            {
                if (updateAttackingIk.Contains(battleUnit))
                {
                    updateAttackingIk.Remove(battleUnit);
                }
                if (updateAttackingWaitIk.ContainsKey(battleUnit))
                {
                    updateAttackingWaitIk.Remove(battleUnit);
                }
                battleField.RemoveBattleUnit(battleUnit);
                attackerList.Remove(battleUnit);
                attackerDic.Remove(index);
                i--;
            }
        }
    }

    private static void UpdateDefender()
    {
        for (int i = 0; i < defenderList.Count; i++)
        {
            BattleUnit battleUnit = defenderList[i];
            if (battleUnit.LogicBattleUnit.staticData.isWall == false)
            {
                ApplyBattleLogic(battleUnit);
                int index = battleUnit.LogicBattleUnit.index;
                if (battleUnit.currentStatus == BattleUnitState.Dead)
                {
                    int id = battleUnit.LogicBattleUnit.id;
                    if (updateAttackingIk.Contains(battleUnit))
                    {
                        updateAttackingIk.Remove(battleUnit);
                    }
                    if (updateAttackingWaitIk.ContainsKey(battleUnit))
                    {
                        updateAttackingWaitIk.Remove(battleUnit);
                    }

                    defenderDic.Remove(index);
                    defenderList.Remove(battleUnit);
                    i--;
                }
            }
        }
    }

    private static void ApplyBattleLogic(BattleUnit battleUnit)
    {
        var logicBattleUnitRuntimeData = battleUnit.LogicBattleUnit.runtimeData;
        //更新血条
        if (battleUnit.Data.hp != logicBattleUnitRuntimeData.hp)
        {
            battleUnit.Data.hp = logicBattleUnitRuntimeData.hp;
            //FightingSystem.PlayBeAttackedSoundEffect(battleUnit);
            //FightingSystem.PlayBeAttackedAnimation(battleUnit);
            UpdateHP(battleUnit);
        }
        //更新位置
        ApplyPosition(battleUnit);
        //更新攻击
        ApplyAttacking(battleUnit);
        //状态Update
        StatusMachineDic[battleUnit.currentStatus].StatusUpdate(battleUnit);
    }

    #endregion

    private static void ApplyPosition(BattleUnit battleUnit)
    {
        var logicBattleUnitRuntimeData = battleUnit.LogicBattleUnit.runtimeData;
        BattleUnit victim = GetTargetByAttacker(battleUnit);

        //为了保证单位的朝向正确，有目标的时候，朝向目标，无目标的时候，有路径，朝向路径的下一个点，无路径保持当前朝向不变

        F64Vec3 targetPos = F64Vec3.Zero;
        if (victim != null)
        {
            targetPos = victim.LogicBattleUnit.runtimeData.pos;
        }
        else
        {
            targetPos = battleUnit.LogicBattleUnit.runtimeData.moveToPos;
        }
        var direction = targetPos - battleUnit.LogicBattleUnit.runtimeData.pos;
        battleUnit.transform.LookAt(targetPos.ToVector3());
        battleUnit.transform.position = battleUnit.LogicBattleUnit.runtimeData.pos.ToVector3();
    }

    private static void ApplyAttacking(BattleUnit battleUnit)
    {
        if (battleUnit.LogicBattleUnit.runtimeData.attackedNumber > battleUnit.attackedNumber)
        {
            battleUnit.attackedNumber = battleUnit.LogicBattleUnit.runtimeData.attackedNumber;
        }
    }

    public static BattleUnit GetBattleUnitByIndex(int index)
    {
        if (attackerDic.ContainsKey(index))
        {
            return attackerDic[index];
        }
        else if (defenderDic.ContainsKey(index))
        {
            return defenderDic[index];
        }
        else
        {
            return null;
        }
    }

    private static BattleUnit GetTargetByAttacker(BattleUnit attacker)
    {
        LogicOnceAttackRelate related = GetOnceAttackRelateByAttacker(attacker);
        if (related != null && related.mainTarget != null)
        {
            return GetBattleUnitByIndex(related.mainTarget.index);
        }
        return null;
    }

    private static LogicOnceAttackRelate GetOnceAttackRelateByAttacker(BattleUnit attacker)
    {
        return logicBattleSystem.GetOnceAttackRelateByAttacker(attacker.LogicBattleUnit);
    }
}
