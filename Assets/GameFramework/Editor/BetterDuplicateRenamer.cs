using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameFramework
{
    [InitializeOnLoad]
    public class BetterDuplicateRenamer
    {
        private const string PASTE_COMMAND = "Paste";
        private const string DUPLICATE_COMMAND = "Duplicate";

        public static System.Action<GameObject> OnGameObjectDuplicated;

        private static int previousObjectCount;
        private static string lastCommandName = "";

        private static readonly List<int> existingIDs = new List<int>();

        static BetterDuplicateRenamer()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnItemOnGUI;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnItemOnGUI(int instanceID, Rect selectionRect)
        {
            bool isCurrentCommandValid = Event.current.commandName == PASTE_COMMAND || Event.current.commandName == DUPLICATE_COMMAND;
            if (Event.current.type == EventType.ExecuteCommand && isCurrentCommandValid)
            {
                lastCommandName = Event.current.commandName;

                existingIDs.Clear();
                previousObjectCount = 0;

                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage)
                {
                    // 如果当前在Prefab编辑模式中，查找方式不一样

                    GameObject prefabRoot = prefabStage.prefabContentsRoot;
                    var allTransforms = prefabRoot.GetComponentsInChildren<RectTransform>();
                    previousObjectCount = allTransforms.Length;

                    for (int i = 0; i < allTransforms.Length; i++)
                    {
                        existingIDs.Add(allTransforms[i].gameObject.GetInstanceID());
                    }
                }
                else
                {
                    GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                    previousObjectCount = allObjects.Length;

                    for (int i = 0; i < allObjects.Length; i++)
                    {
                        existingIDs.Add(allObjects[i].GetInstanceID());
                    }
                }
            }
            else if (previousObjectCount > 0)
            {
                GameObject[] currentObjects = null;

                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage)
                {
                    // 如果当前在Prefab编辑模式中，查找方式不一样
                    GameObject prefabRoot = prefabStage.prefabContentsRoot;
                    var allTransforms = prefabRoot.GetComponentsInChildren<RectTransform>();
                    currentObjects = allTransforms.Select(x => x.gameObject).ToArray();
                }
                else
                {
                    currentObjects = GameObject.FindObjectsOfType<GameObject>();
                }

                if (previousObjectCount != currentObjects.Length)
                {
                    previousObjectCount = 0;
                    bool isLastCommandValid = (lastCommandName == PASTE_COMMAND || lastCommandName == DUPLICATE_COMMAND);
                    for (int i = 0; i < currentObjects.Length; i++)
                    {
                        if (!existingIDs.Contains(currentObjects[i].GetInstanceID()))
                        {
                            if (isLastCommandValid)
                            {
                                string objectName = currentObjects[i].name;
                                //Unity自动重命名后面可能会添加三种形式：(1) .1 _1
                                string regexValue = Regex.Match(objectName, @"\(\d+\)$|\.\d+$|_\d+$").Value;
                                string pureName = string.Empty;
                                if (!string.IsNullOrEmpty(regexValue))
                                {
                                    //先将Unity自动重命名添加的部分移除
                                    var removeDigitName = objectName.Replace(regexValue, "").TrimEnd();

                                    //如果移除完后面的数字，名字不再以数字结尾，那么保留移除前的名字
                                    regexValue = Regex.Match(removeDigitName, @"\d+$").Value;
                                    if (string.IsNullOrEmpty(regexValue))
                                    {
                                        pureName = objectName;
                                    }
                                    else
                                    {
                                        pureName = removeDigitName;
                                        currentObjects[i].name = pureName;
                                    }
                                }

                                if (!string.IsNullOrEmpty(pureName))
                                {
                                    //pureName 最后得是数字结尾才是要进行递增的物体
                                    regexValue = Regex.Match(pureName, @"\d+$").Value;
                                    if (!string.IsNullOrEmpty(regexValue))
                                    {
                                        //pureName 去掉最后的数字
                                        pureName = pureName.Replace(regexValue, string.Empty);
                                        //遍历所有物体，找出目前编号最大的，把新物体编号+1
                                        int digit = GetMaxNumber(pureName, currentObjects, currentObjects[i]);
                                        digit++;
                                        string digitString = digit.ToString();
                                        currentObjects[i].name = pureName + digitString;
                                    }
                                }
                            }

                            if (OnGameObjectDuplicated != null)
                            {
                                OnGameObjectDuplicated(currentObjects[i]);
                            }
                        }
                    }
                }
                else
                {
                    ResetAll();
                }
            }
        }

        private static int GetMaxNumber(string pureName, GameObject[] gameObjects, GameObject excludeObject)
        {
            int maxNumber = 0;
            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject == excludeObject)
                {
                    continue;
                }
                if (string.IsNullOrEmpty(pureName))
                {
                    //如果名字是个纯数字，那直接解析
                    if (int.TryParse(gameObject.name, out var leftNumber))
                    {
                        maxNumber = Mathf.Max(maxNumber, leftNumber);
                    }
                }
                else if (gameObject.name.StartsWith(pureName))
                {
                    string leftName = gameObject.name.Replace(pureName, string.Empty);
                    // 去掉要查找的名字后，剩下的如果是数字，才算是同一类物体d
                    if (int.TryParse(leftName, out var leftNumber))
                    {
                        maxNumber = Mathf.Max(maxNumber, leftNumber);
                    }
                }
            }

            return maxNumber;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange i_PlayModeState)
        {
            ResetAll();
        }

        private static void ResetAll()
        {
            previousObjectCount = 0;
            existingIDs.Clear();
            lastCommandName = "";
        }
    }
}