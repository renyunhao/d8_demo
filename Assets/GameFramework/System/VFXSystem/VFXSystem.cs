using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public class VFXGameData
    {
        public Action Event_OnVFXEnd;
        public string name;
        public GameObject vfx;
        public int timer;

        public void Clear()
        {
            Event_OnVFXEnd?.Invoke();
            Event_OnVFXEnd = null;
            name = "";
            vfx = null;
            timer = 0;
        }
    }

    public static partial class VFXSystem
    {
        /// <summary>
        /// 特效对象池
        /// </summary>
        private static Dictionary<string, GameObjectPool> vfxPoolDic = new Dictionary<string, GameObjectPool>();
        /// <summary>
        /// 特效数据对象池
        /// </summary>
        private static GenericPool<VFXGameData> vfxDataPool = new GenericPool<VFXGameData>();
        /// <summary>
        /// 正在使用的特效
        /// </summary>
        private static Dictionary<GameObject, (VFXGameData, Action)> usingManualRecycleVFX = new();
        /// <summary>
        /// 特效展示时间
        /// </summary>
        private static Dictionary<string, float> vfxLifeTimeDic = new Dictionary<string, float>();
        private static List<GameObject> tempList = new List<GameObject>();
        private static Transform poolRoot;

        public static void Initialize(Transform parent)
        {
            poolRoot = parent;
        }

        private static void InitializeVFX(string vfxName, float setTime = 1)
        {
            GameObject prefab = AssetSystem.Load<GameObject>(vfxName);
            GameObjectPool pool = new GameObjectPool(prefab, poolRoot);
            vfxPoolDic.Add(vfxName, pool);
            ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>();
            float time = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                var particleTime = VFXUtil.GetParticleTime(particles[i], true);
                if (i == 0)
                {
                    time = particleTime.delay + particleTime.duration + particleTime.life;
                }
                else
                {
                    float currentTime = particleTime.delay + particleTime.duration + particleTime.life;
                    if (time < currentTime)
                    {
                        time = currentTime;
                    }
                }
            }
            if (time == 0)
            {
                vfxLifeTimeDic.Add(vfxName, setTime);
            }
            else
            {
                vfxLifeTimeDic.Add(vfxName, time);
            }
        }

        /// <summary>
        /// 获取一个特效
        /// </summary>
        /// <param name="vfxName"></param>
        /// <param name="autoRecycle"></param>
        /// <param name="sortingLayer"></param>
        /// <param name="sortingOrder"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static GameObject GetVFXInstance(string vfxName, bool autoRecycle, string sortingLayer = "", int sortingOrder = -1, float duration = 0, float playSpeed = 1, Action recycleCallBack = null)
        {
            if (vfxPoolDic.ContainsKey(vfxName) == false)
            {
                InitializeVFX(vfxName);
            }
            GameObject vfxInstance = vfxPoolDic[vfxName].GetInstance();
            VFXGameData gameData = vfxDataPool.GetInstance();
            gameData.name = vfxName;
            gameData.vfx = vfxInstance;
            //gameData.lifeTime = vfxLifeTimeDic[vfxName];
            ParticleSystem[] ps = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            if (string.IsNullOrEmpty(sortingLayer) == false || sortingOrder != -1 || duration != 0)
            {
                for (int i = 0; i < ps.Length; i++)
                {
                    var main = ps[i].main;
                    main.simulationSpeed = playSpeed;

                    ParticleSystemRenderer render = ps[i].GetComponent<ParticleSystemRenderer>();
                    if (string.IsNullOrEmpty(sortingLayer) == false)
                    {
                        render.sortingLayerName = sortingLayer;
                    }
                    if (sortingOrder != -1)
                    {
                        render.sortingOrder += sortingOrder;
                    }
                    if (duration != 0)
                    {
                        ParticleSystem.MainModule module = ps[i].main;
                        ps[i].Stop();
                        module.loop = false;
                        module.duration = duration /*/ playSpeed*/;
                        ps[i].Play();
                    }
                }
            }
            if (autoRecycle)
            {
                duration = duration == 0 ? 1 : duration;
                gameData.timer = TimerSystem.StartTimerWithTimeInterval(duration, VFXEndCallback, vfxInstance);
            }
            usingManualRecycleVFX.Add(vfxInstance, (gameData, recycleCallBack));
            return vfxInstance;
        }

        /// <summary>
        /// 回收特效
        /// </summary>
        /// <param name="vfxInstance"></param>
        public static void RecycleVFXInstance(GameObject vfxInstance)
        {
            (var gameData, var action) = usingManualRecycleVFX[vfxInstance];
            action?.Invoke();
            vfxPoolDic[gameData.name].RecycleInstance(vfxInstance);
            if (TimerSystem.GetTimerStatus(gameData.timer) != TimerStatus.Closed)
            {
                TimerSystem.Close(gameData.timer);
            }
            gameData.Clear();
            vfxDataPool.RecycleInstance(gameData);
            usingManualRecycleVFX.Remove(vfxInstance);
        }

        private static void VFXEndCallback(object paraml)
        {
            GameObject vfxInstance = (GameObject)paraml;
            if (vfxInstance == null)
            {
                GameFramework.Debug.LogError("特效计时结束回调对象为空");
                return;
            }
            if (usingManualRecycleVFX.ContainsKey(vfxInstance))
            {
                (var data, var action) = usingManualRecycleVFX[vfxInstance];
                if (vfxPoolDic.ContainsKey(data.name))
                {
                    action?.Invoke();
                    vfxPoolDic[data.name].RecycleInstance(data.vfx);
                }
                else
                {
                    GameFramework.Debug.LogError($"特效对象池回收时发现：{data.name}不在");
                }
                usingManualRecycleVFX.Remove(vfxInstance);

                data.Clear();
                vfxDataPool.RecycleInstance(data);
            }
            else
            {
                GameFramework.Debug.LogError($"特效计时结束发现特效不在使用列表中：{vfxInstance.name}");
            }
        }

        /// <summary>
        /// 清理所有特效
        /// </summary>
        public static void ClearAllAutoRecycleVFX()
        {
            tempList.Clear();
            foreach (var item in usingManualRecycleVFX)
            {
                //自动回收的特效
                if (item.Value.Item1.timer != 0)
                {
                    if (TimerSystem.GetTimerStatus(item.Value.Item1.timer) == TimerStatus.Running)
                    {
                        TimerSystem.Close(item.Value.Item1.timer);
                    }
                    tempList.Add(item.Key);
                    //回收
                    if (item.Value.Item1.vfx != null)
                    {
                        vfxPoolDic[item.Value.Item1.name].RecycleInstance(item.Value.Item1.vfx);
                    }
                    if (item.Value.Item1 != null)
                    {
                        item.Value.Item1.Clear();
                        vfxDataPool.RecycleInstance(item.Value.Item1);
                    }
                }
            }

            foreach (var item in tempList)
            {
                if (usingManualRecycleVFX.ContainsKey(item))
                {
                    usingManualRecycleVFX.Remove(item);
                }
            }
        }

        public static float GetVFXLifeTime(string vfxName)
        {
            vfxLifeTimeDic.TryGetValue(vfxName, out var lifeTime);
            return lifeTime;
        }
    }
}