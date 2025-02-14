using GameFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    void Start()
    {
        AssetSystem.Initialize();
        UISystem.Initialize(GameNode.UICamera, LayerMask.NameToLayer("UI"), 1080, 1920, AssetSystem.Load<GameObject>);
        UISystem.Show<LoginUI>();
        BattleSystem.Initialize();
    }

    private void Update()
    {
        BattleSystem.Update();
    }
}
