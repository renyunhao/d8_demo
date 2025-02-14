using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : BaseUI
{
    public Button loginButton;

    public override void OnCreate()
    {
        loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    public override void OnShow()
    {
    }

    private void OnLoginButtonClick()
    {
        UISystem.Hide<LoginUI>();
        BattleSystem.StartBattle();
        UISystem.Show<BattleUI>();
    }
}
