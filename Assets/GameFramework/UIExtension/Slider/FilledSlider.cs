using System;
using System.Collections;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFramework
{
    public class FilledSlider : UIBehaviour
    {
        public event Action Event_ReachMax;
        public event Action Event_TransitionFinish;

        [SerializeField]
        public Image image;
        [SerializeField]
        public RectTransform handler;
        [SerializeField]
        [Range(0, 1)]
        public float progress = 0.5f;
        [SerializeField]
        public SliderTransition transition = SliderTransition.Instant;
        [SerializeField]
        public float speed = 0.5f;
        [SerializeField]
        public Image.FillMethod fillMethod = Image.FillMethod.Horizontal;
        [SerializeField]
        public Image.OriginHorizontal originHorizontal = Image.OriginHorizontal.Left;
        [SerializeField]
        public Image.OriginVertical originVertical = Image.OriginVertical.Bottom;
        [SerializeField]
        public Image.Origin90 origin90 = Image.Origin90.BottomLeft;
        [SerializeField]
        public Image.Origin360 origin360 = Image.Origin360.Bottom;

        private SliderCoreLogic coreLogic = new SliderCoreLogic();

        public float Progress
        {
            get
            {
                return progress;
            }
            set
            {
                progress = value;
                coreLogic.Progress = value;
                if (transition == SliderTransition.Instant)
                {
                    image.fillAmount = coreLogic.Progress;
                    if (handler != null)
                    {
                        handler.anchoredPosition = new Vector2((this.transform as RectTransform).sizeDelta.x * coreLogic.Progress , 0);
                    }
                }
            }
        }

        public bool IsTransitioning => coreLogic.IsTransitioning;

        protected override void Awake()
        {
            base.Awake();
            progress = image.fillAmount;
            coreLogic.Initialize(transition, speed);
            coreLogic.SetProgressInstant(image.fillAmount);
            coreLogic.Event_ReachMax += CoreLogic_Event_ReachMax;
            coreLogic.Event_TransitionFinish += CoreLogic_Event_TransitionFinish;
            InitContent();
        }

        private void Update()
        {
            if (coreLogic.IsTransitioning)
            {
                this.progress = coreLogic.Progress;
                image.fillAmount = coreLogic.Progress;
                if (handler != null)
                {
                    handler.anchoredPosition = new Vector2((this.transform as RectTransform).sizeDelta.x * coreLogic.Progress, 0);
                }
            }
        }

        public void SetProgressInstant(float progress)
        {
            if (this.Progress != progress)
            {
                this.progress = progress;
                coreLogic?.SetProgressInstant(progress);
                image.fillAmount = progress;
                if (handler != null)
                {
                    handler.anchoredPosition = new Vector2((this.transform as RectTransform).sizeDelta.x * coreLogic.Progress, 0);
                }
            }
        }

        public void SetIgnoreTimeScale(bool value)
        {
            coreLogic.SetIgnoreTimeScale(value);
        }

        public void Stop()
        {
            coreLogic.Stop();
        }

        private void InitContent()
        {
            if (image != null)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = fillMethod;
                if (image.fillMethod == Image.FillMethod.Horizontal)
                {
                    image.fillOrigin = (int)originHorizontal;
                }
                else if (image.fillMethod == Image.FillMethod.Vertical)
                {
                    image.fillOrigin = (int)originVertical;
                }
                else if (image.fillMethod == Image.FillMethod.Radial90)
                {
                    image.fillOrigin = (int)origin90;
                }
                else if (image.fillMethod == Image.FillMethod.Radial180)
                {
                    image.fillOrigin = (int)origin360;
                }
                else if (image.fillMethod == Image.FillMethod.Radial360)
                {
                    image.fillOrigin = (int)origin360;
                }
            }
            if (handler != null)
            {
                if (Application.isPlaying)
                {
                    SetHandlerAnchorImmediately();
                }
#if UNITY_EDITOR
                else
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(SetHandlerAnchor());
                }
#endif
            }
        }

        private IEnumerator SetHandlerAnchor()
        {
            yield return null;
            SetHandlerAnchorImmediately();
        }

        private void SetHandlerAnchorImmediately()
        {
            handler.anchorMin = new Vector2(0, 0.5f);
            handler.anchorMax = new Vector2(0, 0.5f);
            handler.anchoredPosition = new Vector2((this.transform as RectTransform).sizeDelta.x * coreLogic.Progress, 0);
        }

        private void CoreLogic_Event_ReachMax()
        {
            this.progress = coreLogic.Progress;
            image.fillAmount = coreLogic.Progress;
            if (handler != null)
            {
                handler.anchoredPosition = new Vector2((this.transform as RectTransform).sizeDelta.x * coreLogic.Progress, 0);
            }
            Event_ReachMax?.Invoke();
        }

        private void CoreLogic_Event_TransitionFinish()
        {
            this.progress = coreLogic.Progress;
            image.fillAmount = coreLogic.Progress;
            if (handler != null)
            {
                handler.anchoredPosition = new Vector2((this.transform as RectTransform).sizeDelta.x * coreLogic.Progress, 0);
            }
            Event_TransitionFinish?.Invoke();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            InitContent();
        }
#endif
    }
}