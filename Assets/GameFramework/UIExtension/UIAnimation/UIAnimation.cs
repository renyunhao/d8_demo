using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFramework
{
    public enum UIAnimationDirection
    {
        Center,
        Up,
        Down,
        Left,
        Right,
        PlayerSet
    }

    public enum UIAnimationStyle
    {
        Appear,
        Hide,
    }

    public enum UIAnimationType
    {
        TweeningEase,
        AnimationCurve,
    }

    public enum UIAnimateContent
    {
        Move,
        MaskAlpha,
        Scale,
        Rotation,
        Alpha,
    }

    public class AnimationData
    {
        public bool enable;
        public float animationTime;
        public float startValue;
        public float overValue;
        public AnimationData(float animationTime, float startValue, float overValue)
        {
            this.animationTime = animationTime;
            this.startValue = startValue;
            this.overValue = overValue;
        }
    }

    public class UIAnimation : MonoBehaviour
    {
        public const float INIT_MASK_MAX_ALPHA = 0.86F;
        public const float INIT_ANIMATION_TIME = 0.25F;
        private event Action Event_MaskDown;
        private event Action Event_Start;
        private event Action Event_Over;
        private event Action<float> Event_Update;
        private bool affectTimeScale = true;

        /// <summary>
        /// UI动画作用的物体
        /// </summary>
        private GameObject targetObject;
        #region 遮罩相关
        /// <summary>
        /// UI动画作用物体的遮罩
        /// </summary>
        private GameObject maskObject;
        [HideInInspector]
        /// <summary>
        /// 是否存在遮罩
        /// </summary>
        public bool hasMask = false;
        [HideInInspector]
        /// <summary>
        /// 遮罩的深度
        /// </summary>
        public float maskMaxAlpha;
        [HideInInspector]
        /// <summary>
        /// 遮罩的颜色
        /// </summary>
        public Color maskColor;
        [HideInInspector]
        /// <summary>
        /// 遮罩的图片
        /// </summary>
        public Sprite maskSprite;
        #endregion
        #region 动画曲线
        [HideInInspector]
        /// <summary>
        /// Dotween曲线
        /// </summary>
        public UIAnimationType animationType = UIAnimationType.TweeningEase;
        [HideInInspector]
        /// <summary>
        /// 使用的曲线
        /// </summary>
        public AnimationCurve useCurve;
        [HideInInspector]
        /// <summary>
        /// 使用的动画类型
        /// </summary>
        public Ease useEase = Ease.Linear;
        #endregion
        [HideInInspector]
        /// <summary>
        /// 动画的时间
        /// </summary>
        public float animationTime;
        #region 动画播放控制
        [HideInInspector]
        /// <summary>
        /// 等待的时间
        /// </summary>
        public float delayTime;
        /// <summary>
        /// 动画计时
        /// </summary>
        private float timer;
        /// <summary>
        /// 播放控制
        /// </summary>
        private bool startPlayAnimation = false;
        #endregion
        /// <summary>
        /// 当前的执行的判断行为的Yweener
        /// </summary
        private Tween currentTweener;
        /// <summary>
        /// 当前的执行的UITween
        /// </summary>
        private Tween currentScaleTweener;
        /// <summary>
        /// 当前的遮罩执行的tween
        /// </summary>
        private Tween currentMaksTweener;
        /// <summary>
        /// 当前Group
        /// </summary>
        private Tween groupTweener;
        /// <summary>
        /// 当前的执行的移动动画
        /// </summary>
        private Tween currentMoveTweener;
        /// <summary>
        /// 当前的执行的UITween
        /// </summary>
        private Tween currentRotationTweener;
        /// <summary>
        /// 当前动画处于的状态
        /// </summary>
        private UIAnimationStyle currentState;
        /// <summary>
        /// 当前动画的方向
        /// </summary>
        private UIAnimationDirection derType;
        private Dictionary<UIAnimateContent, bool> animationSwitchDic = new Dictionary<UIAnimateContent, bool>();
        private Dictionary<UIAnimateContent, AnimationData> animationDic = new Dictionary<UIAnimateContent, AnimationData>();
        private List<AnimationData> animationList = new List<AnimationData>();

        public bool HasMask
        {
            get
            {
                return hasMask;
            }

            set
            {
                hasMask = value;
                if (hasMask && maskObject == null)
                {
                    #region 生成遮罩
                    int index = targetObject.transform.GetSiblingIndex();
                    GameObject _mask = new GameObject();
                    // 定义对象名为 Mask  
                    _mask.name = "Mask";
                    // 添加Image  
                    _mask.AddComponent<UnityEngine.UI.Image>();

                    _mask.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(2000, 2000);
                    _mask.transform.SetParent(targetObject.transform.parent);
                    _mask.transform.localPosition = Vector3.zero;
                    _mask.transform.localScale = Vector3.one;
                    _mask.transform.SetSiblingIndex(index);
                    _mask.transform.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
                    maskObject = _mask;
                    EventTrigger eventTrigger = maskObject.AddComponent<EventTrigger>();
                    UnityAction<BaseEventData> click = new UnityAction<BaseEventData>(MaskDown);
                    EventTrigger.Entry myclick = new EventTrigger.Entry();
                    myclick.eventID = EventTriggerType.PointerClick;
                    myclick.callback.AddListener(click);
                    eventTrigger.triggers.Add(myclick);
                    maskObject.gameObject.SetActive(false);

                    Canvas canvas = this.transform.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        maskObject.layer = canvas.gameObject.layer;
                    }
                    #endregion
                }
            }
        }

        void Update()
        {
            if (startPlayAnimation)
            {
                timer += Time.unscaledDeltaTime;
                if (timer >= delayTime)
                {
                    startPlayAnimation = false;
                    Play(currentState);
                    timer = 0;
                }
            }
            if (currentTweener != null)
            {
                if (Event_Update != null)
                {
                    if (currentTweener.Elapsed(false) / currentTweener.Duration(false) < 0.99f)
                    {
                        Event_Update(currentTweener.Elapsed(false) / currentTweener.Duration(false));
                    }
                }
            }
        }

        public void InitAnimation(Transform targetObject, Image mask = null)
        {
            this.targetObject = targetObject.gameObject;
            if (mask != null)
            {
                this.maskObject = mask.gameObject;
                this.maskObject.gameObject.SetActive(false);
            }
            HasMask = true;
            animationTime = INIT_ANIMATION_TIME;
            maskColor = new Color(0, 0, 0, 0);
            maskMaxAlpha = INIT_MASK_MAX_ALPHA;
            animationSwitchDic.Add(UIAnimateContent.Alpha, true);
            animationSwitchDic.Add(UIAnimateContent.MaskAlpha, true);
            animationSwitchDic.Add(UIAnimateContent.Rotation, false);
            animationSwitchDic.Add(UIAnimateContent.Move, true);

            animationList.Add(new AnimationData(animationTime, 0, 1));
            animationList.Add(new AnimationData(animationTime, 0, 1));
            animationList.Add(new AnimationData(animationTime, 0, 360));
            animationList.Add(new AnimationData(animationTime, 0, 1));
            animationList.Add(new AnimationData(animationTime, 0, 1));
        }

        public void Hide()
        {
            currentState = UIAnimationStyle.Hide;
            gameObject.SetActive(false);
            if (maskObject != null)
            {
                maskObject.SetActive(false);
            }
        }

        public void PlayAnimation(UIAnimationStyle playState)
        {
            InitBeforePlay(playState);

        }

        public void PlayAnimation(UIAnimationStyle playState, UIAnimationDirection der)
        {
            InitBeforePlay(playState, der);
        }

        public void PlayAnimation(UIAnimationStyle playState, float animationTime = INIT_ANIMATION_TIME, UIAnimationDirection der = UIAnimationDirection.Center, bool hasMask = true)
        {
            if (animationTime != 0)
            {
                InitBeforePlay(playState, der, animationTime, hasMask);
                if (maskObject != null)
                {
                    maskObject.gameObject.SetActive(false);
                }
            }
            else
            {
                gameObject.SetActive(false);
                if (maskObject != null)
                {
                    maskObject.gameObject.SetActive(false);
                }
            }
        }

        void InitBeforePlay(UIAnimationStyle playState, UIAnimationDirection der = UIAnimationDirection.Center,
        float animationTime = INIT_ANIMATION_TIME, bool hasMask = true)
        {
            HasMask = hasMask;
            if (GetComponent<CanvasGroup>() == null)
            {
                gameObject.AddComponent<CanvasGroup>();
            }
            this.animationTime = animationTime;
            if (der != UIAnimationDirection.Center)
            {
                transform.localScale = Vector3.one;
            }
            derType = der;
            if (currentState == playState && playState == UIAnimationStyle.Hide)
            {
                return;
            }
            currentState = playState;
            startPlayAnimation = true;
            delayTime = 0.01f;
            timer = 0;
            if (currentState == UIAnimationStyle.Appear)
            {
                //默认动画配置设置
                animationType = UIAnimationType.TweeningEase;
                //useCurve = UISystem.instance.useCurve;
                gameObject.SetActive(true);
                transform.localScale = Vector3.one * 0.1f;
                GetComponent<CanvasGroup>().alpha = 0;
                maskColor = new Color(25 / 255f, 32 / 255f, 43 / 255f, 0);
                SetMaskAlphaIntervalValue(0, INIT_MASK_MAX_ALPHA);
            }
            else
            {
                HasMask = false;
            }
            InitTweener();
            ClearAction();
        }
        #region 动画逻辑
        public void Play(UIAnimationStyle playState)
        {
            InitTweener();
            Init();
            if (derType == UIAnimationDirection.Center || derType == UIAnimationDirection.PlayerSet)
            {
                //缩放动画
                UItargetScaleAnimation(playState);
                currentTweener = currentScaleTweener;
                currentTweener.SetUpdate(affectTimeScale);
            }
            else
            {
                #region 定制缩放
                if (animationSwitchDic.ContainsKey(UIAnimateContent.Scale))
                {
                    UItargetScaleAnimation(playState);
                    animationSwitchDic[UIAnimateContent.Scale] = false;
                }
                #endregion
                PlayTweener(playState);
                currentTweener = currentMoveTweener;
                currentTweener.SetUpdate(affectTimeScale);
            }
            TweenerAction(currentTweener, playState);
            if (animationSwitchDic[UIAnimateContent.Rotation])
            {
                UITargetRotationAnimation(playState);
                animationSwitchDic[UIAnimateContent.Rotation] = false;
            }
            //透明度动画
            if (animationSwitchDic[UIAnimateContent.Alpha])
            {
                UIGroupAnimation(playState).SetUpdate(affectTimeScale);
            }
            #region 遮罩动画
            if (animationSwitchDic[UIAnimateContent.MaskAlpha])
            {
                UIMaskAnimation(playState);
            }
            #endregion
            #endregion
        }

        /// <summary>
        /// 缩放动画
        /// </summary>
        /// <param name="targetGame"></param>
        /// <param name="State"></param>
        public void UItargetScaleAnimation(UIAnimationStyle state)
        {

            #region 加入玩家定制动画
            float animationTime = this.animationTime;
            float targetValue = 0;
            float startValue = 0;
            if (state == UIAnimationStyle.Hide)
            {
                targetValue = 0;
                startValue = 0;
            }
            else if (state == UIAnimationStyle.Appear)
            {
                targetValue = 1;
                startValue = 0.4f;
            }
            bool isDefault = false;
            if (animationDic.ContainsKey(UIAnimateContent.Scale))
            {
                animationTime = animationDic[UIAnimateContent.Scale].animationTime;
                targetValue = animationDic[UIAnimateContent.Scale].overValue;
                startValue = animationDic[UIAnimateContent.Scale].startValue;
                animationDic.Remove(UIAnimateContent.Scale);
            }
            else
            {
                //使用默认缩放逻辑
                isDefault = true;
            }
            #endregion

            if (state == UIAnimationStyle.Hide)
            {
                if (startValue != 1)
                {
                    #region UI缩放动画
                    currentScaleTweener = transform.DOScale(Vector3.one * 0.2f, animationTime);
                    #endregion
                }
                else
                {
                    #region UI缩放动画
                    currentScaleTweener = transform.DOScale(startValue, animationTime);
                    #endregion
                }
            }
            else if (state == UIAnimationStyle.Appear)
            {
                gameObject.SetActive(true);
                if (isDefault == false)
                {
                    transform.localScale = Vector3.one * startValue;
                    currentScaleTweener = transform.DOScale(Vector3.one * targetValue, animationTime);
                }
                else
                {
                    transform.localScale = Vector3.one * 0.4f;
                    Sequence mySequence = DOTween.Sequence();
                    /// 这里设置迷人的缩放UI动画
                    Tweener tweenerx1 = transform.DOScaleX(0.98f, 0.1f);
                    Tweener tweenery1 = transform.DOScaleY(1.18f, 0.1f);
                    Tweener tweenerx2 = transform.DOScaleX(1.04f, 0.05f);
                    Tweener tweenery2 = transform.DOScaleY(0.93f, 0.05f);
                    Tweener tweenerx3 = transform.DOScaleX(0.97f, 0.083f);
                    Tweener tweenery3 = transform.DOScaleY(1.05f, 0.083f);
                    Tweener tweenerx4 = transform.DOScaleX(1f, 0.0333f);
                    Tweener tweenery4 = transform.DOScaleY(1f, 0.0333f);
                    mySequence.Append(tweenerx1);
                    mySequence.Join(tweenery1);
                    mySequence.Append(tweenerx2);
                    mySequence.Join(tweenery2);
                    mySequence.Append(tweenerx3);
                    mySequence.Join(tweenery3);
                    mySequence.Append(tweenerx4);
                    mySequence.Join(tweenery4);
                    currentScaleTweener = mySequence;
                    currentScaleTweener.SetUpdate(affectTimeScale);
                }
            }
        }

        /// <summary>
        /// 遮罩动画
        /// </summary>
        /// <param name="targetGame"></param>
        /// <param name="State"></param>
        public void UIMaskAnimation(UIAnimationStyle state)
        {
            #region 加入玩家定制动画
            float animationTime = this.animationTime;
            float targetValue = 0;
            // float startValue = 0;
            if (state == UIAnimationStyle.Hide)
            {
                targetValue = 0;
                // startValue = 0;

            }
            else if (state == UIAnimationStyle.Appear)
            {
                targetValue = maskMaxAlpha;
                // startValue = 0;
            }

            if (animationDic.ContainsKey(UIAnimateContent.MaskAlpha))
            {
                animationTime = animationDic[UIAnimateContent.MaskAlpha].animationTime;
                targetValue = animationDic[UIAnimateContent.MaskAlpha].overValue;
                // startValue = animationDic[UIAnimateContent.MaskAlpha].startValue;
                animationDic.Remove(UIAnimateContent.MaskAlpha);
            }
            #endregion

            if (state == UIAnimationStyle.Hide)
            {
                if (HasMask)
                {
                    maskObject.gameObject.SetActive(true);
                    maskObject.GetComponent<UnityEngine.UI.Image>().sprite = maskSprite;
                    currentMaksTweener = maskObject.GetComponent<UnityEngine.UI.Image>().DOColor(new Color(maskColor.r, maskColor.g, maskColor.b, targetValue), animationTime).OnComplete(() =>
                    {
                        maskObject.gameObject.SetActive(false);
                    });
                    currentMaksTweener.SetUpdate(affectTimeScale);
                }
                else
                {
                    maskObject.gameObject.SetActive(false);
                }
            }
            else if (state == UIAnimationStyle.Appear)
            {
                if (HasMask)
                {
                    maskObject.gameObject.SetActive(true);
                    maskObject.GetComponent<UnityEngine.UI.Image>().sprite = maskSprite;
                    currentMaksTweener = maskObject.GetComponent<UnityEngine.UI.Image>().DOColor(new Color(maskColor.r, maskColor.g, maskColor.b, targetValue), animationTime);
                    currentMaksTweener.SetUpdate(affectTimeScale);
                }
                else
                {
                    maskObject.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 透明度动画
        /// </summary>
        /// <param name="targetGame"></param>
        /// <param name="State"></param>
        /// <returns></returns>
        public Tween UIGroupAnimation(UIAnimationStyle state)
        {
            groupTweener = null;
            #region 加入玩家定制动画
            float animationTime = this.animationTime;
            float targetValue = 0;
            float startValue = 0;
            if (state == UIAnimationStyle.Hide)
            {
                targetValue = 0;
                startValue = 0;
            }
            else if (state == UIAnimationStyle.Appear)
            {
                targetValue = 1;
                startValue = 0;
            }

            if (animationDic.ContainsKey(UIAnimateContent.Alpha))
            {
                animationTime = animationDic[UIAnimateContent.Alpha].animationTime;
                targetValue = animationDic[UIAnimateContent.Alpha].overValue;
                startValue = animationDic[UIAnimateContent.Alpha].startValue;
                animationDic.Remove(UIAnimateContent.Alpha);
            }
            #endregion

            if (state == UIAnimationStyle.Hide)
            {
                groupTweener = DOTween.To(x => GetComponent<CanvasGroup>().alpha = x, GetComponent<CanvasGroup>().alpha, targetValue, 0.15f);
                return groupTweener;
            }
            else if (state == UIAnimationStyle.Appear)
            {
                GetComponent<CanvasGroup>().alpha = 0;
                groupTweener = DOTween.To(x => GetComponent<CanvasGroup>().alpha = x, startValue, targetValue, animationTime);
                return groupTweener;
            }
            return groupTweener;
        }

        /// <summary>
        /// 缩放动画
        /// </summary>
        /// <param name="targetGame"></param>
        /// <param name="State"></param>
        public void UITargetRotationAnimation(UIAnimationStyle state)
        {

            #region 加入玩家定制动画
            float animationTime = this.animationTime;
            float targetValue = 0;
            float startValue = 0;
            if (state == UIAnimationStyle.Hide)
            {
                targetValue = 0;
                startValue = 0;
            }
            else if (state == UIAnimationStyle.Appear)
            {
                targetValue = maskMaxAlpha;
                startValue = 0.4f;
            }

            if (animationDic.ContainsKey(UIAnimateContent.Rotation))
            {
                animationTime = animationDic[UIAnimateContent.Rotation].animationTime;
                targetValue = animationDic[UIAnimateContent.Rotation].overValue;
                startValue = animationDic[UIAnimateContent.Rotation].startValue;
                animationDic.Remove(UIAnimateContent.Rotation);
                #endregion

                if (state == UIAnimationStyle.Hide)
                {
                    GameFramework.Debug.Log(startValue + " + " + targetValue);
                    #region UI缩放动画
                    currentRotationTweener = transform.DOLocalRotate(new Vector3(0, 0, targetValue), animationTime).OnKill(() =>
                        {
                            currentRotationTweener = null;
                        });
                    #endregion
                }
                else if (state == UIAnimationStyle.Appear)
                {
                    gameObject.SetActive(true);
                    transform.localRotation = Quaternion.Euler(new Vector3(0, 0, startValue));
                    currentRotationTweener = transform.DOLocalRotate(new Vector3(0, 0, targetValue), animationTime).OnKill(() =>
                        {
                            currentRotationTweener = null;
                        });
                }
            }
        }

        /// <summary>
        /// 清除所有Tweener操作
        /// </summary>
        void InitTweener()
        {
            if (currentTweener != null)
            {
                currentTweener.Kill();
                currentTweener = null;
            }
            if (currentMoveTweener != null)
            {
                currentMoveTweener.Kill();
                currentMoveTweener = null;
            }
            if (currentScaleTweener != null)
            {
                currentScaleTweener.Kill(false);
                currentScaleTweener = null;
            }
            if (currentMaksTweener != null)
            {
                currentMaksTweener.Kill();
                currentMaksTweener = null;
            }
            if (groupTweener != null)
            {
                groupTweener.Kill();
                groupTweener = null;
            }
            if (currentRotationTweener != null)
            {
                currentRotationTweener.Kill();
                currentRotationTweener = null;
            }
        }

        /// <summary>
        /// 初始化位置
        /// </summary>
        void Init()
        {
            if (currentState == UIAnimationStyle.Appear)
            {
                transform.GetComponent<RectTransform>().localRotation = Quaternion.Euler(Vector3.zero);
                Vector2 position = transform.GetComponent<RectTransform>().anchoredPosition;
                switch (derType)
                {
                    case UIAnimationDirection.Center:
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                        break;
                    case UIAnimationDirection.Down:
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -Screen.height + 100);
                        break;
                    case UIAnimationDirection.Up:
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, Screen.height + 100);
                        break;
                    case UIAnimationDirection.Left:
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(-Screen.width + 100, 0);
                        break;
                    case UIAnimationDirection.Right:
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(Screen.width + 100, 0);
                        break;
                }
            }
        }

        public void PlayTweener(UIAnimationStyle playState)
        {
            if (animationSwitchDic.ContainsKey(UIAnimateContent.Move))
            {
                animationSwitchDic[UIAnimateContent.Move] = false;
            }
            float startValue = 0;
            float overValue = 0;
            if (animationDic.ContainsKey(UIAnimateContent.Move))
            {
                startValue = animationDic[UIAnimateContent.Move].startValue;
                overValue = animationDic[UIAnimateContent.Move].overValue;
                animationDic.Remove(UIAnimateContent.Move);
            }

            transform.localScale = Vector3.one;
            switch (derType)
            {
                case UIAnimationDirection.Down:
                    {
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, startValue);
                        if (playState == UIAnimationStyle.Appear)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosY(overValue == 0 ? 0 : overValue, animationTime);
                        }
                        else if (playState == UIAnimationStyle.Hide)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosY(overValue == 0 ? (-Screen.height - 100) : overValue, animationTime);
                        }
                    }
                    break;
                case UIAnimationDirection.Up:
                    {
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, startValue);
                        if (playState == UIAnimationStyle.Appear)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosY(overValue == 0 ? 0 : overValue, animationTime);
                        }
                        else if (playState == UIAnimationStyle.Hide)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosY(overValue == 0 ? (Screen.height + 100) : overValue, animationTime);
                        }
                    }
                    break;
                case UIAnimationDirection.Left:
                    {
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(startValue, 0);
                        if (playState == UIAnimationStyle.Appear)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosX(overValue == 0 ? 0 : overValue, animationTime);
                        }
                        else if (playState == UIAnimationStyle.Hide)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosX(overValue == 0 ? (-Screen.width - 100) : overValue, animationTime);
                        }
                    }
                    break;
                case UIAnimationDirection.Right:
                    {
                        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(startValue, 0);
                        if (playState == UIAnimationStyle.Appear)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosX(overValue == 0 ? 0 : overValue, animationTime);
                        }
                        else if (playState == UIAnimationStyle.Hide)
                        {
                            currentMoveTweener = transform.GetComponent<RectTransform>().DOAnchorPosX(overValue == 0 ? (Screen.width + 100) : overValue, animationTime);
                        }
                    }
                    break;
            }
            currentMoveTweener.OnKill(() =>
            {
                currentMoveTweener = null;
            });
        }

        /// <summary>
        /// 动画的行为反馈
        /// </summary>
        /// <param name="tweener"></param>
        /// <param name="state"></param>
        void TweenerAction(Tween tweener, UIAnimationStyle state)
        {
            if (animationType == UIAnimationType.AnimationCurve)
            {
                tweener.SetEase(useCurve);
            }
            else if (animationType == UIAnimationType.TweeningEase)
            {
                tweener.SetEase(useEase);
            }
            tweener.OnComplete(() =>
            {
                if (Event_Over != null)
                {
                    Event_Over();
                }
                if (state == UIAnimationStyle.Hide)
                {
                    gameObject.SetActive(false);
                }
                else if (state == UIAnimationStyle.Appear)
                {
                    transform.localScale = Vector3.one;
                    GetComponent<CanvasGroup>().alpha = 1;
                }
                tweener = null;
            }).OnStart(() =>
            {
                if (Event_Start != null)
                {
                    Event_Start();
                }
            });
        }
        /// <summary>
        /// 清除所有事件
        /// </summary>
        void ClearAction()
        {
            if (Event_Start != null)
            {
                Delegate[] ar = Event_Start.GetInvocationList();
                for (int i = 0; i < ar.Length; i++)
                {
                    Event_Start -= ar[i] as Action;
                }
            }
            if (Event_Over != null)
            {
                Delegate[] ar = Event_Over.GetInvocationList();
                for (int i = 0; i < ar.Length; i++)
                {
                    Event_Over -= ar[i] as Action;
                }
            }
            if (Event_Update != null)
            {
                Delegate[] ar = Event_Update.GetInvocationList();
                for (int i = 0; i < ar.Length; i++)
                {
                    Event_Update -= ar[i] as Action<float>;
                }
            }
        }

        #region 行为
        public void OnComplete(Action over)
        {
            Event_Over = over;
        }

        public void OnStart(Action start)
        {
            Event_Start = start;
        }

        public void OnUpdate(Action<float> update)
        {
            Event_Update = update;
        }
        #endregion

        #region 属性设置

        public void SetEase(Ease ease)
        {
            animationType = UIAnimationType.TweeningEase;
            useEase = ease;
        }

        public void SetCurve(AnimationCurve ease)
        {
            animationType = UIAnimationType.AnimationCurve;
            useCurve = ease;
        }

        public void SetDelay(float time = 0.01f)
        {
            if (time < 0.01f)
            {
                time = 0.01f;
            }
            delayTime = time;
            timer = 0;
        }

        /// <summary>
        /// 设置通用时间
        /// </summary>
        /// <param name="animationTime"></param>
        public void SetCurrencyTime(float animationTime = 0.45f)
        {
            this.animationTime = animationTime;
        }

        #region 遮罩属性设置

        private void MaskDown(BaseEventData go)
        {
            if (Event_MaskDown != null)
            {
                Event_MaskDown();
            }
        }
        /// <summary>
        /// 遮罩开关
        /// </summary>
        /// <param name="hasMask"></param>
        public void SetMaskCallBack(Action callBack)
        {
            HasMask = true;
            Event_MaskDown = callBack;
        }


        /// <summary>
        /// 遮罩开关
        /// </summary>
        /// <param name="hasMask"></param>
        public void SetMaskEnable(bool hasMask = true)
        {
            animationSwitchDic[UIAnimateContent.MaskAlpha] = hasMask;
            HasMask = hasMask;
            if (maskObject != null && HasMask == false)
            {
                maskObject.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 设置遮罩的图片
        /// </summary>
        /// <param name="sprite"></param>
        public void SetMaskSprite(Sprite sprite)
        {
            HasMask = true;
            maskSprite = sprite;
        }

        /// <summary>
        /// 设置遮罩的颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetMaskColor(Color color)
        {
            HasMask = true;
            maskColor = color;
        }

        /// <summary>
        /// 设置遮罩透明度的结束值
        /// 如果你在Hide时StartValue是无意义的，它会适应当前的属性继续变化
        /// </summary>
        /// <param name="endValue"></param>
        public void SetMaskAlphaIntervalValue(float startValue, float overValue)
        {
            HasMask = true;
            AnimationData data = GetData(UIAnimateContent.MaskAlpha);
            data.startValue = startValue;
            data.overValue = overValue;
        }

        /// <summary>
        /// 设置遮罩的动画时间
        /// </summary>
        /// <param name="animationTime"></param>
        public void SetMaskAnimationTime(float animationTime)
        {
            HasMask = true;
            AnimationData data = GetData(UIAnimateContent.MaskAlpha);
            data.animationTime = animationTime;
        }
        #endregion

        #region 移动属性设置
        public void SetMoveIntervalValue(float startValue, float overValue)
        {
            animationSwitchDic[UIAnimateContent.Move] = true;
            AnimationData data = GetData(UIAnimateContent.Move);
            data.startValue = startValue;
            data.overValue = overValue;
        }
        #endregion

        #region 缩放属性设置
        /// <summary>
        /// 设置缩放的开始值
        /// 如果你在Hide时StartValue是无意义的，它会适应当前的属性继续变化
        /// </summary>
        /// <param name="hasScale"></param>
        public void SetScaleIntervalValue(float startValue, float overValue)
        {
            animationSwitchDic[UIAnimateContent.Scale] = true;
            AnimationData data = GetData(UIAnimateContent.Scale);
            data.startValue = startValue;
            data.overValue = overValue;
        }

        /// <summary>
        /// 设置缩放的结束值
        /// </summary>
        /// <param name="hasScale"></param>
        public void SetScaleAnimtionTime(float animationTime)
        {
            animationSwitchDic[UIAnimateContent.Scale] = true;
            AnimationData data = GetData(UIAnimateContent.Scale);
            data.animationTime = animationTime;
        }

        #endregion

        #region 旋转属性设置

        /// <summary>
        /// 设置旋转的开始值
        /// 如果你在Hide时StartValue是无意义的，它会适应当前的属性继续变化
        /// </summary>
        /// <param name="hasScale"></param>
        public void SetRotationIntervalValue(float startValue, float overValue)
        {
            animationSwitchDic[UIAnimateContent.Rotation] = true;
            AnimationData data = GetData(UIAnimateContent.Rotation);
            data.startValue = startValue;
            data.overValue = overValue;
        }

        /// <summary>
        /// 设置旋转的时间
        /// </summary>
        /// <param name="hasScale"></param>
        public void SetRotationAnimtionTime(float animationTime)
        {
            animationSwitchDic[UIAnimateContent.Rotation] = true;
            AnimationData data = GetData(UIAnimateContent.Rotation);
            data.animationTime = animationTime;
        }
        #endregion

        #region 透明度属性设置
        /// <summary>
        /// 设置透明度的开始值
        /// 如果你在Hide时StartValue是无意义的，它会适应当前的属性继续变化
        /// </summary>
        /// <param name="hasScale"></param>
        public void SetAlphaIntervalValue(float startValue, float overValue)
        {
            animationSwitchDic[UIAnimateContent.Alpha] = true;
            AnimationData data = GetData(UIAnimateContent.Alpha);
            data.startValue = startValue;
            data.overValue = overValue;
        }

        /// <summary>
        /// 设置透明度的结束值
        /// </summary>
        /// <param name="hasScale"></param>
        public void SetAlphaAnimtionTime(float animationTime)
        {
            animationSwitchDic[UIAnimateContent.Alpha] = true;
            AnimationData data = GetData(UIAnimateContent.Alpha);
            data.animationTime = animationTime;
        }
        #endregion

        #region 
        /// <summary>
        /// 受时间影响(如果设置为Ture,那么会受unity 的TimeScale 影响)
        /// </summary>
        /// <param name="affect"></param>
        public void SetAffectByTimeScale(bool affect)
        {
            affectTimeScale = !affect;
        }
        #endregion

        /// <summary>
        /// 定制属性
        /// </summary>
        /// <param name="startValue">如果你在Hide时StartValue是无意义的，它会适应当前的属性继续变化</param>
        /// <param name="endValue"></param>
        /// <param name="animationTime"></param>
        public void SetAnimationAttribute(UIAnimateContent type, float startValue, float overValue, float animationTime = 0.42f)
        {
            animationSwitchDic[type] = true;
            AnimationData data = GetData(type);
            data.startValue = startValue;
            data.overValue = overValue;
            data.animationTime = animationTime;
        }

        private AnimationData GetData(UIAnimateContent type)
        {
            if (animationDic.ContainsKey(type) == false)
            {
                animationDic.Add(type, animationList[(int)type]);
            }
            return animationDic[type];
        }
        #endregion
    }
}