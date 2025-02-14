//#define DETAIL_LOG
using UnityEngine;
using System;

namespace GameFramework
{
    public enum GameDisplayOrientation
    {
        Portrait,
        Landscape
    }

    /// <summary>
    /// 刘海屏，异形屏适配脚本
    /// </summary>
    public class NotchScreenAdaptor : MonoBehaviour
    {
        /// <summary>
        /// 全局测试用的安全区
        /// </summary>
        public static Rect GlobalTestSafeArea = new Rect(0, 0, 720, 1180);
        /// <summary>
        /// 全局测试开关
        /// </summary>
        public static bool IsGlobalTest = false;
        /// <summary>
        /// 适配触发器，设置为true后会触一次
        /// </summary>
        private static bool AdapteTrigger = false;

        /// <summary>
        /// 使用前一定要注册的Android设备安全区获取方法
        /// </summary>
        public static Func<string> AndroidScreenSafeAreaProvider;

        /// <summary>
        /// 游戏设计的屏幕方向，只能使用横屏或竖屏
        /// </summary>
        public static GameDisplayOrientation GameDisplayOrientation = GameDisplayOrientation.Portrait;

        /// <summary>
        /// 每次适配完成后触发
        /// </summary>
        public event Action<RectTransform> Event_AdaptationChanged;

        /// <summary>
        /// UI设计宽度
        /// </summary>
        public static int DesignWidth = 720;
        /// <summary>
        /// UI设计高度
        /// </summary>
        public static int DesignHeight = 1280;

        /// <summary>
        /// 最大宽高比
        /// </summary>
        public static float MaxAspect = 768f / 1024;
        /// <summary>
        /// 最小宽高比
        /// </summary>
        public static float MinAspect = 720f / 1700;

        /// <summary>
        /// 设计宽高比
        /// </summary>
        public static float DesignAspect => (float)DesignWidth / DesignHeight;

        /// <summary>
        /// 最终的安全区
        /// </summary>
        private static Rect safeArea;
        /// <summary>
        /// 屏幕宽度的物理像素
        /// </summary>
        private static int screenWidth;
        /// <summary>
        /// 屏幕高度的物理像素
        /// </summary>
        private static int screenHeight;

        /// <summary>
        /// 自定义的边距补偿，四个参数分别表示，上左下右边额外缩进的距离
        /// </summary>
        public Vector4 customPatch = new Vector4(0, 0, 0, 0);

        /// <summary>
        /// 最终的屏幕方向
        /// </summary>
        private ScreenOrientation orientation = ScreenOrientation.LandscapeLeft;
        /// <summary>
        /// 测试用的屏幕方向
        /// </summary>
        public ScreenOrientation testOrientation;
        /// <summary>
        /// 测试用的安全区
        /// </summary>
        public Rect testSafeArea = new Rect(0, 0, 720, 1180);
        /// <summary>
        /// 测试开关
        /// </summary>
        public bool isTest = false;

        public static Rect SafeArea
        {
            get
            {
                return safeArea;
            }
        }

        private RectTransform rectTransform;

        public static void SetGlobalTest(bool test)
        {
            IsGlobalTest = test;
            AdapteTrigger = true;
            GlobalTestSafeArea.width = Screen.width;
            GlobalTestSafeArea.height = Screen.height - 100;
        }

        private void Start()
        {
            //Screen.width与Screen.height在某些Android机上，获取到的值是相反的，会导致计算错误
            //所以这里要根据屏幕的方向纠正
            if (GameDisplayOrientation == GameDisplayOrientation.Portrait)
            {
                //竖屏宽比高小
                screenWidth = Mathf.Min(Screen.width, Screen.height);
                screenHeight = Mathf.Max(Screen.width, Screen.height);
            }
            else if (GameDisplayOrientation == GameDisplayOrientation.Landscape)
            {
                //横屏宽比高大
                screenWidth = Mathf.Max(Screen.width, Screen.height);
                screenHeight = Mathf.Min(Screen.width, Screen.height);
            }
#if DETAIL_LOG
            Debug.LogFormat("screenWidth: {0}, screenHeight: {1}", screenWidth, screenHeight);
#endif
            rectTransform = this.GetComponent<RectTransform>();
            if (IsGlobalTest)
            {
                safeArea = GlobalTestSafeArea;
                orientation = testOrientation;
            }
            else
            {
                safeArea = isTest ? testSafeArea : GetDeviceSafeArea();
                orientation = isTest ? testOrientation : Screen.orientation;
            }

            AdjustUILayoutForNotch(rectTransform, safeArea);
            Event_AdaptationChanged?.Invoke(rectTransform);
        }

