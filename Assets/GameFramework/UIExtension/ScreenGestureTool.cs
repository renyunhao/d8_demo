using UnityEngine;
using System;
using DG.Tweening;

namespace GameFramework
{
    public class ScreenGestureTool : MonoBehaviour
    {
        private Camera mapCamera;
        private Camera uiCamera;

        public Action<Vector3> Event_CameraMove;
        public Action Event_DragNoJuge;
        public Action Event_DragStart;
        public Action<bool> Event_Dragging;
        public Func<bool> Event_DragEnd;
        public Action Event_OnPinch;

        public static ScreenGestureTool Instance
        {
            get;
            private set;
        }

        /// <summary>初始orthographicSize</summary>
        public const float ENTER_GAME_ORTHOGRAPHIC_SIZE = 6.5f;

        /// <summary>摄像机放大orthographicSize的可超越幅度值 </summary>
        public const float MAX_SIZE_EXCEED = 0f;

        /// <summary>摄像机缩小orthographicSize的可超越幅度值</summary>
        public const float MIN_SIZE_EXCEED = 0f;

        /// <summary>摄像机移动可超越视野范围的幅度值</summary>
        public const float MOVE_CAMERA_EXCEED = 0f;

        /// <summary>拖拽结束后是否还应该继续移动摄像机（逐渐减速到0）</summary>
        private bool shouldMoveAfterDragEnd = false;

        /// <summary>拖拽结束时的惯性速度</summary>
        private Vector2 dragEndDeltaMove = new Vector2();

        /// <summary>是否正在播放摄像机视野逐渐缩小的效果</summary>
        private bool isPlayingCameraScaleIn = false;

        /// <summary>摄像机是否在移动</summary>
        private bool isCameraMoving = false;

        /// <summary>摄像机是否在缩放</summary>
        public bool isCameraScaling = false;

        /// <summary>是否禁用</summary>
        private bool isDisabled = false;

        /// <summary>摄像机能达到的最大orthographicSize 需计算或者直接设置值, 比如进入截图模式会比正常游戏的限制size更大</summary>
        public float maxOrthographicSizeLimit = 5;

        /// <summary>摄像机能达到最小orthographicSize的数值/summary>///
        public float minOrthographicSizeLimit = 5f;

        /// <summary>摄像机放大缩小改变的速度值</summary>
        public float pinchSpeed = 0.5f;

        /// <summary>校正摄像机视野值到最大或者最小值时的回弹时间</summary>
        public float correctOrthographicSizeTime = 0.25f;

        /// <summary>校正摄像机到达移动边界时的回弹时间</summary>
        public float correctPosTime = 0.3f;

        /// <summary>拖动结束后惯性移动插值强度</summary>
        public float dragEndMoveLerpStrength = 5;

        public float rotationLerpStrength = 7.0f;

        public float dragAcceleration = 150.0f;

        public float minPitchAngle = -60.0f;

        public float maxPitchAngle = 60.0f;

        private Vector2 angularVelocity = Vector2.zero;

        /// <summary> 摄像机最大orthographicSize，可超越</summary>
        public float maxOrthographicSize = 9;

        /// <summary> 摄像机最小orthographicSize</summary>
        public float minOrthographicSize = 7f;

        public float maxFOVSize = 60;
        public float minFOVSize = 5f;

        /// <summary>
        /// 摄像机的视野有效范围
        /// </summary>
        public Rect viewRange;

        public Rect ViewRange
        {
            get
            {
                return viewRange;
            }
        }

        public static Vector2 MapCameraPos
        {
            get
            {
                return Instance.MapCamera.transform.position;
            }
        }

        /// <summary>游戏摄像机(除UI外)</summary>
        public Camera MapCamera
        {
            get
            {
                if (mapCamera == null)
                {
                    mapCamera = GameObject.Find(nameof(MapCamera)).GetComponent<Camera>();
                    //mapCamera.transform.position = new Vector3(0, 0, -10);
                    //mapCamera.orthographicSize = ENTER_GAME_ORTHOGRAPHIC_SIZE;
                }
                return mapCamera;
            }
        }

