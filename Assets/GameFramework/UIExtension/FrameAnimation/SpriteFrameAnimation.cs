using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public class SpriteFrameAnimation : MonoBehaviour
    {
        public event Action animationFinished;
        public event Action animationStart;

        [SerializeField] private SpriteRenderer targetImage;
        [SerializeField] private int frameRate;
        [SerializeField] private FrameLoopType loopType;
        [SerializeField] private float timeScale;
        [SerializeField] private bool inverse;
        [SerializeField] private bool ignoreTimeScale;

        public List<Sprite> sprites;

        private FrameAnimator animator = new FrameAnimator();
        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = gameObject.GetComponent<SpriteRenderer>();
            }
            animator.animationStart += StartCallback;
            animator.animationFinished += FinishCallback;
        }

        private void StartCallback()
        {
            animationStart?.Invoke();
        }

        private void FinishCallback()
        {
            animationFinished?.Invoke();
        }

        private void Update()
        {
            if (inverse)
            {
                targetImage.sprite = sprites[sprites.Count - (animator.FrameIndex == 0 ? 1 : animator.FrameIndex)];
            }
            else
            {
                targetImage.sprite = sprites[animator.FrameIndex];
            }
        }

        public SpriteFrameAnimation StartFrameAnimation(List<Sprite> sprites, bool inverse, bool ignoreTimeScale = false)
        {
            this.sprites.Clear();
            this.sprites = sprites;
            this.inverse = inverse;
            this.ignoreTimeScale = ignoreTimeScale;
            animator.Initialize(sprites.Count, frameRate * (int)timeScale, ignoreTimeScale);
            animator.Start();
            return this;
        }

        public SpriteFrameAnimation SetFrameRate(int frameRate)
        {
            this.frameRate = frameRate;
            animator.Initialize(sprites.Count, frameRate * (int)timeScale, ignoreTimeScale);
            return this;
        }

        /// <summary>
        /// 设置播放速率 
        /// </summary>
        /// <param name="timeScale"></param>
        /// <returns></returns>
        public SpriteFrameAnimation SetTimeScale(float timeScale)
        {
            this.timeScale = timeScale;
            animator.Initialize(sprites.Count, frameRate * (int)timeScale, ignoreTimeScale);
            return this;
        }

        public SpriteFrameAnimation SetLoop(FrameLoopType type)
        {
            this.loopType = type;
            animator.SetLoop(loopType);
            return this;
        }

        public void StopFrameAnimation()
        {
            animator.Stop();
        }

        public void PauseFrameAnimation()
        {
            animator.Pause();
        }

        public void ResumeFrameAnimation()
        {
            animator.Resume();
        }
    }
}
