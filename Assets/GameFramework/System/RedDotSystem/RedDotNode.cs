using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameFramework
{
    public class RedDotNode
    {
        public string name;
        public RedDotNode parent;
        public List<RedDotNode> childList;
        public GameObject gameObject;
        public TMP_Text textComponent;
        public int count;

        public RedDotNode(string name, RedDotNode parent = null)
        {
            this.name = name;
            this.parent = parent;
            this.childList = null;
            this.gameObject = null;
            this.textComponent = null;
            this.count = 0;
        }

        public void Bind(Transform parent)
        {
            if (parent == null) return;

            Transform redDot = parent.RecursiveFindChild("RedDot");
            if (redDot != null)
            {
                this.gameObject = redDot.gameObject;
            }

            if (redDot == null)
            {
                string prefabName = name.Contains("_NUM") ? "RedDotNum" : "RedDot";
                var instance = Object.Instantiate(GameFramework.AssetSystem.Load<GameObject>(prefabName), parent)
                    .gameObject;
                instance.name = prefabName;
                RectTransform rtPoint = instance.GetComponent<RectTransform>();
                if (rtPoint == null)
                {
                    rtPoint = instance.AddComponent<RectTransform>();
                }

                if (rtPoint == null)
                {
                    GameObject.Destroy(instance);
                    return;
                }

                rtPoint.anchorMax = Vector2.one;
                rtPoint.anchorMin = Vector2.one;
                rtPoint.pivot = Vector2.one;
                rtPoint.anchoredPosition = Vector2.zero;
                this.gameObject = instance;
            }
            //查找红点下的显示数量的Text组件
            textComponent = gameObject.GetComponentInChildren<TMP_Text>();
            Refresh();
        }

        public void UnBind()
        {
            this.gameObject = null;
            this.textComponent = null;
        }

        public bool IsVisible()
        {
            return this.count > 0;
        }

        public RedDotNode AddChild(string namestr, RedDotNode parentGo = null)
        {
            if (childList == null)
            {
                childList = new List<RedDotNode>();
            }

            RedDotNode node = new RedDotNode(namestr, parentGo);
            childList.Add(node);
            return node;
        }

        public RedDotNode GetChild(string namestr)
        {
            if (childList != null)
            {
                for (int i = 0; i < childList.Count; i++)
                {
                    if (childList[i].name == namestr)
                    {
                        return childList[i];
                    }
                }
            }

            return null;
        }

        //设置红点显示状态
        public void SetVisible(bool visible)
        {
            if (IsVisible() == visible && (childList == null || childList.Count <= 0))
            {
                return;
            }

            if (visible)
            {
                count += 1;
            }
            else
            {
                count -= 1;
            }

            Refresh();
            if (this.parent != null)
            {
                this.parent.SetVisible(visible);
            }
        }

        //设置红点显示状态
        public void SetCount(int count)
        {
            bool visible = count > 0;
            if (IsVisible() == visible && (childList == null || childList.Count <= 0))
            {
                return;
            }

            this.count = count;

            Refresh();

            if (this.parent != null)
            {
                this.parent.SetVisible(visible);
            }
        }

        //刷新红点显示
        public void Refresh()
        {
            if (this.gameObject == null) return;

            if (this.textComponent != null)
            {
                int totalCount = GetTotalCount();
                this.textComponent.text = count >= 100 ? "99" : totalCount.ToString();
            }

            this.gameObject.SetActive(IsVisible());
        }

        private int GetTotalCount()
        {
            if (childList == null || childList.Count == 0)
            {
                return count;
            }
            else
            {
                int totalCount = count;
                foreach (var child in childList)
                {
                    totalCount += child.GetTotalCount();
                }
                return totalCount;
            }
        }
    }
}