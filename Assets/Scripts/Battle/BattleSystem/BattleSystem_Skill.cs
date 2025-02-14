using GameFramework;
using System.Collections.Generic;
using UnityEngine;

public partial class BattleSystem
{
    private static readonly string Skill_Name = "Skill_{0}";
    private static readonly string SkillVFX_Name = "SkillVFX_{0}";
    private static Dictionary<int, bool> skillHasEntity = new Dictionary<int, bool>();
    private static Dictionary<int, GameObjectPool> skillEntityPool = new Dictionary<int, GameObjectPool>();
    private static Dictionary<int, GameObjectPool> skillVFXEntityPool = new Dictionary<int, GameObjectPool>();
    private static int skillOrder;

    private static List<SkillBase> usingSkillList = new List<SkillBase>();
    private static Dictionary<SkillBase, List<GameObject>> usingSkillVFXDict = new Dictionary<SkillBase, List<GameObject>>();

    public static void UpdateSkill()
    {
        for (int i = 0; i < usingSkillList.Count; i++)
        {
            if (usingSkillList[i].logicSkill.isAlive == false && usingSkillList[i].progress == SkillBase.SkillProgress.Complete)
            {
                SkillBase skill = usingSkillList[i];
                int skillId = skill.logicSkill.tableData.id;
                logicBattleSystem.RecycleLogicSkill(skill.logicSkill);
                skill.Clear();
                skillEntityPool[skillId].RecycleInstance(skill.gameObject);
                if (usingSkillVFXDict.ContainsKey(skill))
                {
                    var vfxList = usingSkillVFXDict[skill];
                    foreach (var vfx in vfxList)
                    {
                        skillVFXEntityPool[skillId].RecycleInstance(vfx);
                    }
                    vfxList.Clear();
                }
                usingSkillList.RemoveAt(i);
                i--;
            }
            else
            {
                usingSkillList[i].CustomUpdate();
            }
        }
    }

    public static void ApplySkillFrameData(BattleFrameOutputData frameData)
    {
        foreach (LogicSkillBase item in frameData.addSkills)
        {
            ReleaseSkill(item);
        }
    }

    public static void ReleaseSkill(LogicSkillBase logicSkill)
    {
        int skillId = logicSkill.tableData.id;
        if (skillEntityPool.ContainsKey(skillId) == false && skillHasEntity.ContainsKey(skillId) == false)
        {
            string skillName = string.Format(Skill_Name, skillId);
            if (AssetSystem.Have(skillName))
            {
                GameObjectPool pool = new GameObjectPool(AssetSystem.Load<GameObject>(skillName), GameNode.PoolRoot);
                skillEntityPool.Add(skillId, pool);
                skillHasEntity.Add(skillId, true);
            }
            else
            {
                skillHasEntity.Add(skillId, false);
            }
        }
        if (skillHasEntity[skillId])
        {
            skillOrder++;
            SkillBase skill = skillEntityPool[skillId].GetInstance().GetComponent<SkillBase>();
            skill.logicSkill = logicSkill;
            usingSkillList.Add(skill);
            skill.StartSkill(skillOrder);

            //技能在目标身上也要有效果
            if (logicSkill.tableData.targetVisualVFX)
            {
                if (skillVFXEntityPool.ContainsKey(skillId) == false)
                {
                    string skillName = string.Format(SkillVFX_Name, skillId);
                    GameObjectPool pool = new GameObjectPool(AssetSystem.Load<GameObject>(skillName), GameNode.PoolRoot);
                    skillVFXEntityPool.Add(skillId, pool);
                }

                if (usingSkillVFXDict.TryGetValue(skill, out var vfxList) == false)
                {
                    vfxList = new List<GameObject>(logicSkill.targetList.Count);
                    usingSkillVFXDict.Add(skill, vfxList);
                }
            }
        }

        var releaser = GetBattleUnitByIndex(logicSkill.releaser.index);
        if (releaser != null && releaser.currentStatus != BattleUnitState.Dead)
        {
            releaser.TryPlaySkillAnimation(skillId);
        }
    }

    public static void ClearSkill()
    {
        skillOrder = 0;
        for (int i = 0; i < usingSkillList.Count; i++)
        {
            SkillBase skill = usingSkillList[i];
            int skillId = skill.logicSkill.tableData.id;
            skill.Clear();
            skillEntityPool[skillId].RecycleInstance(skill.gameObject);
            if (skillVFXEntityPool.ContainsKey(skillId))
            {
                skillVFXEntityPool[skillId].RecycleAllInstance();
            }
        }
        usingSkillVFXDict.Clear();
        usingSkillList.Clear();
    }
}
