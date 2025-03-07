using System.Collections.Generic;
using UnityEngine;
using GameFramework;
using Unity.Entities;

public partial class BattleSystem
{
    public static readonly float Time_Reconnoiter = 30;
    /// <summary>
    /// 战斗时间缩放倍数
    /// </summary>
    public readonly int[] TimeScaleArray = new int[] { 1, 2, 4 };

    public static BattleSystem Instance;

    /// <summary>
    /// 防守方战斗单位
    /// </summary>
    private static Dictionary<int, BattleUnit> defenderDic = new Dictionary<int, BattleUnit>();
    private static List<BattleUnit> defenderList = new List<BattleUnit>();

    /// <summary>
    /// 进攻方战斗单位
    /// </summary>
    private static Dictionary<int, BattleUnit> attackerDic = new Dictionary<int, BattleUnit>();
    private static List<BattleUnit> attackerList = new List<BattleUnit>();

    private static LogicBattleSystem logicBattleSystem = new LogicBattleSystem();
    private static BattleField battleField;
    
    public static BattleProgress CurrentProgress { get; private set; }

    public static BattleField BattleField => battleField;

    public static void Initialize(BattleField instance)
    {
        battleField = instance;
    }

    public static void StartBattle()
    {
        CurrentProgress = BattleProgress.Prepare;
        InitLogicBattleSystem();
    }

    public static void StartFight()
    {
        CurrentProgress = BattleProgress.Fight;
        logicBattleSystem.StartFight();
    }

    private static void InitLogicBattleSystem()
    {
        BattleInputData inputData = new BattleInputData();
        inputData.timeScale = 1;
        inputData.attackerCampID = 1;
        inputData.defenderCampID = 2;
        inputData.attackerBasecamp = battleField.attackerBasecamp;
        inputData.defenderBasecamp = battleField.defenderBasecamp;
        logicBattleSystem.Initialize(inputData);
    }

    public static void Update()
    {
        if (CurrentProgress == BattleProgress.Fight)
        {
            //战斗数据帧
            logicBattleSystem.Update();

            //战斗显示帧
            bool isEnd = false;
            while (logicBattleSystem.outputDatasFrame.Count > 0)
            {
                BattleFrameOutputData frameData = logicBattleSystem.outputDatasFrame.Dequeue();
                ApplyAddBattleUnitFrameData(frameData);
                ApplyStatusFrameData(frameData);
                ApplyBulletFrameData(frameData);
                ApplySkillFrameData(frameData);

                if (frameData.isEnd)
                {
                    isEnd = true;
                }
                logicBattleSystem.RecycleBattleFrameData(frameData);
            }

            UpdateAttacker();
            UpdateDefender();
            UpdateBullet();
            UpdateSkill();
            if (isEnd)
            {
                EndBattle();
            }
        }
    }

    /// <summary>
    /// 开始结算
    /// </summary>
    public static void EndBattle()
    {
        CurrentProgress = BattleProgress.Settlement;
        ClearBullet();
        ClearSkill();
    }

    public static void AddSoldier(bool isAttacker, int id, int count)
    {
        if (logicBattleSystem.Fighting)
        {
            // 创建输入帧数据
            BattleFrameInputData frameData = new BattleFrameInputData();
            frameData.time = logicBattleSystem.battleTimerMS;
            
            // 添加生成士兵的数据
            frameData.addBattleUnitData = new AddBattleUnitData
            {
                campID = isAttacker ? 1 : 2,
                soldierID = id,
                count = count,
            };
            
            // 将帧数据加入到逻辑系统的输入队列
            logicBattleSystem.AddInputDataPerFrame(frameData);
        }
    }
}
