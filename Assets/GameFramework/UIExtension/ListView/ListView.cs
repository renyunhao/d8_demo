using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFramework
{
    [RequireComponent(typeof(ScrollRect))]
    public class ListView : MonoBehaviour, IEndDragHandler, IBeginDragHandler
    {
        public delegate void CreateScrollViewItem(GameObject itemGameObject);
        public delegate void RefreshScrollViewItem(GameObject itemGameObject, int index);
        public delegate Vector2 CalculateItemSize(int index);
        public delegate void ChildCenterOn(int index);

        public enum MoveDirection
        {
            Forward,
            Backward
        }

        #region Fields

        /// <summary>
        /// GalleryMode下Item的最小缩放值
        /// </summary>
        public const float SCALE_MIN_SIZE = 0.2f;
        /// <summary>
        /// 创建Item实例事件
        /// </summary>
        public event CreateScrollViewItem Event_OnCreateScrollViewItem;
        /// <summary>
        /// 刷新Item数据事件
        /// </summary>
        public event RefreshScrollViewItem Event_OnRefreshScrollViewItem;
        /// <summary>
        /// 隐藏Item数据事件
        /// </summary>
        public event RefreshScrollViewItem Event_OnHideScrollViewItem;
        /// <summary>
        /// 计算ItemSize事件，初始化就需要生成大小不同的预制时使用
        /// </summary>
        public event CalculateItemSize Event_OnCalculateItemSize;
        /// <summary>
        /// 元素居中事件
        /// </summary>
        public event ChildCenterOn Event_OnChildCenterOn;
        /// <summary>
        /// 列表滚动方向
        /// </summary>
        public Slider.Direction direction = Slider.Direction.LeftToRight;

        /// <summary>
        /// 元素的间距
        /// </summary>
        [SerializeField]
        private Vector2 spacing = Vector2.zero;
        /// <summary>
        /// 边界填充
        /// </summary>
        [SerializeField]
        private RectOffset padding = new RectOffset();
        /// <summary>
        /// 当Content尺寸小于ViewPort范围时，元素布局的填充值，不参与Content尺寸的计算，只影响元素的坐标
        /// </summary>
        private RectOffset layoutPadding = new RectOffset();
        /// <summary>
        /// 可以显示的Item的总数
        /// </summary>
        [SerializeField]
        private int dataCount;
        /// <summary>
        /// Item预制体
        /// </summary>
        [SerializeField]
        private GameObject itemPrefab = null;
        /// <summary>
        /// 是否使用图库模式
        /// </summary>
        [SerializeField]
        private bool useGalleryMode = false;
        /// <summary>
        /// 图库模式的缩放曲线
        /// </summary>
        [SerializeField]
        private AnimationCurve galleryItemScaleCurve = AnimationCurve.Linear(0, 1, 0, 1);
        /// <summary>
        /// 是否自动居中元素
        /// </summary>
        [SerializeField]
        private bool centerOnChild = false;
        /// <summary>
        /// 重新设置子元素的节点层级关系
        /// </summary>
        [SerializeField]
        private bool resortSibling = false;
        /// <summary>
        /// 是否按照索引大小降序
        /// </summary>
        [SerializeField]
        private bool ascendingOrder = false;
        /// <summary>
        /// 行数
        /// </summary>
        [SerializeField]
        private int row = 1;
        /// <summary>
        /// 列数
        /// </summary>
        [SerializeField]
        private int column = 1;
        /// <summary>
        /// 是否自动初始化，默认为true，当组件OnAwake的时候，就会自动初始化
        /// 如果开发者需要在特定时机初始化，可将此值改为false，到适当的时机手动调用Initialize
        /// </summary>
        [SerializeField]
        private bool autoInitialize = true;
        /// <summary>
        /// 计算Item位置的时候，是否使用Item的轴点进行偏移
        /// </summary>
        [SerializeField]
        private bool calculatePositionWithItemPivot = false;
        /// <summary>
        /// 当内容的尺寸小于视野范围时，自动将内容整体居中显示（其实就是自动计算padding）
        /// 内容超出视野范围时，无效
        /// </summary>
        [SerializeField]
        private bool centerLayout = false;

        /// <summary>
        /// 正在使用的Item实例列表
        /// </summary>
        public SortedList<int, ListViewItem> usingItemList = new SortedList<int, ListViewItem>();
        /// <summary>
        /// 列表内容改变时，新增的Item列表，其内容处于未初始化状态
        /// </summary>
        private Queue<ListViewItem> willUsingItemList = new Queue<ListViewItem>();
        /// <summary>
        /// 被回收的Item实例列表
        /// </summary>
        private Queue<ListViewItem> recycledItemList = new Queue<ListViewItem>();
        /// <summary>
        /// 每一个数据索引的Item位置
        /// </summary>
        public Dictionary<int, Vector2> itemPositionDic = new Dictionary<int, Vector2>();
        /// <summary>
        /// 每一个数据索引的Item尺寸
        /// </summary>
        public Dictionary<int, Vector2> itemSizeDic = new Dictionary<int, Vector2>();
        /// <summary>
        /// ItemPrefab的RectTranform组件
        /// </summary>
        private RectTransform prefabRectTransform;
        /// <summary>
        /// Item的默认大小，以预制体为准
        /// </summary>
        private Vector2 itemSize = Vector2.zero;
        /// <summary>
        /// 视野范围
        /// </summary>
        private Vector2 viewPortSize = Vector2.zero;
        /// <summary>
        /// 是否支持Item改变大小，多行多列下不支持
        /// </summary>
        private bool supportChangeSize = false;
        /// <summary>
        /// 移动方向，Foward代表向尾部前进，Backward代表向头部前进
        /// </summary>
        private MoveDirection moveDirection = MoveDirection.Forward;
        /// <summary>
        /// 上一帧的ScrollRect Content偏移百分比值
        /// </summary>
        private Vector2 lastScrollRectPercent;
        /// <summary>
        /// 上一帧的Content位置
        /// </summary>
        private Vector2 lastContentPosition;
        /// <summary>
        /// 预设资源的名称，避免每次调用Object.name引起的GC
        /// </summary>
        private string prefabName;
        /// <summary>
        /// 标记是否正在预览，预览过程中生成的gameObject是不显示在Hierachy中，且不会保存下来的
        /// </summary>
        private bool isPreviewing = false;
        /// <summary>
        /// 标记是否有DOTween动画正在移动位置
        /// </summary>
        private bool isTweenerMoving = false;
        #endregion

        #region Properties

        /// <summary>
        /// 整个WrapContent是否初始化完成
        /// </summary>
        public bool IsInitailized { get; private set; } = false;
        /// <summary>
        /// 滚动列表的Content
        /// </summary>
        public RectTransform Content { get; set; }
        /// <summary>
        /// UGUI的原生滚动列表组件
        /// </summary>
        public ScrollRect ScrollRect { get; set; }
        /// <summary>
        /// 当前居中的元素索引
        /// </summary>
        public int CenterOnChildIndex { get; set; }
        /// <summary>
        /// 元素列表
        /// </summary>
        public IList<ListViewItem> ItemList => usingItemList.Values;
        /// <summary>
        /// 当前Content偏移值
        /// </summary>
        public Vector2 ContentOffset => Content.anchoredPosition;
        /// <summary>
        /// 当前的滚动方向
        /// </summary>
        public MoveDirection MovingDirection => moveDirection;

        public Vector2 Spacing => spacing;

        public RectOffset Padding => padding;

        public Vector2 ItemSize => itemSize;

        public int Column => column;
        #endregion

        #region Monobehaviour Life Cycle
        void Awake()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }
        #endregion

        #region Public Method

        /// <summary>
        /// 初始化滚动列表，可由外部调用初始化，也可以由组件自动初始化（前提是autoInitialize为true）
        /// </summary>
        public void Initialize()
        {
            if (IsInitailized)
            {
                return;
            }
            dataCount = -1;
            IsInitailized = true;

            ScrollRect = GetComponent<ScrollRect>();
            ScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
            RefreshViewportSize();
            Content = ScrollRect.content;
            Content.anchoredPosition = Vector2.zero;
            InitializeContentAnchorAndPivot();

            prefabRectTransform = itemPrefab.GetComponent<RectTransform>();
            itemSize = new Vector2(prefabRectTransform.rect.width, prefabRectTransform.rect.height);
            prefabName = itemPrefab.name;
            InitializePrefabAnchoredPosition();
            if (string.IsNullOrEmpty(itemPrefab.scene.name) == false)
            {
                itemPrefab.SetActive(false);
            }

            CheckRowAndColumn();
            CheckCanChangeSize();
        }

        /// <summary>
        /// 刷新滚动列表的内容
        /// </summary>
        /// <param name="count">数据的数量</param>
        public void Refresh(int count, bool forceUpdateData = false)
        {
            if (count < 0)
            {
                return;
            }
            //由于UI预设的Canvas设置可能在运行时被修改，Viewport的尺寸在初始化获取的值可能已经变化了，因此每次刷新Item前都重新获取一下Viewport尺寸
            RefreshViewportSize();
            //数据长度发生变化时，无条件更新全部的缓存信息
            if (dataCount != count || forceUpdateData)
            {
                dataCount = count;
                ResetAllItemSizeDict(count);
                ResetContentSize();
                if (centerLayout)
                {
                    if (Content.rect.width <= viewPortSize.x && Content.rect.height <= viewPortSize.y)
                    {
                        if (direction == Slider.Direction.TopToBottom)
                        {
                            layoutPadding.left = (int)((viewPortSize.x - Content.rect.width) / 2);
                            layoutPadding.top = (int)((viewPortSize.y - Content.rect.height) / 2);
                        }
                        else if (direction == Slider.Direction.BottomToTop)
                        {
                            layoutPadding.left = (int)((viewPortSize.x - Content.rect.width) / 2);
                            layoutPadding.bottom = (int)((viewPortSize.y - Content.rect.height) / 2);
                        }
                        else if (direction == Slider.Direction.LeftToRight)
                        {
                            layoutPadding.left = (int)((viewPortSize.x - Content.rect.width) / 2);
                            layoutPadding.top = (int)((viewPortSize.y - Content.rect.height) / 2);
                        }
                        else if (direction == Slider.Direction.RightToLeft)
                        {
                            layoutPadding.right = (int)((viewPortSize.x - Content.rect.width) / 2);
                            layoutPadding.top = (int)((viewPortSize.y - Content.rect.height) / 2);
                        }
                    }
                    else
                    {
                        layoutPadding.left = 0;
                        layoutPadding.right = 0;
                        layoutPadding.top = 0;
                        layoutPadding.bottom = 0;
                    }
                }
                else
                {
                    layoutPadding.left = 0;
                    layoutPadding.right = 0;
                    layoutPadding.top = 0;
                    layoutPadding.bottom = 0;
                }
                ResetContentSize();
                ResetAllItemPosition();
                ResetContentPosition();
            }
            UpdateItemInViewRange(true);
            if (useGalleryMode)
            {
                for (int i = 0; i < 10; i++)
                {
                    UpdateItemInViewRangeForGallery();
                    UpdateItemInViewRange(false);
                }
            }
        }

        /// <summary>
        /// 将Index对应的Item位置调整到视图范围的起始/结束处(瞬间移动）
        /// </summary>
        /// <param name="index">Item索引</param>
        /// <param name="isBeginOrEnd">起始/结束</param>
        /// <returns></returns>
        public Vector2 MoveToTargetPosition(int index, bool isBeginOrEnd = true)
        {
            Vector2 pos = CalculateContentPositionByIndex(index, isBeginOrEnd);
            if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
            {
                Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, pos.y);
            }
            else
            {
                Content.anchoredPosition = new Vector2(pos.x, Content.anchoredPosition.y);
            }
            UpdateItemInViewRange(true);
            return Content.anchoredPosition;
        }

        /// <summary>
        /// 将Index对应的Item位置调整到视图范围的起始处(有滚动过程）
        /// </summary>
        /// <param name="index">Item索引</param>
        /// <param name="isBeginOrEnd">起始/结束</param>
        /// <returns></returns>
        public Tweener MoveToTargetPositionAnimated(int index, bool isBeginOrEnd = true)
        {
            Vector2 newPos;
            Vector2 pos = CalculateContentPositionByIndex(index, isBeginOrEnd);
            float time = 0.2f;
            if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
            {
                newPos = new Vector2(Content.anchoredPosition.x, pos.y);
                time = Mathf.Abs(newPos.y - Content.anchoredPosition.y) / viewPortSize.y * 4;
            }
            else
            {
                newPos = new Vector2(pos.x, Content.anchoredPosition.y);
                time = Mathf.Abs(newPos.x - Content.anchoredPosition.x) / viewPortSize.x * 4;
            }
            time = Mathf.Clamp(time, 0.1f, 0.2f);
            isTweenerMoving = true;
            return Content.DOAnchorPos(newPos, time).OnUpdate(MovingTweenerUpdate).SetUpdate(UpdateType.Late, true).OnComplete(MovingTweenerComplete);
        }

        /// <summary>
        /// 将Index对应的Item位置调整到视图范围的中间(瞬间移动）
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector2 MoveTargetToCenter(int index)
        {
            Vector2 offset = CalculateContentCenterOnPosition(index);
            if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
            {
                Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, offset.y);
            }
            else
            {
                Content.anchoredPosition = new Vector2(offset.x, Content.anchoredPosition.y);
            }
            if (useGalleryMode)
            {
                UpdateItemInViewRangeForGallery();
            }
            UpdateItemInViewRange(false);
            return Content.anchoredPosition;
        }

        /// <summary>
        /// 将Index对应的Item位置调整到视图范围的中间(有滚动过程）
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Tweener MoveTargetToCenterAnimated(int index)
        {
            Vector2 newPos;
            Vector2 offset = CalculateContentCenterOnPosition(index);
            float time = 0;
            if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
            {
                newPos = new Vector2(Content.anchoredPosition.x, offset.y);
                time = Mathf.Abs(newPos.y - Content.anchoredPosition.y) / viewPortSize.y;
            }
            else
            {
                newPos = new Vector2(offset.x, Content.anchoredPosition.y);
                time = Mathf.Abs(newPos.x - Content.anchoredPosition.x) / viewPortSize.x;
            }
            time = Mathf.Clamp(time, 0.05f, 0.5f);
            CenterOnChildIndex = index;

            foreach (ListViewItem item in usingItemList.Values)
            {
                if (item.Index == index)
                {
                    item.OnCentered();
                }
                else
                {
                    item.OnLoseCentered();
                }
            }
            Event_OnChildCenterOn?.Invoke(index);
            isTweenerMoving = true;
            return Content.DOAnchorPos(newPos, time).OnUpdate(MovingTweenerUpdate).SetUpdate(UpdateType.Late, true).OnComplete(MovingTweenerComplete);
        }

        #endregion

        #region Private Method

        /// <summary>
        /// 初始化Content锚点与轴点
        /// </summary>
        public void InitializeContentAnchorAndPivot()
        {
            if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
            {
                if (direction == Slider.Direction.BottomToTop)
                {
                    Content.anchorMin = Vector2.zero;
                    Content.anchorMax = Vector2.zero;
                    Content.pivot = new Vector2(0, 0f);
                }
                else
                {
                    Content.anchorMin = Vector2.up;
                    Content.anchorMax = Vector2.up;
                    Content.pivot = new Vector2(0, 1f);
                }
                ScrollRect.vertical = true;
                ScrollRect.horizontal = false;
            }
            else
            {
                if (direction == Slider.Direction.LeftToRight)
                {
                    Content.anchorMin = Vector2.up;
                    Content.anchorMax = Vector2.up;
                    Content.pivot = new Vector2(0, 1);
                }
                else
                {
                    Content.anchorMin = Vector2.one;
                    Content.anchorMax = Vector2.one;
                    Content.pivot = new Vector2(1f, 1);
                }
                ScrollRect.horizontal = true;
                ScrollRect.vertical = false;
            }
        }

        /// <summary>
        /// 初始化Item预制的锚点与位置
        /// </summary>
        private void InitializePrefabAnchoredPosition()
        {
            if (string.IsNullOrEmpty(prefabRectTransform.gameObject.scene.name))
            {
                RectTransform instanceTransform = Instantiate(prefabRectTransform);
                Vector2 originAnchoredPos = instanceTransform.anchoredPosition;
                instanceTransform.SetParent(this.Content.transform);
                instanceTransform.anchoredPosition = originAnchoredPos;
                Vector3 originPos = instanceTransform.position;
                switch (direction)
                {
                    case Slider.Direction.LeftToRight:
                    case Slider.Direction.TopToBottom:
                        instanceTransform.anchorMin = Vector2.up;
                        instanceTransform.anchorMax = Vector2.up;
                        break;
                    case Slider.Direction.RightToLeft:
                        instanceTransform.anchorMin = Vector2.one;
                        instanceTransform.anchorMax = Vector2.one;
                        break;
                    case Slider.Direction.BottomToTop:
                        instanceTransform.anchorMin = Vector2.zero;
                        instanceTransform.anchorMax = Vector2.zero;
                        break;
                    default:
                        break;
                }
                instanceTransform.position = originPos;
                DestroyImmediate(instanceTransform.gameObject);
            }
            else
            {
                Vector3 originPos = prefabRectTransform.position;
                Vector2 originAnchorMin = prefabRectTransform.anchorMin;
                Vector2 originAnchorMax = prefabRectTransform.anchorMax;
                switch (direction)
                {
                    case Slider.Direction.LeftToRight:
                    case Slider.Direction.TopToBottom:
                        prefabRectTransform.anchorMin = Vector2.up;
                        prefabRectTransform.anchorMax = Vector2.up;
                        break;
                    case Slider.Direction.RightToLeft:
                        prefabRectTransform.anchorMin = Vector2.one;
                        prefabRectTransform.anchorMax = Vector2.one;
                        break;
                    case Slider.Direction.BottomToTop:
                        prefabRectTransform.anchorMin = Vector2.zero;
                        prefabRectTransform.anchorMax = Vector2.zero;
                        break;
                    default:
                        break;
                }
                prefabRectTransform.position = originPos;
                prefabRectTransform.anchorMin = originAnchorMin;
                prefabRectTransform.anchorMax = originAnchorMax;
                prefabRectTransform.position = originPos;
            }
        }

        /// <summary>
        /// 初始化Item的锚点
        /// </summary>
        private void InitializeItemAnchor(ListViewItem item)
        {
            RectTransform itemTransform = item.GetComponent<RectTransform>();
            Vector3 tempPos = itemTransform.position;
            switch (direction)
            {
                case Slider.Direction.LeftToRight:
                case Slider.Direction.TopToBottom:
                    itemTransform.anchorMin = Vector2.up;
                    itemTransform.anchorMax = Vector2.up;
                    break;
                case Slider.Direction.RightToLeft:
                    itemTransform.anchorMin = Vector2.one;
                    itemTransform.anchorMax = Vector2.one;
                    break;
                case Slider.Direction.BottomToTop:
                    itemTransform.anchorMin = Vector2.zero;
                    itemTransform.anchorMax = Vector2.zero;
                    break;
                default:
                    break;
            }
            itemTransform.position = tempPos;
        }

        /// <summary>
        /// 重置ItemSize字典
        /// </summary>
        /// <param name="count"></param>
        private void ResetAllItemSizeDict(int count)
        {
            itemSizeDic.Clear();
            for (int i = 0; i < count; i++)
            {
                Vector2 size = itemSize;
                if (supportChangeSize)
                {
                    //在支持动态改变大小的情况下才会触发计算大小事件
                    if (Event_OnCalculateItemSize != null)
                    {
                        size = Event_OnCalculateItemSize(i);
                    }
                }
                itemSizeDic.Add(i, size);
            }
        }

        /// <summary>
        /// 重置ItemPosition字典
        /// </summary>
        private void ResetAllItemPosition()
        {
            itemPositionDic.Clear();
            if (row > 1 || column > 1)
            {
                //多行多列暂只支持ItemSize不变的布局
                if (direction == Slider.Direction.TopToBottom)
                {
                    for (int i = 0; i < dataCount; i++)
                    {
                        float position_x = i % column * (itemSize.x + spacing.x) + padding.left + prefabRectTransform.pivot.x * itemSize.x + layoutPadding.left;
                        float position_y = i / column * (itemSize.y + spacing.y) + padding.top + (1 - prefabRectTransform.pivot.y) * itemSize.y + layoutPadding.top;
                        itemPositionDic.Add(i, new Vector2(position_x, -position_y));
                    }
                }
                else if (direction == Slider.Direction.BottomToTop)
                {
                    for (int i = 0; i < dataCount; i++)
                    {
                        float position_x = i % column * (itemSize.x + spacing.x) + padding.left + prefabRectTransform.pivot.x * itemSize.x + layoutPadding.left;
                        float position_y = i / column * (itemSize.y + spacing.y) + padding.bottom + prefabRectTransform.pivot.y * itemSize.y + layoutPadding.bottom;
                        itemPositionDic.Add(i, new Vector2(position_x, position_y));
                    }
                }
                else if (direction == Slider.Direction.LeftToRight)
                {
                    for (int i = 0; i < dataCount; i++)
                    {
                        float position_x = i / row * (itemSize.x + spacing.x) + padding.left + prefabRectTransform.pivot.x * itemSize.x + layoutPadding.left;
                        float position_y = -(1 - prefabRectTransform.pivot.y) * itemSize.y - (i % row * (itemSize.y + spacing.y) + padding.top + layoutPadding.top);
                        itemPositionDic.Add(i, new Vector2(position_x, position_y));
                    }
                }
                else if (direction == Slider.Direction.RightToLeft)
                {
                    for (int i = 0; i < dataCount; i++)
                    {
                        float position_x = i / row * (itemSize.x + spacing.x) + padding.right + prefabRectTransform.pivot.x * itemSize.x + layoutPadding.right;
                        float position_y = -(1 - prefabRectTransform.pivot.y) * itemSize.y - (i % row * (itemSize.y + spacing.y) + padding.top + layoutPadding.top);
                        itemPositionDic.Add(i, new Vector2(-position_x, position_y));
                    }
                }
            }
            else
            {
                if (direction == Slider.Direction.BottomToTop)
                {
                    float size_y = 0;
                    for (int i = 0; i < dataCount; i++)
                    {
                        if (i == 0)
                        {
                            size_y += padding.bottom + layoutPadding.bottom + itemSizeDic[0].y * prefabRectTransform.pivot.y;
                        }
                        else
                        {
                            size_y += spacing.y + itemSizeDic[i - 1].y * (1 - prefabRectTransform.pivot.y) + itemSizeDic[i].y * prefabRectTransform.pivot.y;
                        }

                        float xOffset = padding.left + layoutPadding.left;
                        if (calculatePositionWithItemPivot)
                        {
                            xOffset += itemSizeDic[i].x * prefabRectTransform.pivot.x;
                        }
                        else
                        {
                            xOffset += viewPortSize.x / 2;
                        }

                        itemPositionDic[i] = new Vector2(xOffset, size_y);
                    }
                }
                else if (direction == Slider.Direction.TopToBottom)
                {
                    float size_y = 0;
                    for (int i = 0; i < dataCount; i++)
                    {
                        if (i == 0)
                        {
                            size_y -= padding.top + layoutPadding.top + itemSizeDic[0].y * (1 - prefabRectTransform.pivot.y);
                        }
                        else
                        {
                            size_y -= spacing.y + itemSizeDic[i - 1].y * prefabRectTransform.pivot.y + itemSizeDic[i].y * (1 - prefabRectTransform.pivot.y);
                        }

                        float xOffset = padding.left + layoutPadding.left;
                        if (calculatePositionWithItemPivot)
                        {
                            xOffset += itemSizeDic[i].x * prefabRectTransform.pivot.x;
                        }
                        else
                        {
                            xOffset += viewPortSize.x / 2;
                        }

                        itemPositionDic[i] = new Vector2(xOffset, size_y);
                    }
                }
                else if (direction == Slider.Direction.LeftToRight)
                {
                    float size_x = 0;
                    for (int i = 0; i < dataCount; i++)
                    {
                        if (i == 0)
                        {
                            size_x += padding.left + layoutPadding.left + itemSizeDic[0].x * prefabRectTransform.pivot.x;
                        }
                        else
                        {
                            size_x += spacing.x + itemSizeDic[i - 1].x * (1 - prefabRectTransform.pivot.x) + itemSizeDic[i].x * prefabRectTransform.pivot.x;
                        }

                        float yOffset = -padding.top - layoutPadding.top;
                        if (calculatePositionWithItemPivot)
                        {
                            yOffset -= itemSizeDic[i].y * (1 - prefabRectTransform.pivot.y);
                        }
                        else
                        {
                            yOffset -= viewPortSize.y / 2;
                        }

                        itemPositionDic[i] = new Vector2(size_x, yOffset);
                    }
                }
                else if (direction == Slider.Direction.RightToLeft)
                {
                    float size_x = 0;
                    for (int i = 0; i < dataCount; i++)
                    {
                        if (i == 0)
                        {
                            size_x -= padding.right + layoutPadding.right + itemSizeDic[0].x * (1 - prefabRectTransform.pivot.x);
                        }
                        else
                        {
                            size_x -= spacing.x + itemSizeDic[i - 1].x * prefabRectTransform.pivot.x + itemSizeDic[i].x * (1 - prefabRectTransform.pivot.x);
                        }

                        float yOffset = -padding.top - layoutPadding.top;
                        if (calculatePositionWithItemPivot)
                        {
                            yOffset -= itemSizeDic[i].y * (1 - prefabRectTransform.pivot.y);
                        }
                        else
                        {
                            yOffset -= viewPortSize.y / 2;
                        }

                        itemPositionDic[i] = new Vector2(size_x, yOffset);
                    }
                }
            }
        }

        /// <summary>
        /// 重新计算Content的Size
        /// </summary>
        private void ResetContentSize()
        {
            //Vertical
            if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
            {
                float size_x = 0;
                float size_y = 0;
                if (supportChangeSize == false)
                {
                    int xItemCount = dataCount <= column ? dataCount : column;
                    size_x = itemSize.x * xItemCount + spacing.x * (xItemCount - 1) + padding.left + padding.right;
                    size_y = itemSize.y * Mathf.CeilToInt((float)dataCount / column) + spacing.y * (Mathf.CeilToInt((float)dataCount / column) - 1) + padding.top + padding.bottom;
                }
                else
                {
                    float paddingSize = padding.left + padding.right;
                    foreach (var item in itemSizeDic.Keys)
                    {
                        float newSizeX = paddingSize + itemSizeDic[item].x;
                        size_x = Mathf.Max(size_x, newSizeX);
                        size_y += itemSizeDic[item].y;
                    }
                    size_y += spacing.y * (dataCount - 1) + padding.top + padding.bottom;
                }
                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size_x);
                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size_y);
            }
            //Horizontical
            else
            {
                float size_x = 0;
                float size_y = 0;
                if (supportChangeSize == false)
                {
                    int yItemCount = dataCount <= row ? dataCount : row;
                    size_x = itemSize.x * Mathf.CeilToInt((float)dataCount / row) + spacing.x * (Mathf.CeilToInt((float)dataCount / row) - 1) + padding.left + padding.right;
                    size_y = itemSize.y * yItemCount + spacing.y * (yItemCount - 1) + padding.top + padding.bottom;
                }
                else
                {
                    float paddingSize = padding.left + padding.right;
                    foreach (var item in itemSizeDic.Keys)
                    {
                        size_x += itemSizeDic[item].x;
                        float newSizeY = paddingSize + itemSizeDic[item].y;
                        size_y = Mathf.Max(size_y, newSizeY);
                    }
                    size_x += spacing.x * (dataCount - 1) + padding.left + padding.right;
                }
                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size_x);
                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size_y);
            }
        }

        /// <summary>
        /// 重置Content位置到起始位置
        /// </summary>
        private void ResetContentPosition()
        {
            float coordinate = 0;
            if (direction == Slider.Direction.TopToBottom)
            {
                if (Content.rect.height > viewPortSize.y)
                {
                    coordinate = Mathf.Clamp(Content.anchoredPosition.y, 0, Content.rect.height - viewPortSize.y);
                }
                Content.anchoredPosition = new Vector2(0, coordinate);
            }
            else if (direction == Slider.Direction.BottomToTop)
            {
                if (Content.rect.height > viewPortSize.y)
                {
                    coordinate = Mathf.Clamp(Content.anchoredPosition.y, viewPortSize.y - Content.rect.height, 0);
                }
                Content.anchoredPosition = new Vector2(0, coordinate);
            }
            else if (direction == Slider.Direction.LeftToRight)
            {
                if (Content.rect.width > viewPortSize.x)
                {
                    coordinate = Mathf.Clamp(0, viewPortSize.x - Content.rect.width, 0);
                }
                Content.anchoredPosition = new Vector2(coordinate, Content.anchoredPosition.y);
            }
            else
            {
                if (Content.rect.width > viewPortSize.x)
                {
                    coordinate = Mathf.Clamp(Content.anchoredPosition.x, 0, Content.rect.width - viewPortSize.x);
                }
                Content.anchoredPosition = new Vector2(coordinate, 0);
            }
        }

        /// <summary>
        /// 检查是否可以动态的改变大小(在多行多列的情况下不支持Item大小变化)
        /// </summary>
        /// <returns></returns>
        private void CheckCanChangeSize()
        {
            if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
            {
                supportChangeSize = column <= 1;
            }
            else
            {
                supportChangeSize = row <= 1;
            }
        }

        /// <summary>
        /// 检查行列数，最少为1
        /// </summary>
        private void CheckRowAndColumn()
        {
            if (row < 1)
            {
                row = 1;
            }
            if (column < 1)
            {
                column = 1;
            }
        }

        /// <summary>
        /// 更新Item实例数量
        /// </summary>
        private void UpdateItemInstanceCount(int count)
        {
            if (count < 0)
            {
                return;
            }
            //实例多出的时候将多出的回收
            if (usingItemList.Count > count)
            {
                while (usingItemList.Count > count)
                {
                    //根据当前移动方向从头部或尾部移除
                    ListViewItem item = null;
                    if (moveDirection == MoveDirection.Forward)
                    {
                        item = usingItemList.Values[0];
                        usingItemList.RemoveAt(0);
                        Event_OnHideScrollViewItem?.Invoke(item.gameObject, item.Index);
                    }
                    else
                    {
                        item = usingItemList.Values[usingItemList.Count - 1];
                        usingItemList.RemoveAt(usingItemList.Count - 1);
                        Event_OnHideScrollViewItem?.Invoke(item.gameObject, item.Index);
                    }
                    item.gameObject.SetActive(false);
                    item.OnLoseCentered();
                    recycledItemList.Enqueue(item);
                }
            }
            //预制不够用时
            else if (usingItemList.Count < count)
            {
                //优先从缓存中获取
                while (recycledItemList.Count > 0 && (usingItemList.Count + willUsingItemList.Count) < count)
                {
                    ListViewItem item = recycledItemList.Dequeue();
                    item.gameObject.SetActive(true);
                    willUsingItemList.Enqueue(item);
                }
                while ((usingItemList.Count + willUsingItemList.Count) < count)
                {
                    GameObject go = Instantiate(itemPrefab, Vector3.zero, Quaternion.identity, Content);
                    if (isPreviewing)
                    {
                        go.hideFlags = HideFlags.DontSave;
                    }
                    go.transform.SetParent(Content);
                    go.SetActive(true);
                    ListViewItem item = go.GetComponent<ListViewItem>();
                    item.SetListView(this);
                    go.transform.localScale = Vector3.one;
                    go.transform.localPosition = itemPrefab.transform.localPosition;
                    InitializeItemAnchor(item);
                    willUsingItemList.Enqueue(item);
                    Event_OnCreateScrollViewItem?.Invoke(item.gameObject);
                }
            }
            if (resortSibling)
            {
                var tempList = usingItemList.Values.ToList();
                tempList.Sort((a, b) =>
                {
                    if (ascendingOrder == false)
                        return b.Index - a.Index;
                    else
                        return a.Index - b.Index;
                });
                foreach (var p in tempList)
                {
                    p.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>
        /// 更新视野范围内的所有Item
        /// </summary>
        /// <param name="forceUpdateData">索引相同时是否更新数据</param>
        public void UpdateItemInViewRange(bool forceUpdateData)
        {
            int startIndex = CalculateItemStartIndexInViewRange();
            int endIndex = CalculateItemEndIndexInViewRange();
            int count = Mathf.Clamp(endIndex - startIndex + 1, 0, dataCount);
            UpdateItemInstanceCount(count);
            if (count > 0)
            {
                if (startIndex >= dataCount)
                {
                    endIndex = startIndex;
                }
                if (endIndex >= dataCount)
                {
                    endIndex = dataCount - 1;
                }
                UpdateItemsWithIndex(startIndex, endIndex, forceUpdateData);
            }
        }

        /// <summary>
        /// 根据索引更新Item位置和数据
        /// </summary>
        /// <param name="startIndex">起始索引(包含)</param>
        /// <param name="endIndex">结尾索引(包含)</param>
        /// <param name="forceUpdateData">索引未发生变化的Item是否需要更新数据</param>
        private void UpdateItemsWithIndex(int startIndex, int endIndex, bool forceUpdateData)
        {
            //先将usingWrapContentItemList中和当前要显示的索引范围重合的物体处理掉
            for (int i = 0; i < usingItemList.Values.Count; i++)
            {
                ListViewItem item = usingItemList.Values[i];
                bool inViewRange = item.Index >= startIndex && item.Index <= endIndex;

                if (inViewRange)
                {
                    item.RectTransform.anchoredPosition = itemPositionDic[item.Index];
                    item.RectTransform.sizeDelta = itemSizeDic[item.Index];
                    if (forceUpdateData)
                    {
                        UpdateOneItemData(item.Index);
                    }
                }
                else
                {
                    usingItemList.Remove(item.Index);
                    willUsingItemList.Enqueue(item);
                    Event_OnHideScrollViewItem?.Invoke(item.gameObject, item.Index);
                    //注意，因为从usingWrapContentItemList中移除了一项，索引要回退1，不然循环就会出现跳过一项的bug
                    i--;
                }
            }

            //再处理剩下没有满足的索引物体
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (i >= dataCount)
                {
                    //当i超过dataCount时，说明已经没有数据可以展示了
                    break;
                }
                if (usingItemList.ContainsKey(i) == false)
                {
                    ListViewItem item = willUsingItemList.Dequeue();
                    item.RectTransform.anchoredPosition = itemPositionDic[i];
                    item.RectTransform.sizeDelta = itemSizeDic[i];
                    item.name = string.Format("{0}_{1}", prefabName, i);
                    usingItemList.Add(i, item);
                    UpdateOneItemData(i);
                }
            }
        }

        /// <summary>
        /// 更新一个Item的数据
        /// </summary>
        /// <param name="itemKey"></param>
        /// <param name="dataIndex"></param>
        private void UpdateOneItemData(int dataIndex)
        {
            usingItemList[dataIndex].Index = dataIndex;
            //理论上来说，只要按列表移动方向来进行实例回收的话，是不需要重新赋值缩放与Alpha的，这里只是为了做个保障
            //Alpha先不重新赋值，留着排查问题（没有按列表移动方向来进行实例回收的问题）
            if (useGalleryMode)
            {
                usingItemList[dataIndex].SetSize(itemSizeDic[dataIndex]);
            }
            Event_OnRefreshScrollViewItem?.Invoke(usingItemList[dataIndex].gameObject, dataIndex);
        }

        /// <summary>
        /// 计算出当前视野范围应该显示的Item的起始索引
        /// </summary>
        private int CalculateItemStartIndexInViewRange()
        {
            Vector2 pos = Content.anchoredPosition;
            int startIndex = 0;
            bool find = false;
            //计算content在当前位置的时候应该显示的索引
            if (direction == Slider.Direction.TopToBottom)
            {
                for (int i = 0; i < itemPositionDic.Count; i++)
                {
                    if (-pos.y >= itemPositionDic[i].y - itemSizeDic[i].y * prefabRectTransform.pivot.y)
                    {
                        startIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            else if (direction == Slider.Direction.BottomToTop)
            {
                for (int i = 0; i < itemPositionDic.Count; i++)
                {
                    if (-pos.y <= itemPositionDic[i].y + itemSizeDic[i].y * prefabRectTransform.pivot.y)
                    {
                        startIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            else if (direction == Slider.Direction.LeftToRight)
            {
                for (int i = 0; i < itemPositionDic.Count; i++)
                {
                    if (-pos.x <= itemPositionDic[i].x + itemSizeDic[i].x * (prefabRectTransform.pivot.x + 1))
                    {
                        startIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            else if (direction == Slider.Direction.RightToLeft)
            {
                for (int i = 0; i < itemPositionDic.Count; i++)
                {
                    if (-pos.x >= itemPositionDic[i].x - itemSizeDic[i].x * prefabRectTransform.pivot.x)
                    {
                        startIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            if (find)
            {
                startIndex = Mathf.Clamp(startIndex, 0, dataCount - 1);
            }
            else
            {
                startIndex = 0;
            }
            return startIndex;
        }

        /// <summary>
        /// 更新ItemSize字典
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newSize"></param>
        private void UpdateItemSize(int index, Vector2 newSize)
        {
            itemSizeDic[index] = newSize;
        }

        /// <summary>
        /// 计算出当前视野范围应该显示的Item的末尾索引
        /// </summary>
        /// <returns></returns>
        private int CalculateItemEndIndexInViewRange()
        {
            Vector2 pos = Content.anchoredPosition;
            int endIndex = itemPositionDic.Count;
            bool find = false;
            if (direction == Slider.Direction.TopToBottom)
            {
                for (int i = itemPositionDic.Count - 1; i >= 0; i--)
                {
                    if ((-pos.y - viewPortSize.y) <= itemPositionDic[i].y + itemSizeDic[i].y * (1 - prefabRectTransform.pivot.y))
                    {
                        endIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            else if (direction == Slider.Direction.BottomToTop)
            {
                for (int i = itemPositionDic.Count - 1; i >= 0; i--)
                {
                    if ((-pos.y + viewPortSize.y) >= itemPositionDic[i].y - itemSizeDic[i].y * prefabRectTransform.pivot.y)
                    {
                        endIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            else if (direction == Slider.Direction.LeftToRight)
            {
                for (int i = itemPositionDic.Count - 1; i >= 0; i--)
                {
                    if ((viewPortSize.x - pos.x) >= itemPositionDic[i].x - itemSizeDic[i].x * prefabRectTransform.pivot.x)
                    {
                        endIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            else if (direction == Slider.Direction.RightToLeft)
            {
                for (int i = itemPositionDic.Count - 1; i >= 0; i--)
                {
                    if ((-pos.x - viewPortSize.x) <= itemPositionDic[i].x + itemSizeDic[i].x * (1 - prefabRectTransform.pivot.x))
                    {
                        endIndex = i;
                        find = true;
                        break;
                    }
                }
            }
            if (find)
            {
                endIndex = Mathf.Clamp(endIndex, 0, dataCount - 1);
            }
            else
            {
                endIndex = 0;
            }
            return endIndex;
        }

        /// <summary>
        /// 计算将Index对应的Item位置调整到视图范围的起始/结束处需要的Content偏移值
        /// </summary>
        /// <param name="index">Item索引</param>
        /// <param name="isBeginOrEnd">起始/结束</param>
        /// <param name="clampResult">是否限制结果在视口范围内</param>
        /// <returns></returns>
        private Vector2 CalculateContentPositionByIndex(int index, bool isBeginOrEnd = true, bool clampResult = true)
        {
            Vector2 pos = Vector2.zero;
            if (isBeginOrEnd)
            {
                if (useGalleryMode)
                {
                    float minScale = galleryItemScaleCurve.Evaluate(1);
                    if (minScale < SCALE_MIN_SIZE)
                    {
                        //避免缩为0无限小导致宽度上可以放无限个物体的死循环
                        minScale = SCALE_MIN_SIZE;
                    }
                    if (direction == Slider.Direction.BottomToTop)
                    {
                        pos.y = -(minScale * itemSize.y * index);
                        if (clampResult)
                        {
                            pos.y = Mathf.Clamp(pos.y, viewPortSize.y - Content.rect.height, 0);
                        }
                    }
                    else if (direction == Slider.Direction.TopToBottom)
                    {
                        pos.y = minScale * itemSize.y * index;
                        if (clampResult)
                        {
                            pos.y = Mathf.Clamp(pos.y, 0, Content.rect.height - viewPortSize.y);
                        }
                    }
                    else if (direction == Slider.Direction.LeftToRight)
                    {
                        pos.x = -(minScale * itemSize.x * index);
                        if (clampResult)
                        {
                            pos.x = Mathf.Clamp(pos.x, viewPortSize.x - Content.rect.width, 0);
                        }
                    }
                    else if (direction == Slider.Direction.RightToLeft)
                    {
                        pos.x = minScale * itemSize.x * index;
                        if (clampResult)
                        {
                            pos.x = Mathf.Clamp(pos.x, 0, Content.rect.width - viewPortSize.x);
                        }
                    }
                }
                else
                {
                    if (direction == Slider.Direction.BottomToTop)
                    {
                        pos.y = -itemPositionDic[index].y + itemSizeDic[index].y * prefabRectTransform.pivot.y;
                        if (clampResult)
                        {
                            pos.y = Mathf.Clamp(pos.y, viewPortSize.y - Content.rect.height, 0);
                        }
                    }
                    else if (direction == Slider.Direction.TopToBottom)
                    {
                        pos.y = -itemPositionDic[index].y - itemSizeDic[index].y * (1 - prefabRectTransform.pivot.y);
                        if (clampResult)
                        {
                            if (Content.rect.height > viewPortSize.y)
                            {
                                pos.y = Mathf.Clamp(pos.y, 0, Content.rect.height - viewPortSize.y);
                            }
                            else
                            {
                                pos.y = 0;
                            }
                        }
                    }
                    else if (direction == Slider.Direction.LeftToRight)
                    {
                        pos.x = -itemPositionDic[index].x + itemSizeDic[index].x * prefabRectTransform.pivot.x;
                        if (clampResult)
                        {
                            float sizeDiff = viewPortSize.x - Content.rect.width;
                            float min = Mathf.Min(sizeDiff, 0);
                            float max = Mathf.Min(sizeDiff, 0);
                            pos.x = Mathf.Clamp(pos.x, min, max);
                        }
                    }
                    else if (direction == Slider.Direction.RightToLeft)
                    {
                        pos.x = -itemPositionDic[index].x - itemSizeDic[index].x * (1 - prefabRectTransform.pivot.x);
                        if (clampResult)
                        {
                            float sizeDiff = Content.rect.width - viewPortSize.x;
                            float min = Mathf.Min(sizeDiff, 0);
                            float max = Mathf.Min(sizeDiff, 0);
                            pos.x = Mathf.Clamp(pos.x, min, max);
                        }
                    }
                }
            }
            else
            {
                if (useGalleryMode)
                {
                    Debug.LogError("Gallery 暂不支持此模式，原因：未实现，待有需求时根据左右方向的代码实现一下");
                }
                else
                {
                    if (direction == Slider.Direction.BottomToTop)
                    {
                        pos.y = -itemPositionDic[index].y;
                        if (clampResult)
                        {
                            pos.y = Mathf.Clamp(pos.y, viewPortSize.y - Content.rect.height, 0);
                        }
                    }
                    else if (direction == Slider.Direction.TopToBottom)
                    {
                        pos.y = (-itemPositionDic[index + column - 1].y) - padding.bottom;
                        if (clampResult)
                        {
                            if (Content.rect.height > viewPortSize.y)
                            {
                                pos.y = Mathf.Clamp(pos.y, 0, Content.rect.height - viewPortSize.y);
                            }
                            else
                            {
                                pos.y = 0;
                            }
                        }
                    }
                    else if (direction == Slider.Direction.LeftToRight)
                    {
                        pos.x = -itemPositionDic[index].x;
                        if (clampResult)
                        {
                            pos.x = Mathf.Clamp(pos.x, viewPortSize.x - Content.rect.width, 0);
                        }
                    }
                    else if (direction == Slider.Direction.RightToLeft)
                    {
                        pos.x = -itemPositionDic[index + row].x - viewPortSize.x;
                        if (clampResult)
                        {
                            pos.x = Mathf.Clamp(pos.x, 0, Content.rect.width - viewPortSize.x);
                        }
                    }
                }
            }
            return pos;
        }

        /// <summary>
        /// 计算将Index对应的Item位置调整到视图范围中间所需要的Content偏移值
        /// </summary>
        /// <param name="index">Item索引</param>
        /// <returns></returns>
        private Vector2 CalculateContentCenterOnPosition(int index)
        {
            Vector2 contentOffset = Vector2.zero;
            if (useGalleryMode)
            {
                Vector2 halfView = viewPortSize / 2;
                float maxScale = galleryItemScaleCurve.Evaluate(0);
                float minScale = galleryItemScaleCurve.Evaluate(1);
                if (minScale < SCALE_MIN_SIZE)
                {
                    //避免缩为0无限小导致宽度上可以放无限个物体的死循环
                    minScale = SCALE_MIN_SIZE;
                }

                Vector2 totalItemSize = Vector2.zero;

                //加上最中间的ItemSize
                totalItemSize += itemSize * maxScale;
                Vector2 preItemSize = itemSize;

                int leftItemCount = index;
                if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
                {
                    //TODO: 实现竖向的GalleryMode显示
                    Debug.LogError("尚未实现的Feature");
                }
                else if (direction == Slider.Direction.LeftToRight || direction == Slider.Direction.RightToLeft)
                {
                    while (totalItemSize.x < viewPortSize.x && leftItemCount > 0)
                    {
                        leftItemCount--;
                        //使用逼近法适配任意曲线
                        //先假设一个值，检验这个值合适不，初始为前一Item尺寸的一半
                        float assumeNextSize = preItemSize.x / 2;
                        float assumeNextPos = (totalItemSize.x + assumeNextSize) / 2 + spacing.x;
                        float percent = assumeNextPos / halfView.x;
                        float scaleFromCurve = galleryItemScaleCurve.Evaluate(percent);
                        float sizeByScale = itemSize.x * scaleFromCurve;
                        float delta = sizeByScale - assumeNextSize;
                        while (Mathf.Abs(delta) > 0.1)
                        {
                            assumeNextSize = (sizeByScale + assumeNextSize) / 2;
                            assumeNextPos = (totalItemSize.x + assumeNextSize) / 2 + spacing.x;
                            percent = assumeNextPos / halfView.x;
                            scaleFromCurve = galleryItemScaleCurve.Evaluate(percent);
                            sizeByScale = itemSize.x * scaleFromCurve;
                            delta = sizeByScale - assumeNextSize;
                        }
                        float nextSize = assumeNextSize;
                        preItemSize.x = nextSize;

                        if (nextSize < itemSize.x * minScale)
                        {
                            nextSize = itemSize.x * minScale;
                        }
                        totalItemSize.x += nextSize * 2 + spacing.x * 2;
                    }

                    contentOffset.x = -(totalItemSize.x / 2 + padding.left - viewPortSize.x / 2) - leftItemCount * (minScale * itemSize.x + spacing.x);
                }
            }
            else
            {
                if (direction == Slider.Direction.BottomToTop)
                {
                    contentOffset = CalculateContentPositionByIndex(index, true, false);
                    contentOffset.y = Mathf.Clamp(contentOffset.y -= viewPortSize.y / 2, viewPortSize.y - Content.rect.height, 0);
                }
                else if (direction == Slider.Direction.TopToBottom)
                {
                    contentOffset = CalculateContentPositionByIndex(index, true, false);
                    contentOffset.y = Mathf.Clamp(contentOffset.y -= viewPortSize.y / 2 - itemSizeDic[index].y * (1 - prefabRectTransform.pivot.y), 0, Content.rect.height - viewPortSize.y);
                }
                else if (direction == Slider.Direction.LeftToRight)
                {
                    contentOffset = CalculateContentPositionByIndex(index, true, false);
                    contentOffset.x = Mathf.Clamp(contentOffset.x += viewPortSize.x / 2 - itemSizeDic[index].x * (1 - prefabRectTransform.pivot.x), viewPortSize.x - Content.rect.width, 0);
                }
                else if (direction == Slider.Direction.RightToLeft)
                {
                    contentOffset = CalculateContentPositionByIndex(index, true, false);
                    contentOffset.x = Mathf.Clamp(contentOffset.x -= viewPortSize.x / 2 + itemSizeDic[index].x * prefabRectTransform.pivot.x, viewPortSize.x - Content.rect.width, 0);
                }
            }
            return contentOffset;
        }

        /// <summary>
        /// Tweener动画更新过程中实时刷新Item
        /// </summary>
        private void MovingTweenerUpdate()
        {
            //虽然DOTween动画过程中OnScrollRectValueChanged也会触发（但是否稳定不确定），为了逻辑上的准确，自行调用，且在OnScrollRectValueChanged屏蔽
            if (useGalleryMode)
            {
                UpdateItemInViewRangeForGallery();
            }
            UpdateItemInViewRange(false);
        }

        private void MovingTweenerComplete()
        {
            isTweenerMoving = false;
            foreach (ListViewItem item in usingItemList.Values)
            {
                if (item.Index == CenterOnChildIndex && item.IsCenterOn == false)
                {
                    item.OnCentered();
                    break;
                }
            }
        }

        /// <summary>
        /// 滚动列表滚动响应，自动刷新Item
        /// </summary>
        /// <param name="percent"></param>
        private void OnScrollRectValueChanged(Vector2 percent)
        {
            //DOTween动画过程自然会调用刷新方法，这里不再重复调用
            if (isTweenerMoving)
            {
                lastScrollRectPercent = percent;
                return;
            }

            if (Mathf.Abs(lastContentPosition.x - Content.anchoredPosition.x) < 0.01f &&
                Mathf.Abs(lastContentPosition.y - Content.anchoredPosition.y) < 0.01f)
            {
                lastScrollRectPercent = percent;
                //避免大量无意义的刷新
                return;
            }

            lastContentPosition = Content.anchoredPosition;
            if (direction == Slider.Direction.BottomToTop)
            {
                moveDirection = lastScrollRectPercent.y > percent.y ? MoveDirection.Backward : MoveDirection.Forward;
            }
            else if (direction == Slider.Direction.TopToBottom)
            {
                moveDirection = lastScrollRectPercent.y > percent.y ? MoveDirection.Forward : MoveDirection.Backward;
            }
            else if (direction == Slider.Direction.LeftToRight)
            {
                moveDirection = lastScrollRectPercent.x > percent.x ? MoveDirection.Backward : MoveDirection.Forward;
            }
            else if (direction == Slider.Direction.RightToLeft)
            {
                moveDirection = lastScrollRectPercent.x > percent.x ? MoveDirection.Forward : MoveDirection.Backward;
            }

            lastScrollRectPercent = percent;

            if (useGalleryMode)
            {
                UpdateItemInViewRangeForGallery();
            }
            UpdateItemInViewRange(false);
        }

        /// <summary>
        /// 刷新Viewport的尺寸
        /// </summary>
        public void RefreshViewportSize()
        {
            viewPortSize = new Vector2(ScrollRect.viewport.rect.width, ScrollRect.viewport.rect.height);
        }

        /// <summary>
        /// 刷新图库模式下视野范围内每个元素的尺寸
        /// </summary>
        private void UpdateItemInViewRangeForGallery()
        {
            Vector2 halfView = viewPortSize / 2;
            Vector2 scrollRectCenter = Vector2.zero;
            if (direction == Slider.Direction.BottomToTop)
            {
                scrollRectCenter = halfView - this.ContentOffset;
            }
            else if (direction == Slider.Direction.TopToBottom)
            {
                scrollRectCenter = this.ContentOffset - halfView;
            }
            else if (direction == Slider.Direction.LeftToRight)
            {
                scrollRectCenter = halfView - this.ContentOffset;
            }
            else if (direction == Slider.Direction.RightToLeft)
            {
                scrollRectCenter = this.ContentOffset - halfView;
            }

            float minScale = galleryItemScaleCurve.Evaluate(1);
            if (minScale < SCALE_MIN_SIZE)
            {
                //避免缩为0无限小导致宽度上可以放无限个物体的死循环
                minScale = SCALE_MIN_SIZE;
            }
            Vector2 minSize = minScale * itemSize;

            //先将所有Item的Size设为最小值
            for (int i = 0; i < dataCount; i++)
            {
                itemSizeDic[i] = minSize;
            }
            //然后更新视野范围内的Item的Size
            foreach (ListViewItem item in usingItemList.Values)
            {
                Vector2 itemCenter = item.Position + new Vector2(item.Size.x / 2 * (prefabRectTransform.pivot.x - 0.5f), item.Size.y / 2 * (prefabRectTransform.pivot.y - 0.5f));
                Vector2 distance = itemCenter - scrollRectCenter;
                float percent = 1;
                if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
                {
                    percent = distance.y / halfView.y;
                }
                else if (direction == Slider.Direction.LeftToRight || direction == Slider.Direction.RightToLeft)
                {
                    percent = distance.x / halfView.x;
                }
                item.RelativePosToCenter = percent;
                percent = Mathf.Clamp01(Mathf.Abs(percent));

                float scale = galleryItemScaleCurve.Evaluate(percent);
                if (scale < SCALE_MIN_SIZE)
                {
                    //避免缩为0无限小导致宽度上可以放无限个物体的死循环
                    scale = SCALE_MIN_SIZE;
                }
                item.Scale(scale);
                UpdateItemSize(item.Index, item.Size);
            }
            ResetContentSize();
            ResetAllItemPosition();
        }
        #endregion

        #region 制作界面时的工具函数，可在Inspector中点击相应的按钮

        /// <summary>
        /// 预览布局效果，用完记得Clear
        /// </summary>
        public void Preview()
        {
            int cacheCount = dataCount;
            isPreviewing = true;
            Initialize();
            dataCount = cacheCount;
            Refresh(dataCount, true);
            isPreviewing = false;
            itemPrefab.SetActive(false);
        }

        /// <summary>
        /// 清空预览产生的数据
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < usingItemList.Count; i++)
            {
                GameObject willDestroyObj = usingItemList.Values[i].gameObject;
                usingItemList.RemoveAt(i);
                DestroyImmediate(willDestroyObj);
                i--;
            }
            while (recycledItemList.Count > 0)
            {
                ListViewItem item = recycledItemList.Dequeue();
                DestroyImmediate(item.gameObject);
            }
            itemSizeDic.Clear();
            itemPositionDic.Clear();
            itemPrefab.SetActive(true);
        }

        #endregion

        #region IBeginDragHandler 接口实现

        /// <summary>
        /// 开始拖拽事件，用于处理元素失去居中
        /// </summary>
        /// <param name="eventData"></param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            foreach (ListViewItem item in usingItemList.Values)
            {
                item.OnLoseCentered();
            }
        }

        #endregion

        #region IEndDragHandler 接口实现

        /// <summary>
        /// 结束拖拽事件，用于处理元素自动居中
        /// </summary>
        /// <param name="eventData"></param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (centerOnChild)
            {
                if (direction == Slider.Direction.LeftToRight || direction == Slider.Direction.RightToLeft)
                {
                    float centerX = transform.localPosition.x;
                    float minDistance = float.MaxValue;
                    int index = 0;
                    for (int i = 0; i < itemPositionDic.Count; i++)
                    {
                        Vector2 itemPos = itemPositionDic[i];
                        Vector3 worldPos = Content.TransformPoint(itemPos);
                        Vector2 localPos = transform.parent.InverseTransformPoint(worldPos);
                        float distance = Mathf.Abs(localPos.x - centerX);
                        if (minDistance > distance)
                        {
                            minDistance = distance;
                            index = i;
                        }
                    }
                    MoveTargetToCenterAnimated(index);
                }
                else if (direction == Slider.Direction.BottomToTop || direction == Slider.Direction.TopToBottom)
                {
                    float centerY = transform.localPosition.y;
                    float minDistance = float.MaxValue;
                    int index = 0;
                    for (int i = 0; i < itemPositionDic.Count; i++)
                    {
                        Vector2 itemPos = itemPositionDic[i];
                        Vector3 worldPos = Content.TransformPoint(itemPos);
                        Vector2 localPos = transform.parent.InverseTransformPoint(worldPos);
                        float distance = Mathf.Abs(localPos.y - centerY);
                        if (minDistance > distance)
                        {
                            minDistance = distance;
                            index = i;
                        }
                    }
                    MoveTargetToCenterAnimated(index);
                }
            }
        }

        #endregion
    }
}