using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using Debug = GameFramework.Debug;

//本项目中，由于旧模块的存在且被大量使用，这个模块暂不替换，延用旧模块代码
namespace GameFramework
{
    public static class UISystem
    {
        public static event Action<BaseUI> Event_UICreated;
        public static event Action<BaseUI> Event_UIWillShow;
        public static event Action<BaseUI> Event_UIShow;
        public static event Action<BaseUI> Event_UIHide;

        /// <summary>
        /// 需要外部指定一个加载UI预设的方法
        /// </summary>
        private static event Func<string, GameObject> PrefabProvider;

        /// <summary>
        /// 初始SortingOrder层级
        /// </summary>
        private static readonly int DefaultSortingOrder = 1000;
        /// <summary>
        /// 所有已经创建过UI实例字典（其中的UI实例不一定处于显示状态）
        /// </summary>
        private static Dictionary<string, BaseUI> UICacheDic = new Dictionary<string, BaseUI>();
        /// <summary>
        /// 所有处于显示状态的UI集合（双向链表结构）
        /// </summary>
        private static LinkedList<BaseUI> UIList = new LinkedList<BaseUI>();
        /// <summary>
        /// Key:UIName,Value:sortingLayer
        /// </summary>
        private static Dictionary<string, string> SortingLayerMap = new Dictionary<string, string>();
        private static Dictionary<string, int> SortingOrderMap = new Dictionary<string, int>();
        private static Dictionary<int, string> IDMap = new Dictionary<int, string>();
        private static Transform UIRootCached;
        private static int DefaultSortingLayerID;

        private static List<string> mIgnoreUIs = new List<string>{ "GuideUI" };
        /// <summary>
        /// UI 根节点
        /// </summary>
        public static Transform UIRoot
        {
            get
            {
                if (UIRootCached == null)
                {
                    var obj = GameObject.Find(nameof(UIRoot));
                    if (obj != null)
                    {
                        UIRootCached = obj.transform;
                    }
                }
                if (UIRootCached == null)
                {
                    UIRootCached = new GameObject("UIRoot").transform;
                }
                return UIRootCached;
            }
        }

        /// <summary>
        /// UI摄像机
        /// </summary>
        public static Camera UICamera { get; private set; }

        /// <summary>
        /// 当前界面最上层的UI
        /// </summary>
        public static BaseUI TopUI
        {
            get
            {
                return UIList.Count > 0 ? UIList.Last.Value : null;
            }
        }
        /// <summary>
        /// UI设计宽度
        /// </summary>
        public static int DesignWidth
        {
            get;
            set;
        }
        /// <summary>
        /// UI设计高度
        /// </summary>
        public static int DesignHeight
        {
            get;
            set;
        }

        public static bool CheckIsIgnoreUI(string name)
        {
            return mIgnoreUIs.Contains(name);
        }

        #region Public Methods

        public static void Initialize(Camera uiCamera, int defaultSortingLayer, int designWidth, int designHeight, Func<string, GameObject> prefabProvider)
        {
            Clear();
            UICamera = uiCamera;
            DefaultSortingLayerID = defaultSortingLayer;
            DesignWidth = designWidth;
            DesignHeight = designHeight;
            PrefabProvider = prefabProvider;
        }

        public static void SpecifyUISortingLayer<T>(string layerName) where T : BaseUI
        {
            string name = GetUIName<T>();
            if (SortingLayerMap.ContainsKey(name) == false)
            {
                SortingLayerMap.Add(name, layerName);
            }
            else
            {
                Debug.LogError(string.Format("重复的预定义-{0}-{1}", name, layerName));
            }
        }

        public static void SpecifyUISortingOrder<T>(int order) where T : BaseUI
        {
            string name = GetUIName<T>();
            if (SortingOrderMap.ContainsKey(name) == false)
            {
                SortingOrderMap.Add(name, order);
            }
            else
            {
                Debug.LogError(string.Format("重复的预定义-{0}-{1}", name, order));
            }
            SpecifyUISortingLayer<T>("UI");
        }

        public static void SpecifyUI_ID<T>(int ID) where T : BaseUI
        {
            string name = GetUIName<T>();
            if (IDMap.ContainsKey(ID) == false)
            {
                IDMap.Add(ID, name);
            }
            else
            {
                Debug.LogError(string.Format("重复的预定义-{0}-{1}", name, ID));
            }
        }

