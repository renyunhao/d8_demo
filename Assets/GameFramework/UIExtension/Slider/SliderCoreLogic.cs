using DG.Tweening;
using System;
using UnityEngine;

namespace GameFramework
{
    public enum SliderTransition
    {
        Instant,
        Animation
    }

    public class SliderCoreLogic
    {
        /// <summary>
        /// 进度到达1的事件，一次动画过程中可能多次触发
        /// </summary>
        public event Action Event_ReachMax;
        /// <summary>
        /// 整个动画结束事件，一次动画只触发一次
        /// </summary>
        public event Action Event_TransitionFinish;
        private SliderTransition transition;
        private float progress;
        /// <summary>
        /// progress从0到1的时间，单位秒
        /// </summary>
        private float speed;

        /// <summary>
        /// 数值动态变化时，到达1后，再次从0开始增长的次数
        /// </summary>
        private int overflowCount;
        private float transitionEndValue;
        private bool isTransitioning;

        public SliderTransition Transition => transition;
        public float Speed => speed;
        public bool IsTransitioning => isTransitioning;

        private bool ignoreTimeScale = false;
        private Tweener tweener;

        public float Progress
        {
            get
            {
                return progress;
            }
            set
            {
                if (transition == SliderTransition.Instant)
                {
                    SetProgressInstant(value);
                }
                else if (transition == SliderTransition.Animation)
                {
                    int newOverflowCount = 0;
                    float newTransitionEndValue = 0;
                    if (value > 1)
                    {
                        newOverflowCount = Mathf.FloorToInt(value);
                        newTransitionEndValue = value - newOverflowCount;
                    }
                    else
                    {
                        newOverflowCount = 0;
                        newTransitionEndValue = value;
                    }

                    if (transitionEndValue != newTransitionEndValue || overflowCount != newOverflowCount)
                    {
                        overflowCount = newOverflowCount;
                        transitionEndValue = newTransitionEndValue;
                        StartTransition();
                    }
                }
            }
        }

        public void Initialize(SliderTransition transition, float speed = 0.5f)
        {
            this.transition = transition;
            this.speed = speed;
        }

        public void SetProgressInstant(float progress)
        {
            this.progress = progress;
            this.transitionEndValue = progress;
        }

        public void SetIgnoreTimeScale(bool value)
        {
            ignoreTimeScale = value;
        }

        public void Stop()
        {
            if (tweener.IsActive())
            {
                tweener.Kill();
            }
            isTransitioning = false;
        }

        private void StartTransition()
        {
            if (tweener.IsActive())
            {
                tweener.Kill();
            }
            isTransitioning = true;
            if (overflowCount > 0)
            {
                tweener = DOTween.To(ValueGetter, ValueSetter, 1, speed * (1 - progress)).SetEase(Ease.Linear).OnComplete(OverflowTransitionComplete);
            }
            else
            {
                tweener = DOTween.To(ValueGetter, ValueSetter, transitionEndValue, speed * Mathf.Abs(transitionEndValue - progress)).SetEase(Ease.Linear).OnComplete(TransitionComplete);
            }
            if (ignoreTimeScale)
            {
                tweener.SetUpdate(ignoreTimeScale);
            }
        }

        private float ValueGetter()
        {
            return progress;
        }

        private void ValueSetter(float newValue)
        {
            progress = newValue;
        }

        private void OverflowTransitionComplete()
        {
            progress = 0;
            overflowCount--;
            Event_ReachMax?.Invoke();
            StartTransition();
        }

        private void TransitionComplete()
        {
            isTransitioning = false;
            Event_TransitionFinish?.Invoke();
        }
    }
}