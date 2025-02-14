using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleStatus : IStatus
{
    public BattleUnitState Name { get { return BattleUnitState.Idle; } }

    public void StatusEnter(BattleUnit current)
    {
        BattleSystem.IdleStatusEnter(current);
    }

    public void StatusUpdate(BattleUnit current)
    {
    }

    public void StatusQuit(BattleUnit current)
    {

    }
}
