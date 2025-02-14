using GameFramework;
using System;
using UnityEngine.UI;

public class BattleUI : BaseUI
{
    public Button startFightButton;
    public Button attackerAddSoldierButton;
    public Button defenderAddSoldierButton;

    public override void OnCreate()
    {
        startFightButton.onClick.AddListener(OnStartFightButtonClick);
        attackerAddSoldierButton.onClick.AddListener(OnAttackerAddSoldierButtonClick);
        defenderAddSoldierButton.onClick.AddListener(OnDefenderAddSoldierButtonClick);
    }

    public override void OnShow()
    {
    }

    private void OnStartFightButtonClick()
    {
        BattleSystem.StartFight();
        startFightButton.gameObject.SetActive(false);
    }

    private void OnAttackerAddSoldierButtonClick()
    {
        BattleSystem.AddSoldier(true, 1001, 1000);
    }

    private void OnDefenderAddSoldierButtonClick()
    {
        BattleSystem.AddSoldier(false, 1001, 1000);
    }
}