        public Tween mapCameraMoveTween;

        /// <summary>UI摄像机</summary>
        public Camera UICamera
        {
            get
            {
                if (uiCamera == null)
                {
                    uiCamera = GameObject.Find(nameof(UICamera)).GetComponent<Camera>();
                }
                return uiCamera;
            }
        }

        /// <summary> 屏幕像素到摄像机正交大小的转换系数 </summary>
        private float PixelToOrthographicSizeRatio
        {
            get
            {
                if (MapCamera.pixelWidth <= MapCamera.pixelHeight)
                {
                    return MapCamera.orthographicSize * 2 / MapCamera.pixelWidth;
                }
                else
                {
                    return MapCamera.orthographicSize * 2 / MapCamera.pixelHeight;
                }
            }
        }

        /// <summary>摄像机正交宽度的一半 </summary>
        private float CameraOrthoHalfWidth
        {
            get
            {
                return MapCamera.orthographicSize * Screen.width / Screen.height;
            }
        }

        /// <summary>摄像机正交高度的一半</summary>
        private float CameraOrthoHalfHeight
        {
            get
            {
                return MapCamera.orthographicSize;
            }
        }

        // Use this for initialization
        void Awake()
        {
            Instance = this;
        }

        void LateUpdate()
        {
            if (Input.GetMouseButtonDown(0))
            {
                shouldMoveAfterDragEnd = false;
                dragEndDeltaMove = Vector2.zero;
            }

            #region 移动
            if (shouldMoveAfterDragEnd && MapCamera.orthographic)
            {
                dragEndDeltaMove = Vector2.Lerp(dragEndDeltaMove, Vector2.zero, SpringLerp(dragEndMoveLerpStrength, Time.deltaTime));

                Vector3 cameraNewPos = MapCamera.transform.position;
                cameraNewPos.x -= dragEndDeltaMove.x * PixelToOrthographicSizeRatio;
                cameraNewPos.y -= dragEndDeltaMove.y * PixelToOrthographicSizeRatio;
                MoveCameraInstant(cameraNewPos);

                if (dragEndDeltaMove.magnitude < 1)
                {
                    shouldMoveAfterDragEnd = false;
                    dragEndDeltaMove = Vector2.zero;
                }
                Event_DragNoJuge?.Invoke();
            }

            if (shouldMoveAfterDragEnd && !MapCamera.orthographic)
            {
                dragEndDeltaMove = Vector2.Lerp(dragEndDeltaMove, Vector2.zero, SpringLerp(rotationLerpStrength, Time.deltaTime));
                RotateCamera(dragEndDeltaMove);
                if (dragEndDeltaMove.magnitude < 1)
                {
                    shouldMoveAfterDragEnd = false;
                    dragEndDeltaMove = Vector2.zero;
                }
            }
            #endregion
        }

        public void Init(float minOrthographicSize, float maxOrthographicSize, Rect viewRange)
        {
            this.minOrthographicSize = minOrthographicSize;
            this.maxOrthographicSize = maxOrthographicSize;
            if (this.minOrthographicSize > this.maxOrthographicSize)
            {
                this.minOrthographicSize = this.maxOrthographicSize;
            }
            maxOrthographicSizeLimit = this.maxOrthographicSize + MAX_SIZE_EXCEED;
            minOrthographicSizeLimit = minOrthographicSize - MIN_SIZE_EXCEED;
            this.viewRange = viewRange;
        }

        public Vector3 ReturnLeftUpPos()
        {
            return new Vector3(viewRange.xMin, viewRange.yMax, 0);
        }

        public Vector3 ReturnRightDownPos()
        {
            return new Vector3(viewRange.xMax, viewRange.yMin, 0);
        }

        #region 手势操作计算
        private float NormalizePitch(float angle)
        {
            if (angle > 180.0f)
                angle -= 360.0f;

            return angle;
        }

