using System;
using UnityEngine;

namespace GameFramework
{
    public enum FrameLoopType
    {
        None = 0,
        Restart = 1,
        YoYo = 2,
    }

    public class FrameAnimator
    {
        public event Action animationFinished;
        public event Action animationStart;

        private int totalFrameCount;
        private float frameDelta;
        private bool isInverse = false;
        private FrameLoopType loopType = FrameLoopType.None;

        private float startTime;
        private int frameIndex;
        private bool isPaused;
        private bool ignoreTimeScale;

        public int FrameIndex;
        public bool IgnoreTimeScale { get => ignoreTimeScale; set => ignoreTimeScale = value; }

        public void Initialize(int totalFrameCount, int frameRate, bool ingoreTimeScale = false)
        {
            this.totalFrameCount = totalFrameCount;
            this.ignoreTimeScale = ingoreTimeScale;
            frameDelta = 1f / frameRate;
        }

        public void Update(float dt)
        {
            if (!isPaused)
            {
                float frameTotalTime = frameDelta * frameIndex;// 播放了多少帧 所需要的时间总和
                if (isInverse)// YoYo的倒放
                {
                    startTime -= dt;// 时间减等
                    if (startTime <= frameTotalTime)// 该更新帧了
                    {
                        frameIndex = (int)(startTime / frameDelta); // 除法计算当前是第几帧
                        if (frameIndex <= 0) //播放完成
                        {
                            frameIndex = 0;
                            startTime = 0;
                            isInverse = false;
                            animationFinished?.Invoke();
                            animationStart?.Invoke();
                        }
                        FrameIndex = frameIndex; //将 计算完成后的结果赋值给外部使用 
                    }
                }
                else
                {
                    startTime += dt;// 时间加等
                    if (startTime >= frameTotalTime)// 该更新帧了
                    {
                        frameIndex = (int)(startTime / frameDelta); // 除法计算当前是第几帧
                        if (frameIndex == totalFrameCount) //播放完成
                        {
                            switch (loopType)
                            {
                                case FrameLoopType.Restart:
                                    frameIndex = 0;
                                    startTime = 0;
                                    animationFinished?.Invoke();
                                    animationStart?.Invoke();
                                    break;
                                case FrameLoopType.None:
                                    animationFinished?.Invoke();
                                    Stop();
                                    break;
                                case FrameLoopType.YoYo:
                                    frameIndex = totalFrameCount - 1;
                                    isInverse = true;
                                    animationFinished?.Invoke();
                                    animationStart?.Invoke();
                                    break;
                                default:
                                    break;
                            }
                        }
                        FrameIndex = frameIndex; //将 计算完成后的结果赋值给外部使用 
                    }
                }
            }
        }

        public void Start()
        {
            frameIndex = 0;
            if (ignoreTimeScale)
            {
                startTime = Time.realtimeSinceStartup;
                UpdateUtil.AddUpdate(Update, true);
            }
            else
            {
                startTime = Time.deltaTime;
                UpdateUtil.AddUpdate(Update);
            }
            animationStart?.Invoke();
        }

        public void SetLoop(FrameLoopType type)
        {
            loopType = type;
        }

        public void Stop()
        {
            UpdateUtil.RemoveUpdate(Update);
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }
    }
}
