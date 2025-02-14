using System.Collections.Generic;
using TMPro;
using Unity.Profiling;
using UnityEngine;

namespace GameFramework
{
    public class PerformanceBrowser : BindableMonoBehaviour
    {
        [System.Serializable]
        private struct FPSColor
        {
            public Color color;
            public float percent;
        }

        [SerializeField]
        private FPSColor[] fpsColors = new FPSColor[] {
            new FPSColor() { color = Color.green, percent = 0.95f },
            new FPSColor() { color = Color.yellow, percent = 0.45f },
            new FPSColor() { color = new Color(1, 0.5f, 0), percent = 0.24f },
            new FPSColor() { color = Color.red, percent = 0f },
        };

        private static PerformanceBrowser instance;
        public static PerformanceBrowser Instance => instance;

        public Canvas canvas;
        public RectTransform canvasRT;
        public TextMeshProUGUI rawFPSLabel;
        public TextMeshProUGUI averageFPSLabel;
        public TextMeshProUGUI maxFPSLabel;
        public TextMeshProUGUI minFPSLabel;

        public TextMeshProUGUI gcReservedMemoryLabel;
        public TextMeshProUGUI systemUsedMemoryLabel;

        public bool showFPS = false;
        public bool initializeFPS = false;
        public bool limitFPSWithRefreshRate = true;
        public float startRecordDelayTime = 1;

        private float rawDeltaTime = 0;
        private float minDeltaTime = float.MaxValue;
        private float maxDeltaTime = float.MinValue;
        private float averageDeltaTime = 0;
        private Queue<float> deltaTimeHistory;
        private float deltaTmeHistorySum = 0;
        private int deviceMaxFrameRate;

        ProfilerRecorder gcReservedMemoryRecorder;
        ProfilerRecorder systemUsedMemoryRecorder;

        public int RawFPS => Mathf.RoundToInt(1f / rawDeltaTime);
        public int AverageFPS => Mathf.RoundToInt(1f / averageDeltaTime);
        public int MaxFPS => Mathf.RoundToInt(1f / minDeltaTime);
        public int MinFPS => Mathf.RoundToInt(1f / maxDeltaTime);

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("只能有一个PerformanceBrowser");
            }
            instance = this;
            canvas.enabled = showFPS;
        }

        void Start()
        {
            if (initializeFPS)
            {
                if (limitFPSWithRefreshRate)
                {
                    deviceMaxFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
                    Debug.Log($"当前设备屏幕刷新率为：{deviceMaxFrameRate}，将帧率也设置为：{deviceMaxFrameRate}");
                    if (deviceMaxFrameRate < 0)
                    {
                        Debug.Log($"屏幕刷新率数值异常，纠正为30");
                        deviceMaxFrameRate = 30;
                    }
                    Application.targetFrameRate = deviceMaxFrameRate;
                }
                else
                {
                    deviceMaxFrameRate = 30;
                    Debug.Log($"将帧率设置为：{deviceMaxFrameRate}");
                    Application.targetFrameRate = deviceMaxFrameRate;
                }
            }
            else
            {
                deviceMaxFrameRate = Application.targetFrameRate;
                if (deviceMaxFrameRate == -1)
                {
                    deviceMaxFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
                }
            }
            deltaTimeHistory = new Queue<float>(deviceMaxFrameRate);

            gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");

            var floatingLayout = this.GetComponentInChildren<FloatingLayout>();
            if (floatingLayout != null)
            {
                floatingLayout.Event_OnDoubleClick += ResetFPS;
            }

            DontDestroyOnLoad(canvasRT);
        }

        void Update()
        {
            if (Time.timeSinceLevelLoad < startRecordDelayTime)
            {
                //游戏启动前一小段时间不记录，因为帧率有剧烈波动
                return;
            }
            rawDeltaTime = Time.unscaledDeltaTime;
            if (minDeltaTime > rawDeltaTime)
            {
                minDeltaTime = rawDeltaTime;
            }
            if (maxDeltaTime < rawDeltaTime)
            {
                maxDeltaTime = rawDeltaTime;
            }
            if (deltaTimeHistory.Count >= deviceMaxFrameRate)
            {
                float removeValue = deltaTimeHistory.Dequeue();
                deltaTmeHistorySum -= removeValue;
            }
            deltaTimeHistory.Enqueue(rawDeltaTime);
            deltaTmeHistorySum += rawDeltaTime;
            averageDeltaTime = deltaTmeHistorySum / deltaTimeHistory.Count;

            canvas.enabled = showFPS;
            if (showFPS)
            {
                ShowFPS(rawDeltaTime, rawFPSLabel);
                ShowFPS(averageDeltaTime, averageFPSLabel);
                ShowFPS(minDeltaTime, maxFPSLabel);
                ShowFPS(maxDeltaTime, minFPSLabel);

                ShowMemory(gcReservedMemoryRecorder, gcReservedMemoryLabel);
                ShowMemory(systemUsedMemoryRecorder, systemUsedMemoryLabel);
            }
        }

        public void ResetFPS()
        {
            rawDeltaTime = 0;
            minDeltaTime = float.MaxValue;
            maxDeltaTime = float.MinValue;
            averageDeltaTime = 0;
            deltaTimeHistory.Clear();
            deltaTmeHistorySum = 0;
        }

        private void ShowFPS(float deltaTime, TextMeshProUGUI textComponent)
        {
            int fps = Mathf.RoundToInt(1f / deltaTime);
            fps = Mathf.Clamp(fps, 0, TimeUtil.Number_TrimZero.Length - 1);
            textComponent.text = TimeUtil.Number_TrimZero[fps];
            textComponent.color = GetFPSColor(fps);
        }

        private void ShowMemory(ProfilerRecorder pr, TextMeshProUGUI textComponent)
        {
            if (pr.Valid)
            {
                textComponent.text = pr.LastValue.ToByteKMB();
            }
        }

        private Color GetFPSColor(int fps)
        {
            foreach (var fpsColor in fpsColors)
            {
                float percent = (float)fps / deviceMaxFrameRate;

                if (percent >= fpsColor.percent)
                {
                    return fpsColor.color;
                }
            }
            return Color.white;
        }
    }
}
