using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 灼烧
/// </summary>
public class LogicSkill_Burn : LogicSkillBase
{
    protected override void SkillTakeEffect()
    {
        targetListCopy.Clear();
        targetListCopy.UnionWith(targetList);
        foreach (var target in targetListCopy)
        {
            if (target.runtimeData.currentStatus == BattleUnitState.Dead)
            {
                continue;
            }
            //固定伤害部分
            int damage1 = (int)(releaser.GetSkillAttackPower(target) * tableData.effectValue[0]);
            //目标已损失生命值部分
            int damage2 = (int)((target.staticData.maxHP - target.runtimeData.hp) * tableData.effectValue[1]);
            int damage = damage1 + damage2;
            LogicBattleSystem.Instance.OnVictimBloodLoss(releaser, target, damage);
        }
    }
}
