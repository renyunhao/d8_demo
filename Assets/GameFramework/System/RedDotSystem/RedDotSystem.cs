using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 红点系统
    /// </summary>
    public class RedDotSystem
    {
        // 存储节点信息
        private static Dictionary<string, List<string>> redDotDict = new Dictionary<string, List<string>>();
        private static RedDotNode root;
        private static GameObject prefab;
        private static GameObject prefabWithNum;
        
        public static GameObject Prefab => prefab;
        public static GameObject PrefabWithNum => prefabWithNum;

        public static void Initialize(GameObject prefab, GameObject prefabWithNum = null)
        {
            RedDotSystem.prefab = prefab;
            RedDotSystem.prefabWithNum = prefabWithNum;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <param name="isCountDown"></param>
        public static void Register(string key, Transform parent)
        {
            if (IsPrefabNull())
            {
                return;
            }
            RedDotNode node = GetNode(key);
            if (node != null)
            {
                node.Bind(parent);
            }
        }

        public static void UnRegister(string key)
        {
            if (IsPrefabNull())
            {
                return;
            }
            if (!IsNodeExist(key)) return;

            RedDotNode node = GetNode(key);
            if (node != null)
            {
                node.UnBind();
            }
        }

        /// <summary>
        /// 设置红点显示状态（只能设置叶子结点）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="visible"></param>
        public static void SetVisible(string key, bool visible)
        {
            SetCount(key, visible ? 1 : 0);
        }

        /// <summary>
        /// 设置红点显示状态（只能设置叶子结点）,通过修改数量值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="count">数量大于0显示，小等于0不显示</param>
        public static void SetCount(string key, int count)
        {
            if (IsPrefabNull())
            {
                return;
            }
            RedDotNode node = GetNode(key);
            if (node.childList != null && node.childList.Count > 0)
            {
                Debug.LogError($"SetVisible Error: 只能设置叶子节点的状态！key = {key}");
                return;
            }

            node.SetCount(count);
        }

        // 判断key对应的红点是否显示
        public static bool IsVisible(string key)
        {
            if (IsPrefabNull())
            {
                return false;
            }
            if (IsNodeExist(key))
            {
                RedDotNode node = GetNode(key);
                return node.IsVisible();
            }

            return false;
        }

        private static bool IsPrefabNull()
        {
            bool isNull = prefab == null;
            if (isNull)
            {
                Debug.LogError("请先调用RedDotSystem.Initialize");
            }
            return isNull;
        }

        private static bool IsAnyPrefabNull()
        {
            bool isNull = prefab == null || prefabWithNum == null;
            if (isNull)
            {
                Debug.LogError("请先调用RedDotSystem.Initialize");
            }
            return isNull;
        }

        // 获取节点
        private static RedDotNode GetNode(string key)
        {
            if (root == null)
            {
                root = new RedDotNode("Root");
            }

            var keyList = ParseKey(key);
            var node = root;
            for (int i = 0; i < keyList.Count; i++)
            {
                var childNode = node.GetChild(keyList[i]);
                if (childNode == null)
                {
                    childNode = node.AddChild(keyList[i], node);
                }

                node = childNode;
            }

            return node;
        }

        // 节点是否存在
        private static bool IsNodeExist(string key)
        {
            return redDotDict.ContainsKey(key);
        }

        private static List<string> ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("ParseKey Error: Key不能为空!");
                return null;
            }

            List<string> keyList = null;
            if (!redDotDict.TryGetValue(key, out keyList))
            {
                redDotDict[key] = key.Split('.').ToList();
                keyList = redDotDict[key];
            }

            return keyList;
        }
    }
}