using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleSystem
{
    private static IStatus currentStatus;
    private static Dictionary<BattleUnitState, IStatus> StatusMachineDic;
    private static HashSet<BattleUnit> updateAttackingIk = new HashSet<BattleUnit>();
    private static Dictionary<BattleUnit, (Vector2 position, bool updateWait)> updateAttackingWaitIk = new Dictionary<BattleUnit, (Vector2, bool)>();

    static BattleSystem()
    {
        string[] names = Enum.GetNames(typeof(BattleUnitState));
        StatusMachineDic = new Dictionary<BattleUnitState, IStatus>(names.Length);

        IStatus idleStatus = new IdleStatus();
        IStatus moveToAttackStatus = new MoveToAttackStatus();
        IStatus moveToEndStatus = new MoveToBasecampStatus();
        IStatus attackingStatus = new AttackingStatus();
        IStatus attackCompletedStatus = new AttackWaitStatus();
        IStatus deadStatus = new DeadStatus();

        StatusMachineDic.Add(idleStatus.Name, idleStatus);
        StatusMachineDic.Add(moveToAttackStatus.Name, moveToAttackStatus);
        StatusMachineDic.Add(moveToEndStatus.Name, moveToEndStatus);
        StatusMachineDic.Add(attackingStatus.Name, attackingStatus);
        StatusMachineDic.Add(attackCompletedStatus.Name, attackCompletedStatus);
        StatusMachineDic.Add(deadStatus.Name, deadStatus);
    }

    public static void SwitchStatus(BattleUnit item, BattleUnitState newStatus)
    {
        if (StatusMachineDic.ContainsKey(newStatus))
        {
            //上个状态退出
            item.preivousStatus = item.currentStatus;
            currentStatus = StatusMachineDic[item.preivousStatus];
            currentStatus.StatusQuit(item);

            //下个状态进入
            item.currentStatus = newStatus;
            currentStatus = StatusMachineDic[newStatus];
            currentStatus.StatusEnter(item);
        }
        else
        {
            GameFramework.Debug.LogError("要切换的状态未注册，请先注册该状态！状态名：" + newStatus.ToString());
        }
    }

    #region Idle

    public static void IdleStatusEnter(BattleUnit battleUnit)
    {
        battleUnit.IdleStatusEnter();
    }

    #endregion

    #region MoveToAttack

    public static void MoveToAttackStatusEnter(BattleUnit battleUnit)
    {
        BattleUnit target = GetTargetByAttacker(battleUnit);
        battleUnit.PlayMoveAnimation();
    }

    #endregion

    #region Attacking

    public static void AttackingStatusEnter(BattleUnit battleUnit)
    {
        battleUnit.attackedNumber = 0;
        BattleUnit target = GetTargetByAttacker(battleUnit);
        battleUnit.AttackingStatusEnter(target);
    }

    public static void AttackingStatusUpdate(BattleUnit battleUnit)
    {
        BattleUnit target = GetTargetByAttacker(battleUnit);
        battleUnit.AttackingStatusUpdate(target);
    }

    #endregion

    #region AttackWait

    public static void AttackWaitStatusEnter(BattleUnit battleUnit)
    {
    }

    public static void AttackWaitStatusUpdate(BattleUnit battleUnit)
    {
    }

    #endregion

    #region DeadStatus


    public static void DeadStatusEnter(BattleUnit battleUnit)
    {
    }

    #endregion

    #region 受伤、死亡

    /// <summary>
    /// 更新血量
    /// </summary>
    /// <param name="battleUnit"></param>
    /// <param name="damage"></param>
    public static void UpdateHP(BattleUnit battleUnit)
    {
    }

    #endregion
}