        private void Update()
        {
            if (Time.frameCount % 10 == 0)
            {
                CheckOrientation();
            }
        }

        private Rect GetDeviceSafeArea()
        {
            Rect safeArea = Screen.safeArea;

#if DETAIL_LOG
            Debug.Log("Screen.safeArea: " + safeArea);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            //Screen.safeArea在Android设备上不一定能拿到正确的值 ，因此从Android原生端获取安全区
            if (AndroidScreenSafeAreaProvider != null)
            {
                string androidRawSafeArea = AndroidScreenSafeAreaProvider();
#if DETAIL_LOG
                Debug.Log("androidRawSafeArea: " + androidRawSafeArea);
#endif
                if (string.IsNullOrEmpty(androidRawSafeArea) == false)
                {
                    string[] strs = androidRawSafeArea.Split(',');

                    int x = int.Parse(strs[0]);
                    int y = int.Parse(strs[3]);
                    int width = screenWidth - x - int.Parse(strs[2]);
                    int height = screenHeight - y - int.Parse(strs[1]);

                    //安全区的宽和高在某些Android机上值是相反的，会导致计算错误，因此要进行数值大小比较纠正
                    int correctedWidth = Mathf.Min(width, height);
                    int correctedHeight = Mathf.Max(width, height);
                    if (GameDisplayOrientation == GameDisplayOrientation.Portrait)
                    {
                        //竖屏宽比高小
                        correctedWidth = Mathf.Min(width, height);
                        correctedHeight = Mathf.Max(width, height);
                    }
                    else if (GameDisplayOrientation == GameDisplayOrientation.Landscape)
                    {
                        //横屏宽比高大
                        correctedWidth = Mathf.Max(width, height);
                        correctedHeight = Mathf.Min(width, height);
                    }

                    safeArea = new Rect(x, y, correctedWidth, correctedHeight);
            
#if DETAIL_LOG
                    Debug.Log("Android native safeArea: " + safeArea);
#endif
                }
            }
            else
            {
                Debug.LogError("未提供Android原生端屏幕安全区数值，请注册AndroidScreenSafeAreaProvider");
            }
#endif
            return safeArea;
        }

        private void CheckOrientation()
        {
            var targetOrientation = (isTest || IsGlobalTest) ? testOrientation : Screen.orientation;
            if (orientation != targetOrientation || AdapteTrigger)
            {
                AdapteTrigger = false;
                orientation = targetOrientation;
                if (IsGlobalTest)
                {
                    safeArea = GlobalTestSafeArea;
                }
                else
                {
                    safeArea = isTest ? testSafeArea : GetDeviceSafeArea();
                }
                AdjustUILayoutForNotch(rectTransform, safeArea);
                Event_AdaptationChanged?.Invoke(rectTransform);
            }
        }

