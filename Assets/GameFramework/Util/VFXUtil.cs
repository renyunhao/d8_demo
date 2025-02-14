using UnityEngine;

namespace GameFramework
{
    public static class VFXUtil
    {
        /// <summary>
        /// 获取特效时间
        /// </summary>
        /// <param name="particle">特效</param>
        /// <param name="includeChildren">是否包含子物体</param>
        /// <returns>（最大延迟时间，最大持续时间，最大存活时间）</returns>
        public static (float delay, float duration, float life) GetParticleTime(this ParticleSystem particle, bool includeChildren = false)
        {
            float maxDelayTime = GetParticleMaxDelayTime(particle, includeChildren);
            float maxDuration = GetParticleMaxDuration(particle, includeChildren);
            float maxLifeTime = GetParticleMaxLifeTime(particle, includeChildren);
            return (maxDelayTime, maxDuration, maxLifeTime);
        }

        public static float GetParticleMaxDelayTime(this ParticleSystem particle, bool includeChildren = false)
        {
            float maxDelayTime = 0;
            switch (particle.main.startDelay.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    maxDelayTime = particle.main.startDelay.constant;
                    break;
                case ParticleSystemCurveMode.Curve:
                    foreach (var item in particle.main.startDelay.curve.keys)
                    {
                        if (maxDelayTime > item.value)
                        {
                            maxDelayTime = item.value;
                        }
                    }
                    break;
                case ParticleSystemCurveMode.TwoCurves:
                    foreach (var item in particle.main.startDelay.curveMax.keys)
                    {
                        if (maxDelayTime > item.value)
                        {
                            maxDelayTime = item.value;
                        }
                    }
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    maxDelayTime = particle.main.startDelay.constantMax;
                    break;
                default:
                    break;
            }
            if (includeChildren)
            {
                ParticleSystem[] pss = particle.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var item in pss)
                {
                    float targetValue = GetParticleMaxDelayTime(item);
                    if (maxDelayTime < targetValue)
                    {
                        maxDelayTime = targetValue;
                    }
                }
            }
            return maxDelayTime;
        }

        public static float GetParticleMaxDuration(this ParticleSystem particle, bool includeChildren = false)
        {
            float maxDuration = particle.main.duration;
            if (includeChildren)
            {
                ParticleSystem[] pss = particle.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var item in pss)
                {
                    float targetValue = GetParticleMaxDuration(item);
                    if (maxDuration < targetValue)
                    {
                        maxDuration = targetValue;
                    }
                }
            }
            return maxDuration;
        }

        public static float GetParticleMaxLifeTime(this ParticleSystem particle, bool includeChildren = false)
        {
            float maxLifeTime = 0;
            switch (particle.main.startLifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    maxLifeTime = particle.main.startLifetime.constant;
                    break;
                case ParticleSystemCurveMode.Curve:
                    foreach (var item in particle.main.startLifetime.curve.keys)
                    {
                        if (maxLifeTime > item.value)
                        {
                            maxLifeTime = item.value;
                        }
                    }
                    break;
                case ParticleSystemCurveMode.TwoCurves:
                    foreach (var item in particle.main.startLifetime.curveMax.keys)
                    {
                        if (maxLifeTime > item.value)
                        {
                            maxLifeTime = item.value;
                        }
                    }
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    maxLifeTime = particle.main.startLifetime.constantMax;
                    break;
                default:
                    break;
            }
            if (includeChildren)
            {
                ParticleSystem[] pss = particle.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var item in pss)
                {
                    float targetValue = GetParticleMaxLifeTime(item);
                    if (maxLifeTime < targetValue)
                    {
                        maxLifeTime = targetValue;
                    }
                }
            }
            return maxLifeTime;
        }
    }
}