        public void Disable()
        {
            isDisabled = true;
        }

        public void Enable()
        {
            isDisabled = false;
        }

        /// <summary>是否禁用手势插件 </summary>
        private bool IsGestureDisabled()
        {
            if (isPlayingCameraScaleIn || isCameraMoving || isDisabled)
            {
                return true;
            }
            if (maxOrthographicSize < minOrthographicSize)
            {
                GameFramework.Debug.LogWarningFormat("Orthographic值设置错误");
                return true;
            }
            if (maxOrthographicSizeLimit < minOrthographicSizeLimit)
            {
                GameFramework.Debug.LogWarningFormat("Orthographic值设置错误");
                return true;
            }
            return false;
        }

        private float SpringLerp(float strength, float deltaTime)
        {
            if (deltaTime > 1f)
            {
                deltaTime = 1f;
            }
            int ms = Mathf.RoundToInt(deltaTime * 1000f);
            deltaTime = 0.001f * strength;
            float cumulative = 0f;
            for (int i = 0; i < ms; ++i)
            {
                cumulative = Mathf.Lerp(cumulative, 1f, deltaTime);
            }
            return cumulative;
        }

        private Vector3 ClampPositionInFlexiableRange(Vector3 originPosition)
        {
            return ClampPosition(originPosition, MOVE_CAMERA_EXCEED);
        }

        private Vector3 ClampPositionInFixedRange(Vector3 originPosition)
        {
            return ClampPosition(originPosition, 0);
        }

        private Vector3 ClampPosition(Vector3 originPosition, float exceedValue)
        {
            //计算摄像机的位置有效范围
            //注意。则于实际设备的屏幕宽高比可能比地图更小，所以出现当高度达到极限时，宽度已经超出了要极限，因此需要兼容一下，即摄像机的视野用地图高度限制，宽度可以超过上限，此时不能左右移动了
            float cameraPosYMin = viewRange.yMin + CameraOrthoHalfHeight - exceedValue;
            float cameraPosYMax = viewRange.yMax - CameraOrthoHalfHeight + exceedValue;
            float cameraPosXMin = viewRange.xMin + CameraOrthoHalfWidth - exceedValue;
            float cameraPosXMax = viewRange.xMax - CameraOrthoHalfWidth + exceedValue;

            float yMin;
            float yMax;
            float xMin;
            float xMax;

            //最大值与最小值在极限情况下可能互换，此时要将位置固定为中间值
            if (cameraPosXMin > cameraPosXMax)
            {
                xMin = xMax = (cameraPosXMax + cameraPosXMin) / 2;
            }
            else
            {
                xMin = cameraPosXMin;
                xMax = cameraPosXMax;
            }

            //最大值与最小值在极限情况下可能互换，此时要将位置固定为中间值
            if (cameraPosYMin > cameraPosYMax)
            {
                yMin = yMax = (cameraPosYMax + cameraPosYMin) / 2;
            }
            else
            {
                yMin = cameraPosYMin;
                yMax = cameraPosYMax;
            }


            //将位置约束在有效范围内
            originPosition.x = Mathf.Clamp(originPosition.x, xMin, xMax);
            originPosition.y = Mathf.Clamp(originPosition.y, yMin, yMax);
            originPosition.z = -10;
            return originPosition;
        }

        /// <summary>
        /// 约束在弹性范围内
        /// </summary>
        /// <param name="orthographicSize"></param>
        /// <returns></returns>
        public float ClampOrthographicSizeInFlexibleRange(float orthographicSize)
        {
            return Mathf.Clamp(orthographicSize, minOrthographicSize - MIN_SIZE_EXCEED, maxOrthographicSize + MAX_SIZE_EXCEED);
        }

        /// <summary>
        /// 约束在绝对范围内
        /// </summary>
        /// <param name="orthographicSize"></param>
        /// <returns></returns>
        public float ClampOrthographicSizeInFixedRange(float orthographicSize)
        {
            return Mathf.Clamp(orthographicSize, minOrthographicSizeLimit, maxOrthographicSizeLimit);
        }

