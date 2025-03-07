using GameFramework;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GameSystem : MonoBehaviour
{
    void Start()
    {
        AssetSystem.Initialize();
        UISystem.Initialize(GameNode.UICamera, LayerMask.NameToLayer("UI"), 1080, 1920, AssetSystem.Load<GameObject>);
        UISystem.Show<LoginUI>();
        //BattleSystem.Initialize();
    }

    private void Update()
    {
        BattleSystem.Update();
    }

    private void OnGUI()
    {
        DebugDrawQuadrant();
    }

    private static void DebugDrawQuadrant()
    {
        int drawSize = 25;
        for (int x = -drawSize; x <= drawSize; x++)
        {
            Vector3 start = new Vector3(x, 0, -drawSize) * QuadrantSystem.QuadrantCellSize;
            Vector3 end = new Vector3(x, 0, drawSize) * QuadrantSystem.QuadrantCellSize;
            Debug.DrawLine(start, end);
        }
        for (int z = -drawSize; z <= drawSize; z++)
        {
            Vector3 start = new Vector3(-drawSize, 0, z) * QuadrantSystem.QuadrantCellSize;
            Vector3 end = new Vector3(drawSize, 0, z) * QuadrantSystem.QuadrantCellSize;
            Debug.DrawLine(start, end);
        }
    }
}
