using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    public static class UIAnimationExtension
    {
        public static UIAnimation GetUIAnimation(this Transform tagert, Image mask = null)
        {
            UIAnimation uiAnimation = tagert.GetComponent<UIAnimation>();
            if (uiAnimation == null)
            {
                tagert.gameObject.AddComponent<UIAnimation>().InitAnimation(tagert, mask);
            }
            return tagert.GetComponent<UIAnimation>();
        }

        public static Transform HideUIAnimation(this Transform tagert)
        {
            GetUIAnimation(tagert).Hide();
            return tagert;
        }

        public static Transform PlayUIAnimation(this Transform target, UIAnimationStyle playState)
        {
            GetUIAnimation(target).PlayAnimation(playState);
            return target;
        }

        /// <summary>
        /// 播放panel的动画, 需要<see cref="CanvasGroup"/>
        /// </summary>
        /// <param name="_target"></param>
        /// <param name="_playState"></param>
        /// <returns></returns>
        public static Transform PlayUIAnimation4Panel(this Transform _target, UIAnimationStyle _playState)
        {
            if (UIAnimationStyle.Appear == _playState)
            {
                CanvasGroup canvasGroup = _target.GetOrAddComponent<CanvasGroup, Transform>();
                float alpha = 0;
                canvasGroup.alpha = alpha;
                _target.gameObject.SetActive(true);
                DOTween.To(() => alpha, _value =>
                {
                    alpha = _value;
                    canvasGroup.alpha = _value;
                }, 1, 0.17f);
                _target.localScale = Vector3.one * 0.7f;
                _target.DOScale(1.05f, 0.23f).OnComplete(() =>
                {
                    _target.DOScale(1f, 0.06f);
                });
            }

            return _target;
        }

        public static Transform SetUIAnimation(this Transform tagert, Image mask)
        {
            GetUIAnimation(tagert, mask);
            return tagert;
        }

        public static Transform PlayUIAnimation(this Transform target, UIAnimationStyle playState, UIAnimationDirection derType)
        {
            GetUIAnimation(target).PlayAnimation(playState, derType);
            return target;
        }

        public static Transform PlayUIAnimation(this Transform target, UIAnimationStyle playState, UIAnimationDirection der, float animationTime, bool hasMask = true)
        {
            GetUIAnimation(target).PlayAnimation(playState, animationTime, der, hasMask);
            return target;
        }

        #region 常用属性

        public static Transform SetUIDelay(this Transform tagert, float time)
        {
            GetUIAnimation(tagert).SetDelay(time);
            return tagert;
        }

        public static Transform OnUIStart(this Transform tagert, Action endCallback = null)
        {
            GetUIAnimation(tagert).OnStart(endCallback);
            return tagert;
        }

        public static Transform OnUIUpdate(this Transform tagert, Action<float> updateCallback = null)
        {
            GetUIAnimation(tagert).OnUpdate(updateCallback);
            return tagert;
        }

        public static Transform OnUIComplete(this Transform tagert, Action startCallback = null)
        {
            GetUIAnimation(tagert).OnComplete(startCallback);
            return tagert;
        }

        public static Transform SetUIEase(this Transform tagert, DG.Tweening.Ease ease)
        {
            GetUIAnimation(tagert).SetEase(ease);
            return tagert;
        }

        public static Transform SetUIEase(this Transform tagert, AnimationCurve curve)
        {
            GetUIAnimation(tagert).SetCurve(curve);
            return tagert;
        }

        public static Transform SetAnimationAttribute(this Transform tagert, UIAnimateContent type, float startValue, float overValue, float animationTime = 0.42f)
        {
            GetUIAnimation(tagert).SetAnimationAttribute(type, startValue, overValue, animationTime);
            return tagert;
        }
        #endregion

        #region 遮罩相关
        public static Transform SetMaskEnable(this Transform tagert, bool hasMask = true)
        {
            GetUIAnimation(tagert).SetMaskEnable(hasMask);
            return tagert;
        }

        public static Transform SetMaskSprite(this Transform tagert, Sprite sprite)
        {
            GetUIAnimation(tagert).SetMaskSprite(sprite);
            return tagert;
        }

        public static Transform SetMaskColor(this Transform tagert, Color color)
        {
            GetUIAnimation(tagert).SetMaskColor(color);
            return tagert;
        }

        public static Transform SetMaskClickCallBack(this Transform tagert, Action callBack)
        {
            GetUIAnimation(tagert).SetMaskCallBack(callBack);
            return tagert;
        }

        public static Transform SetMaskAlphaIntervalValue(this Transform tagert, float startValue, float endValue)
        {
            GetUIAnimation(tagert).SetMaskAlphaIntervalValue(startValue, endValue);
            return tagert;
        }

        public static Transform SetMaskAnimationTime(this Transform tagert, float animationTime)
        {
            GetUIAnimation(tagert).SetMaskAnimationTime(animationTime);
            return tagert;
        }
        #endregion

        #region  移动属性设置

        public static Transform SetMoveIntervalValue(this Transform tagert, float startValue, float overValue)
        {
            GetUIAnimation(tagert).SetMoveIntervalValue(startValue, overValue);
            return tagert;
        }
        #endregion

        #region  缩放属性设置

        public static Transform SetScaleIntervalValue(this Transform tagert, float startValue, float overValue)
        {
            GetUIAnimation(tagert).SetScaleIntervalValue(startValue, overValue);
            return tagert;
        }

        public static Transform SetScaleAnimtionTime(this Transform tagert, float animationTime)
        {
            GetUIAnimation(tagert).SetScaleAnimtionTime(animationTime);
            return tagert;
        }
        #endregion

        #region 旋转属性设置
        public static Transform SetRotationIntervalValue(this Transform tagert, float startValue, float overValue)
        {
            GetUIAnimation(tagert).SetRotationIntervalValue(startValue, overValue);
            return tagert;
        }

        public static Transform SetRotationAnimtionTime(this Transform tagert, float animationTime)
        {
            GetUIAnimation(tagert).SetRotationAnimtionTime(animationTime);
            return tagert;
        }
        #endregion

        #region 透明度属性设置
        public static Transform SetAlphaIntervalValue(this Transform tagert, float startValue, float overValue)
        {
            GetUIAnimation(tagert).SetAlphaIntervalValue(startValue, overValue);
            return tagert;
        }

        public static Transform SetAlphaAnimtionTime(this Transform tagert, float animationTime)
        {
            GetUIAnimation(tagert).SetAlphaAnimtionTime(animationTime);
            return tagert;
        }
        #endregion

        #region 是否受Unity TimeScale设置
        /// <summary>
        /// 受时间影响(如果设置为Ture,那么会受unity 的TimeScale 影响)
        /// </summary>
        public static Transform SetAffectByTimeScale(this Transform tagert, bool affectByUnity)
        {
            GetUIAnimation(tagert).SetAffectByTimeScale(affectByUnity);
            return tagert;
        }
        #endregion
    }
}