        public Vector3 GetViewTopLeftPosition()
        {
            return new Vector3(viewRange.xMin, viewRange.yMax, 0);
        }

        public Vector3 GetViewBottonRightPosition()
        {
            return new Vector3(viewRange.xMax, viewRange.yMin, 0);
        }
        #endregion

        #region 摄像机操控
        public void SetViewRange(float x, float y, float w, float h)
        {
            viewRange = new Rect(x, y, w, h);
        }

        public void LookAtTarget(Vector3 targetPos, bool instant = false)
        {
            if (instant)
            {
                MapCamera.transform.rotation = Quaternion.LookRotation(targetPos - MapCamera.transform.position);
            }
            else
            {
                MapCamera.transform.DORotateQuaternion(Quaternion.LookRotation(targetPos - MapCamera.transform.position), 3);
            }
        }

        public void RotateCamera(Vector2 deltaMove)
        {
            Vector3 localAngles = MapCamera.transform.localEulerAngles;
            Vector2 idealAngularVelocity = Vector2.zero;

            idealAngularVelocity = dragAcceleration * deltaMove.Centimeters();

            angularVelocity = Vector2.Lerp(angularVelocity, idealAngularVelocity, Time.deltaTime * dragAcceleration);
            Vector2 angularMove = Time.deltaTime * angularVelocity;

            //x旋转变化量为x角度加Y方向偏移量，绕X轴选择上下移动所以是Y方向；
            localAngles.x = Mathf.Clamp(NormalizePitch(localAngles.x + angularMove.y), minPitchAngle, maxPitchAngle);

            localAngles.y = Mathf.Clamp(NormalizePitch(localAngles.y - angularMove.x), minPitchAngle, maxPitchAngle);

            MapCamera.transform.localEulerAngles = localAngles;
        }

        public void ShakeCamera(float shakeTime = 1, Vector2 strength = default(Vector2), int vibrato = 5, float random = 60)
        {
            MapCamera.transform.DOShakePosition(shakeTime, strength, vibrato, random);
        }

        /// <summary>摄像机的平滑移动</summary>
        public void MoveCamera(Vector3 targetPosition, float moveTime = 0.2f, Action endCallback = null,
            Ease easeType = Ease.OutQuad, bool withViewRangeLimit = true, bool unscaleTime = false)
        {
            targetPosition.z = -10;

            if (withViewRangeLimit)
            {
                //将摄像机移动目标位置约束在有效范围内
                targetPosition = ClampPositionInFixedRange(targetPosition);
            }

            if (moveTime <= 0)
            {
                MoveCameraInstant(targetPosition);
                return;
            }
            //摄像机位移的时候，要根据实际的距离来动态调整需要的时间，避免摄像机其实已经在目标位置上了，移动还需要那么长的时间
            float distance = Vector3.Distance(MapCamera.transform.position, targetPosition);
            if (distance < MapCamera.orthographicSize)
            {
                moveTime *= (distance / MapCamera.orthographicSize);
                if (moveTime < 0.3f)
                {
                    moveTime = 0.3f;
                }
            }
            isCameraMoving = true;
            if (Instance.mapCameraMoveTween != null)
            {
                if (Instance.mapCameraMoveTween.IsActive() && Instance.mapCameraMoveTween.IsPlaying())
                {
                    Instance.mapCameraMoveTween.Kill();
                    Instance.mapCameraMoveTween = null;
                }
            }
            mapCameraMoveTween = MapCamera.transform.DOMove(targetPosition, moveTime).OnComplete(() =>
            {
                isCameraMoving = false;
                if (endCallback != null)
                {
                    endCallback();
                }
            }).SetEase(easeType).SetUpdate(unscaleTime);
        }

