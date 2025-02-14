using System;
using DG.Tweening;

namespace GameFramework
{
    public static class ScreenGestureToolExtension
    {
        public static void ScaleCamera(this ScreenGestureTool tool, bool isBreak, float newOrthographicSize,
            float duration = 0.2f, Action endCallback = null)
        {
            bool isCameraScaling = tool.isCameraScaling;
            Tweener tweener = null;
            if (isBreak)
            {
                tweener?.Kill();
                isCameraScaling = false;
            }
            if (isCameraScaling)
            {
                return;
            }
            float validSize = tool.ClampOrthographicSizeInFlexibleRange(newOrthographicSize);

            isCameraScaling = true;
            tweener = DOTween.To(() => tool.MapCamera.orthographicSize, (x) => tool.MapCamera.orthographicSize = x, validSize, duration)
                .OnUpdate(() =>
                {
                    tool.Event_OnPinch?.Invoke();
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
    }
}