        public static Canvas CreateCanvas(string name, int layer, string sortingLayerName, int sortingOrder, bool addGraphicRaycaster = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(UIRoot);
            go.layer = layer;

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UICamera;
            canvas.planeDistance = 0;
            canvas.sortingLayerName = sortingLayerName;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (addGraphicRaycaster)
            {
                go.AddComponent<GraphicRaycaster>();
            }
            return canvas;
        }

        public static T Show<T>() where T : BaseUI
        {
            string name = GetUIName<T>();
            if (TopUI != null && TopUI.name == name)
            {
                // 正常来说，如果顶部UI已经是要显示的UI了，没必要再调用一次Show
                //Show(UICacheDic[name]);
                return TopUI as T;
            }
            else
            {
                BaseUI ui;
                if (UICacheDic.ContainsKey(name))
                {
                    ui = UICacheDic[name];
                }
                else
                {
                    ui = Create(name);
                }

                if (ui != null)
                {
                    Show(ui);
                }
                return ui as T;
            }
        }

        public static T Show<T>(ITuple param) where T : BaseUI
        {
            string name = GetUIName<T>();
            if (TopUI != null && TopUI.name == name)
            {
                return TopUI as T;
            }
            else
            {
                BaseUI ui;
                if (UICacheDic.ContainsKey(name))
                {
                    ui = UICacheDic[name];
                }
                else
                {
                    ui = Create(name);
                }


                if (ui != null)
                {
                    Show(ui, param);
                }
                return ui as T;
            }
        }

        public static BaseUI Show(int ID)
        {
            string name = GetUIName(ID);
            if (TopUI != null && TopUI.name == name)
                return TopUI;
            
            BaseUI ui;
            if (UICacheDic.ContainsKey(name))
                ui = UICacheDic[name];
            else
                ui = Create(name);

            if (ui != null)
                Show(ui);
            return ui;
        }

        public static T Hide<T>() where T : BaseUI
        {
            string name = GetUIName<T>();
            if (TopUI == null)
            {
                return TopUI as T;
            }
            if (TopUI.Name == name)
            {
                return Hide(TopUI) as T;
            }
            else
            {
                if (UICacheDic.ContainsKey(name))
                {
                    return Hide(UICacheDic[name]) as T;
                }
                else
                {
                    Debug.LogError("试图Hide不在实例字典中的UI: " + name + "最上层的UI: " + TopUI.Name);
                    return default(T);
                }
            }
        }

        public static bool IsOpen<T>() where T : BaseUI
        {
            string uiName = GetUIName<T>();
            if (UIList.Count > 0)
            {
                foreach (BaseUI ui in UIList)
                {
                    if (ui.Name == uiName)
                        return true;
                }
            }
            return false;
        }

        public static T Get<T>() where T : BaseUI
        {
            string uiName = GetUIName<T>();
            if (UIList.Count > 0)
            {
                foreach (BaseUI ui in UIList)
                {
                    if (ui.Name == uiName)
                        return ui as T;
                }
            }
            return default;
        }

        public static void Destory<T>()
        {
            string uiName = GetUIName<T>();
            if (UICacheDic.TryGetValue(uiName, out var ui))
            {
                UnityEngine.Object.Destroy(UICacheDic[uiName].gameObject);
                UICacheDic.Remove(uiName);
                UIList.Remove(ui);
            }
        }

        public static void BackwardTo(BaseUI ui)
        {
            if (UIList.Contains(ui))
            {
                while (TopUI != ui)
                {
                    Hide(TopUI);
                }
            }
            else
            {
                Debug.LogError("回退目标不存在: " + ui.Name);
            }
        }

        public static void BackwardTo<T>() where T : BaseUI
        {
            string uiName = GetUIName<T>();
            if (UICacheDic.ContainsKey(uiName))
            {
                BackwardTo(UICacheDic[uiName]);
            }
            else
            {
                Debug.LogError("回退目标不存在: " + uiName);
            }
        }

        public static void HideAll()
        {
            while (TopUI != null)
            {
                Hide(TopUI);
            }
        }