        /// <summary>
        /// 适配有留海屏幕的UI调整算法
        /// </summary>
        /// <param name="outline">outline是指某个UI界面的根物体的一个子物体，该子物体的RectTransform设定为宽高皆为Stretch,上下左右边缘距离为0，轴点为0.5,0.5</param>
        /// <param name="safeArea">屏幕安全区域</param>
        public void AdjustUILayoutForNotch(RectTransform outline, Rect safeArea)
        {
#if DETAIL_LOG
            Debug.LogFormat("orientation changed to: {0}, current safeArea : {1}, screenWidth: {2}, screenHeight: {3}", orientation, safeArea, screenWidth, screenHeight);
#endif
            float screenAspect = screenWidth * 1.0f / screenHeight;

#if DETAIL_LOG
            Debug.LogFormat("screenAspect: {0}", screenAspect);
#endif
            if (screenAspect >= DesignAspect)
            {
#if DETAIL_LOG
                Debug.Log("screenAspect >= DESIGN_ASPECT");
#endif
                //当“安全区宽高比”比“设计宽高比”大时，但小于“最大宽高比”时，UI适配方案为高度固定为SCREEN_DESIGN_HEIGHT，宽度根据实际比例计算
                float screenUIWidth = DesignHeight * screenAspect;
                float screenUIHeight = DesignHeight;
                float uiScale = (float)DesignHeight / screenHeight;

                //把安全区也转换为UI像素
                safeArea = new Rect(safeArea.x * uiScale, safeArea.y * uiScale, safeArea.width * uiScale, safeArea.height * uiScale);
#if DETAIL_LOG
                Debug.Log("safeArea换算为UI像素 : " + safeArea);
#endif
                float widthDiff = screenUIWidth - safeArea.width;
#if DETAIL_LOG
                Debug.Log("widthDiff : " + widthDiff);
#endif

                bool canApplyPatch = true;

	            if (screenAspect >= MaxAspect)
	            {
	                float maxUIWidth = DesignHeight * MaxAspect;
#if DETAIL_LOG
	                Debug.Log("maxUIWidth : " + maxUIWidth);
#endif

	                //比较“安全区宽”和“最大宽”
	                if (safeArea.width >= maxUIWidth)
	                {
	                    //此时，应该使用在“安全区”里居中的“最大宽高比”区域作为新的“安全区”
	                    float safeAreaXOffset = (safeArea.width - maxUIWidth) / 2;
	                    safeArea.x += safeAreaXOffset;
	                    safeArea.width -= safeAreaXOffset * 2;
#if DETAIL_LOG
	                    Debug.Log("safeArea.width >= maxUIWidth，使用在“安全区”里居中的“最大宽高比”区域作为新的“安全区”: " + safeArea);
#endif
	                    widthDiff = screenUIWidth - safeArea.width;
#if DETAIL_LOG
                        Debug.Log("widthDiff更新 : " + widthDiff);
#endif

                        //不可以再应用补偿了
                        canApplyPatch = false;
	                }
	                else
	                {
	                    //此时就按安全区适配，如果UI重叠了，就说明无法适配该设备
	                }
	            }
                //计算safeArea相对于完整屏幕的各方向偏移（UI像素）
                float top = screenUIHeight - (safeArea.height + safeArea.y);
                float bottom = safeArea.y;
                float left = safeArea.x;
                float right = screenUIWidth - (safeArea.width + safeArea.x);
#if DETAIL_LOG
                Debug.Log($"top : {top}, bottom: {bottom}, left: {left}, right: {right}");
#endif

                //由于某些界面的按钮距离边界有一定的距离，在适配了安全区后，可能在视觉上偏移过远不好看，因此需要人工补偿一些距离回来
	            float leftPatch = (canApplyPatch && left > 0.1f) ? customPatch.x : 0;
	            float rightPatch = (canApplyPatch && right > 0.1f) ? customPatch.y : 0;
                float topPatch = (canApplyPatch && top > 0.1f) ? customPatch.z : 0;
                float bottomPatch = (canApplyPatch && bottom > 0.1f) ? customPatch.w : 0;
#if DETAIL_LOG
                Debug.Log($"leftPatch : {leftPatch}, rightPatch: {rightPatch}, topPatch: {topPatch}, bottomPatch: {bottomPatch}, canApplyPatch: {canApplyPatch}");
#endif

                //以left和right的比例重新分配水平方向上安全区宽度与UI宽度的差值
                float sum = left + right;
                bool willDivideZero = sum <= 0.1f;
                left = willDivideZero ? 0 : widthDiff * (left / sum);
                right = willDivideZero ? 0 : -widthDiff * (right / sum);

#if DETAIL_LOG
                Debug.Log($"以left和right的比例重新分配水平方向上安全区宽度与UI宽度的差值 left: {left}, right: {right}");
#endif

                //正常来说，获取的设备安全区是要随着设备旋转而改变，因为这样不需要根据方向改变计算公式
                //目前Android是这样的：LanscapeLeft时，左边有刘海，那么当旋转为LanscapeRight时，应该是右边有刘海
                //iOS待测试
                outline.offsetMin = new Vector2(left + leftPatch, bottom + bottomPatch);
                outline.offsetMax = new Vector2(right - rightPatch, -top);
#if DETAIL_LOG
                Debug.Log("outline.offsetMin:" + outline.offsetMin);
                Debug.Log("outline.offsetMax:" + outline.offsetMax);
#endif
            }
            else
            {
#if DETAIL_LOG
                Debug.Log("screenAspect < DESIGN_ASPECT");
#endif
                //当“安全区宽高比”比“设计宽高比”小时，但大于“最小宽高比”时，UI适配方案为宽度固定为SCREEN_DESIGN_WIDTH，高度根据实际比例计算
                float screenUIWidth = DesignWidth;
                float screenUIHeight = DesignWidth / screenAspect;
                float uiScale = (float)DesignWidth / screenWidth;

                //把安全区也转换为UI像素
                safeArea = new Rect(safeArea.x * uiScale, safeArea.y * uiScale, safeArea.width * uiScale, safeArea.height * uiScale);
#if DETAIL_LOG
                Debug.Log("safeArea换算为UI像素 : " + safeArea);
#endif
                float heightDiff = screenUIHeight - safeArea.height;
#if DETAIL_LOG
                Debug.Log("heightDiff : " + heightDiff);
#endif

                bool canApplyPatch = true;

                if (screenAspect <= MinAspect)
	            {
	                float maxUIHeight = DesignWidth / MinAspect;
#if DETAIL_LOG
	                Debug.Log("maxUIHeight : " + maxUIHeight);
#endif

                    //比较“安全区高”和“最大高”
                    if (safeArea.height >= maxUIHeight)
	                {
	                    //此时，应该使用在“安全区”里居中的“最大宽高比”区域作为新的“安全区”
	                    float safeAreaYOffset = (safeArea.height - maxUIHeight) / 2;
	                    safeArea.y += safeAreaYOffset;
	                    safeArea.height -= safeAreaYOffset * 2;
#if DETAIL_LOG
	                    Debug.Log("safeArea.height >= maxUIHeight，使用在“安全区”里居中的“最大宽高比”区域作为新的“安全区”: " + safeArea);
#endif
	                    heightDiff = screenHeight - safeArea.height;
#if DETAIL_LOG
                        Debug.Log("heightDiff更新 : " + heightDiff);
#endif

                        //不可以再应用补偿了
                        canApplyPatch = false;
                    }
	                else
	                {
	                    //此时就按安全区适配，如果UI重叠了，就说明无法适配该设备
	                }
                }
                //计算safeArea相对于完整屏幕的各方向偏移（UI像素）
                //注意：安卓返回的safeArea的坐标原点是左下角，因此safeArea.y表示屏幕底部向上偏移的距离
                float top = screenUIHeight - (safeArea.height + safeArea.y);
                float bottom = safeArea.y;
                float left = safeArea.x;
                float right = screenUIWidth - (safeArea.width + safeArea.x);
#if DETAIL_LOG
                Debug.Log($"top : {top}, bottom: {bottom}, left: {left}, right: {right}");
#endif

                //由于某些界面的按钮距离边界有一定的距离，在适配了安全区后，可能在视觉上偏移过远不好看，因此需要人工补偿一些距离回来
                float leftPatch = (canApplyPatch && left > 0.1f) ? customPatch.x : 0;
                float rightPatch = (canApplyPatch && right > 0.1f) ? customPatch.y : 0;
                float topPatch = (canApplyPatch && top > 0.1f) ? customPatch.z : 0;
                float bottomPatch = (canApplyPatch && bottom > 0.1f) ? customPatch.w : 0;
#if DETAIL_LOG
                Debug.Log($"leftPatch : {leftPatch}, rightPatch: {rightPatch}, topPatch: {topPatch}, bottomPatch: {bottomPatch}, canApplyPatch: {canApplyPatch}");
#endif

                //以top和bottom的比例重新分配在竖直方向上安全区的高度与UI高度的差值 
                float sum = top + bottom;
                bool willDivideZero = sum <= 0.1f;
                top = willDivideZero ? 0 : -heightDiff * (top / sum);
                bottom = willDivideZero ? 0 : heightDiff * (bottom / sum);

#if DETAIL_LOG
                Debug.Log($"以top和bottom的比例重新分配在竖直方向上安全区的高度与UI高度的差值 top: {top}, bottom: {bottom}");
#endif

                outline.offsetMin = new Vector2(left + leftPatch, bottom + bottomPatch);
                outline.offsetMax = new Vector2(-right - rightPatch, top + topPatch);
#if DETAIL_LOG
                Debug.Log("outline.offsetMin:" + outline.offsetMin);
                Debug.Log("outline.offsetMax:" + outline.offsetMax);
#endif
            }
        }
    }
}