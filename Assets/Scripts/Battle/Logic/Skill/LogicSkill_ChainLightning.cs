using FixPointUnity;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 连锁闪电
/// </summary>
public class LogicSkill_ChainLightning : LogicSkillBase
{
    public override bool IsPositive => false;

    public List<F64Vec3> positionList = new List<F64Vec3>();

    protected override void SkillTakeEffect()
    {
        base.SkillTakeEffect();
        LogicBattleUnit targetBattleUnit = null;

        if (targetList.Count > 0)
        {
            var enumerator = targetList.GetEnumerator();
            enumerator.MoveNext();
            F64Vec3 position = enumerator.Current.runtimeData.pos;
            List<LogicBattleUnit> enemyList = LogicBattleSystem.Instance.GetEnemyUnitWithinRange_Sort(position, skillRange, F64.Zero, tableData.targetType, releaser.IsAttacker);
            foreach (var item in enemyList)
            {
                if (targetList.Contains(item) == false)
                {
                    targetBattleUnit = item;
                    targetList.Add(item);
                    positionList.Add(item.runtimeData.pos);
                    break;
                }
            }
        }
        else
        {
            LogicOnceAttackRelate attackRelate = LogicBattleSystem.Instance.GetOnceAttackRelateByAttacker(releaser);
            targetBattleUnit = attackRelate.mainTarget;
            targetList.Add(attackRelate.mainTarget);
            positionList.Add(attackRelate.mainTarget.runtimeData.pos);
        }

        if (targetBattleUnit != null && targetBattleUnit.runtimeData.currentStatus != BattleUnitState.Dead)
        {
            int damage = releaser.GetSkillAttackPower(targetBattleUnit);
            LogicBattleSystem.Instance.OnVictimBloodLoss(releaser, targetBattleUnit, damage);
        }
        else
        {
            SkillDisappear();
        }
    }

    public override void Clear()
    {
        base.Clear();
        targetList.Clear();
        positionList.Clear();
    }
}