        public static void HideAllExceptFirst()
        {
            while (TopUI != null && UIList.Count > 1)
            {
                Hide(TopUI);
            }
        }

        #endregion

        private static void Clear()
        {
            UICacheDic.Clear();
            UIList.Clear();
        }

        private static BaseUI Create(string name)
        {
            GameObject prefab = PrefabProvider.Invoke(name);
            if (prefab != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab, UIRoot);
                go.name = name;

                Canvas canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = UICamera;
                canvas.planeDistance = 0;
                CanvasScaler canvasScaler = go.GetComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                BaseUI ui = go.GetComponent<BaseUI>();
                ui.Name = name;
                ui.UICanvas = canvas;
                if (SortingLayerMap.ContainsKey(ui.Name))
                {
                    ui.UICanvas.sortingLayerName = SortingLayerMap[ui.Name];
                }
                else
                {
                    ui.UICanvas.sortingLayerID = DefaultSortingLayerID;
                }
                UICacheDic.Add(name, ui);
                ui.OnCreate();
                Event_UICreated?.Invoke(ui);
                return ui;
            }
            else
            {
                Debug.LogError("要创建的UI不存在: " + name);
                return null;
            }
        }

        private static void Show(BaseUI ui)
        {
            Debug.Log($"UISystem Show: {ui.Name}");
            if(!UIList.Contains(ui))
                UIList.AddLast(ui);
            ui.gameObject.SetActive(true);
            if (SortingOrderMap.TryGetValue(ui.Name, out var order))
            {
                ui.UICanvas.sortingOrder = order;
            }
            else
            {
                Debug.LogWarning($"UI {ui.Name} 没有手动指定层级，请检查是否有必要指定");
                ui.UICanvas.sortingOrder = GetTopUISortingOrder(ui.UICanvas.sortingLayerID) + 100;
            }
            ui.OnShow();
            Event_UIShow?.Invoke(ui);
        }

        private static void Show(BaseUI ui, ITuple param)
        {
            Debug.Log($"UISystem Show: {ui.Name}");
            Event_UIWillShow?.Invoke(ui);
            if(!UIList.Contains(ui))
                UIList.AddLast(ui);
            ui.gameObject.SetActive(true);
            if (SortingOrderMap.TryGetValue(ui.Name, out var order))
            {
                ui.UICanvas.sortingOrder = order;
            }
            else
            {
                ui.UICanvas.sortingOrder = GetTopUISortingOrder(ui.UICanvas.sortingLayerID) + 100;
            }
            ui.OnShow(param);
            Event_UIShow?.Invoke(ui);
        }

        public static BaseUI Hide(BaseUI ui)
        {
            Debug.Log($"UISystem Hide: {ui.Name}");
            if (TopUI == ui)
            {
                UIList.RemoveLast();
            }
            else
            {
                if (UIList.Contains(ui))
                {
                    //如果头结点，更新后一个UI的PreviousUI为空
                    if (UIList.Count > 0)
                    {
                        UIList.Remove(ui);
                    }
                }
            }
            ui.gameObject.SetActive(false);
            ui.OnHide();
            Event_UIHide?.Invoke(ui);
            return ui;
        }

        private static string GetUIName<T>()
        {
            string name = typeof(T).Name;
            return name.Replace("Ctrl", "").Trim();
        }

        private static string GetUIName(int ID)
        {
            if (IDMap.TryGetValue(ID, out string uiName))
                return uiName;
            Debug.LogError($"没有定义这个ID:{ID}");
            return null;
        }

        private static int GetTopUISortingOrder(int sortingLayerID)
        {
            int order = DefaultSortingOrder;
            if (UIList.Count > 0)
            {
                foreach (BaseUI ui in UIList)
                {
                    if (!CheckIsIgnoreUI(ui.Name) && ui.UICanvas.sortingLayerID == sortingLayerID)
                    {
                        if (order < ui.UICanvas.sortingOrder)
                        {
                            order = ui.UICanvas.sortingOrder;
                        }
                    }
                }
            }
            return order;
        }

        public static void LanguageChangeHandler()
        {
            //当语言变化的时候，将当前所有打开的页面的重新调用一次OnLanguageChange，使界面刷新文本
            foreach (var ui in UIList)
            {
                ui.OnLanguageChange();
            }
        }
    }
}