        /// <summary>立即移动摄像机，考虑边界限制</summary>
        public bool MoveCameraInstant(Vector3 targetPosition, bool flexiableRange = false, bool withViewRangeLimit = true)
        {
            bool isInFlexiableRange = false;
            Vector3 vector = MapCamera.transform.position;
            if (withViewRangeLimit)
            {
                if (flexiableRange)
                {
                    var noFlexiableRangePosition = ClampPositionInFixedRange(targetPosition);
                    MapCamera.transform.position = ClampPositionInFlexiableRange(targetPosition);
                    isInFlexiableRange = noFlexiableRangePosition != MapCamera.transform.position;

                }
                else
                {
                    MapCamera.transform.position = ClampPositionInFixedRange(targetPosition);
                }
            }
            else
                mapCamera.transform.position = targetPosition;

            Event_CameraMove?.Invoke(MapCamera.transform.position - vector);
            return isInFlexiableRange;
        }

        /// <summary>摄像机的平滑缩放，考虑边界限制与大小限制</summary>
        public void ScaleCamera(float newOrthographicSize, float duration = 0.2f, Action endCallback = null)
        {
            if (isCameraScaling)
            {
                return;
            }
            float validSize = ClampOrthographicSizeInFixedRange(newOrthographicSize);

            isCameraScaling = true;
            DOTween.To(() => MapCamera.orthographicSize, (x) => MapCamera.orthographicSize = x, validSize, duration)
                .OnUpdate(() =>
                {
                    Event_OnPinch?.Invoke();
                })
                .OnComplete(() =>
                {
                    isCameraScaling = false;
                    if (endCallback != null)
                    {
                        endCallback();
                    }
                });
        }

        public void ScaleCameraInstant(float newOrthographicSize)
        {
            MapCamera.orthographicSize = ClampOrthographicSizeInFixedRange(newOrthographicSize);
        }

