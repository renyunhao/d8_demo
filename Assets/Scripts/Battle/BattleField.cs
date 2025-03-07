using Cysharp.Text;
using FixPointUnity;
using GameFramework;
using System.Collections.Generic;
using UnityEngine;

public class BattleField : MonoBehaviour
{
    public Basecamp attackerBasecamp;
    public Basecamp defenderBasecamp;
    public Transform battleUnitContainer;
    public Material[] unitColorMaterials;

    private Dictionary<int, MonoBehaviourPool<BattleUnit>> battleUnitPoolDict = new Dictionary<int, MonoBehaviourPool<BattleUnit>>();

    public void RemoveBattleUnit(BattleUnit battleUnit)
    {
        if (battleUnitPoolDict.TryGetValue(battleUnit.ID, out var pool))
        {
            pool.RecycleInstance(battleUnit);
        }
    }

    public BattleUnit SpawnBattleUnit(bool isAttacker, int id, F64Vec3 pos)
    {
        if (battleUnitPoolDict.TryGetValue(id, out var pool) == false)
        {
            pool = new MonoBehaviourPool<BattleUnit>(AssetSystem.Load<GameObject>(ZString.Concat("BattleUnit_", id)).GetComponent<BattleUnit>(), GameNode.PoolRoot, GameNode.PoolRoot);
            battleUnitPoolDict[id] = pool;
        }
        var battleUnit = pool.GetInstance();
        battleUnit.Initialize(id, pos.ToVector3(), isAttacker);

        return battleUnit;
    }
}