        /// <summary>移动摄像机，使得目标点位于摄像机视野范围内</summary>
        public void MoveCameraToIncludePosition(GameObject target, float time = 0.2f, Action endCallBack = null)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }
            Bounds maxBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i].bounds.size.x == 0 || renderers[i].bounds.size.y == 0)
                {
                    //如果没有渲染任何东西，直接跳过
                    continue;
                }
                Vector2 min = maxBounds.min;
                Vector2 max = maxBounds.max;
                min.x = Mathf.Min(min.x, renderers[i].bounds.min.x);
                min.y = Mathf.Min(min.y, renderers[i].bounds.min.y);
                max.x = Mathf.Max(max.x, renderers[i].bounds.max.x);
                max.y = Mathf.Max(max.y, renderers[i].bounds.max.y);
                maxBounds.min = min;
                maxBounds.max = max;
            }

            float maxXGapBetweenCameraAndTarget = CameraOrthoHalfWidth - maxBounds.size.x / 2;
            float maxYGapBetweenCameraAndTarget = CameraOrthoHalfHeight - maxBounds.size.y / 2;
            Vector3 distance = maxBounds.center - MapCamera.transform.position;
            distance.z = 0;

            float moveXDelta = Mathf.Abs(distance.x) - maxXGapBetweenCameraAndTarget;
            float moveYDelta = Mathf.Abs(distance.y) - maxYGapBetweenCameraAndTarget;
            int xDirection = distance.x > 0 ? 1 : -1;
            int yDirection = distance.y > 0 ? 1 : -1;

            Vector3 moveDelta = Vector3.zero;
            if (moveXDelta > 0)
            {
                moveDelta.x = moveXDelta * xDirection;
            }
            if (moveYDelta > 0)
            {
                moveDelta.y = moveYDelta * yDirection;
            }
            Vector3 cameraNewPos = MapCamera.transform.position + moveDelta;
            MoveCamera(cameraNewPos, time, endCallBack);
        }

        /// <summary>
        /// 移动摄像机到指定坐标
        /// </summary>
        /// <param name="postion"></param>
        public void MoveCameraToIncludePosition(Vector3 postion)
        {
            float distanceXOfMouseAndCameraCenter = postion.x - MapCamera.transform.position.x;
            float distanceYOfMouseAndCameraCenter = postion.y - MapCamera.transform.position.y;

            float gapXToCameraViewBoundary = CameraOrthoHalfWidth - Mathf.Abs(distanceXOfMouseAndCameraCenter);
            float gapYToCameraViewBoundary = CameraOrthoHalfHeight - Mathf.Abs(distanceYOfMouseAndCameraCenter);
            Vector3 mouseWorldPosition = MapCamera.ScreenToWorldPoint(Input.mousePosition);
            if (gapXToCameraViewBoundary <= 0.5f || gapYToCameraViewBoundary <= 0.5f)
            {
                if (isCameraMoving == false)
                {
                    Vector3 newPosition = MapCamera.transform.position;
                    newPosition.x += distanceXOfMouseAndCameraCenter * 0.5f;
                    newPosition.y += distanceYOfMouseAndCameraCenter * 0.5f;

                    MoveCamera(newPosition, 0.2f, null, Ease.Linear);
                }
            }
        }
        #endregion

        #region 手势操作
        /// <summary>缩放手势 </summary>
        public void OnPinch(PinchGesture gesture)
        {
            if (IsGestureDisabled())
            {
                return;
            }
            MapCamera.transform.DOKill();
            if (gesture.Phase == ContinuousGesturePhase.Started)
            {
                shouldMoveAfterDragEnd = false;
            }
            else if (gesture.Phase == ContinuousGesturePhase.Updated)
            {
                if (MapCamera.orthographic)
                {
                    float scale = 1;
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        scale = 10;
                    }
                    MapCamera.orthographicSize -= gesture.Delta * PixelToOrthographicSizeRatio * pinchSpeed * scale;
                    MapCamera.orthographicSize = ClampOrthographicSizeInFlexibleRange(MapCamera.orthographicSize);
                    //缩放操作也可能产生位移
                    Vector3 cameraNewPos = MapCamera.transform.position;
                    cameraNewPos.x -= gesture.DeltaMove.x * PixelToOrthographicSizeRatio;
                    cameraNewPos.y -= gesture.DeltaMove.y * PixelToOrthographicSizeRatio;

                    MoveCameraInstant(cameraNewPos, true);
                    Event_OnPinch?.Invoke();
                }
                else
                {
                    MapCamera.fieldOfView -= gesture.Delta * PixelToOrthographicSizeRatio * pinchSpeed;
                    MapCamera.fieldOfView = Mathf.Clamp(MapCamera.fieldOfView, minFOVSize, maxFOVSize);
                }
            }
            else if (gesture.Phase == ContinuousGesturePhase.Ended)
            {
                PinchEnd();
                EnsureCameraInViewRangeElastic();
            }
        }

        /// <summary>缩放手势（在编辑器中响应鼠标滚轮操作）</summary>
        public void OnPinch(float gesture)
        {
            if (IsGestureDisabled())
            {
                return;
            }
            if (MapCamera.orthographic)
            {
                MapCamera.transform.DOKill();
                shouldMoveAfterDragEnd = false;
                float scale = 1;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    scale = 10;
                }
                MapCamera.orthographicSize -= gesture * PixelToOrthographicSizeRatio * pinchSpeed * scale;
                //操作的时候约束在弹性范围内
                MapCamera.orthographicSize = ClampOrthographicSizeInFlexibleRange(MapCamera.orthographicSize);
                //缩放操作也可能产生位移
                Vector3 cameraNewPos = MapCamera.transform.position;
                MoveCameraInstant(cameraNewPos, true);
                EnsureCameraInViewRangeElastic();
                Event_OnPinch?.Invoke();
            }
        }

        /// <summary>缩放操作结束</summary>
        public void PinchEnd()
        {
            //缩放操作结束时如果当前相机显示范围超出绝对范围，用动画变回去
            if (MapCamera.orthographicSize > maxOrthographicSize)
            {
                DOTween.To(() => MapCamera.orthographicSize, x => MapCamera.orthographicSize = x, maxOrthographicSize, correctOrthographicSizeTime).OnUpdate(() =>
                {
                    Event_OnPinch?.Invoke();
                });
            }
            else if (MapCamera.orthographicSize < minOrthographicSize)
            {
                DOTween.To(() => MapCamera.orthographicSize, x => MapCamera.orthographicSize = x, minOrthographicSize, correctOrthographicSizeTime).OnUpdate(() =>
                {
                    Event_OnPinch?.Invoke();
                });
            }
        }

        /// <summary>拖拽</summary>
        public void OnDrag(DragGesture gesture)
        {
            if (IsGestureDisabled())
            {
                return;
            }
            MapCamera.transform.DOKill();
            if (gesture.Phase == ContinuousGesturePhase.Started)
            {
                shouldMoveAfterDragEnd = false;
                Event_DragStart?.Invoke();
            }
            else if (gesture.Phase == ContinuousGesturePhase.Updated)
            {
                bool isFlexiableRange = false;
                if (MapCamera.orthographic)
                {
                    Vector3 cameraNewPos = MapCamera.transform.position;
                    cameraNewPos.x -= gesture.DeltaMove.x * PixelToOrthographicSizeRatio;
                    cameraNewPos.y -= gesture.DeltaMove.y * PixelToOrthographicSizeRatio;
                    isFlexiableRange = MoveCameraInstant(cameraNewPos, true);
                }
                else
                {
                    RotateCamera(gesture.DeltaMove);
                }
                Event_Dragging?.Invoke(isFlexiableRange);
            }
            else if (gesture.Phase == ContinuousGesturePhase.Ended)
            {
                bool cameraControlByOutside = false;
                if (Event_DragEnd != null)
                {
                    cameraControlByOutside = Event_DragEnd.Invoke();
                }
                if (cameraControlByOutside == false)
                {
                    dragEndDeltaMove = gesture.DeltaMove;
                    if (dragEndDeltaMove.sqrMagnitude > 1)
                    {
                        shouldMoveAfterDragEnd = true;
                    }
                    EnsureCameraInViewRangeElastic();
                }
            }
        }

        /// <summary>拖拽</summary>
        public void OnDrag(Vector2 deltaMove)
        {
            if (IsGestureDisabled())
            {
                return;
            }
            MapCamera.transform.DOKill();
            if (MapCamera.orthographic)
            {
                Vector3 cameraNewPos = MapCamera.transform.position;
                cameraNewPos.x -= deltaMove.x * PixelToOrthographicSizeRatio;
                cameraNewPos.y -= deltaMove.y * PixelToOrthographicSizeRatio;
                MoveCameraInstant(cameraNewPos, true);
            }
        }

        /// <summary>
        /// 检查摄像机是否在视野范围内，如果不在，以弹性动画移回去
        /// </summary>
        public void EnsureCameraInViewRangeElastic()
        {
            float cameraPosYMin = viewRange.yMin + CameraOrthoHalfHeight;
            float cameraPosYMax = viewRange.yMax - CameraOrthoHalfHeight;
            float cameraPosXMin = viewRange.xMin + CameraOrthoHalfWidth;
            float cameraPosXMax = viewRange.xMax - CameraOrthoHalfWidth;

            if (MapCamera.transform.position.y < cameraPosYMin)
            {
                shouldMoveAfterDragEnd = false;
                MapCamera.transform.DOMoveY(cameraPosYMin, correctPosTime);
            }
            else if (MapCamera.transform.position.y > cameraPosYMax)
            {
                shouldMoveAfterDragEnd = false;
                MapCamera.transform.DOMoveY(cameraPosYMax, correctPosTime);
            }

            if (MapCamera.transform.position.x < cameraPosXMin)
            {
                shouldMoveAfterDragEnd = false;
                MapCamera.transform.DOMoveX(cameraPosXMin, correctPosTime);
            }
            else if (MapCamera.transform.position.x > cameraPosXMax)
            {
                shouldMoveAfterDragEnd = false;
                MapCamera.transform.DOMoveX(cameraPosXMax, correctPosTime);
            }
        }
        #endregion
    }
}