using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER
using Screen = UnityEngine.Device.Screen; // To support Device Simulator on Unity 2021.1+
#endif

namespace GameFramework
{
    public class FloatingLayout : BindableMonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action Event_OnClick;
        public event Action Event_OnDoubleClick;

        public bool snapToScreenEdge = true;
        private float doubleClickThresholdTime = 0.3f;
        private float oneClickThresholdTime = 0.4f;

        private RectTransform layoutTransform;
        private Vector2 normalizedPosition;
        private IEnumerator moveToPosCoroutine = null;
        private Vector2 halfSize;

        private RectTransform containerRT;
        private Vector2 dragOffset;
        private int clickTime = 0;

        private void Awake()
        {
            layoutTransform = (RectTransform)transform;
            halfSize = layoutTransform.sizeDelta * 0.5f;
            Vector2 pos = layoutTransform.anchoredPosition;
            if (pos.x != 0f || pos.y != 0f)
            {
                normalizedPosition = pos.normalized;
            }
            else
            {
                normalizedPosition = new Vector2(0.5f, 0f);
            }
            containerRT = GetComponentInParent<FloatingLayoutContainer>().transform as RectTransform;
        }

        private float lastClickTime = 0;
        private float pointerDownTime = 0;
        private bool isPointerDown = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            pointerDownTime = Time.realtimeSinceStartup;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isPointerDown)
            {
                var oneDelta = Time.realtimeSinceStartup - pointerDownTime;
                if (oneDelta < oneClickThresholdTime)
                {
                    clickTime++;
                    var doubleDelta = pointerDownTime - lastClickTime;
                    if (doubleDelta < doubleClickThresholdTime && clickTime == 2)
                    {
                        Event_OnDoubleClick?.Invoke();
                    }
                    else
                    {
                        lastClickTime = Time.realtimeSinceStartup;
                        Event_OnClick?.Invoke();
                    }
                    if (clickTime == 2)
                    {
                        clickTime = 0;
                    }
                }
            }
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            isPointerDown = false;
            if (moveToPosCoroutine != null)
            {
                StopCoroutine(moveToPosCoroutine);
                moveToPosCoroutine = null;
            }
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRT, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                dragOffset = (Vector2)layoutTransform.localPosition - localPoint;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRT, eventData.position, eventData.pressEventCamera, out localPoint))
                layoutTransform.localPosition = localPoint + dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (snapToScreenEdge)
            {
                SnapToScreenEdge(false);
            }
        }

        public void SnapToScreenEdge(bool immediately)
        {
            Vector2 containerSize = containerRT.rect.size;
            Rect layoutRect = layoutTransform.rect;
            Vector2 pos = (Vector2)layoutTransform.localPosition;
            layoutRect.center += pos;

            float containerWidth = containerSize.x;
            float containerHeight = containerSize.y;
            float edgeLeft = -containerWidth / 2;
            float edgeRight = containerWidth / 2;
            float edgeTop = containerHeight / 2;
            float edgeBottom = -containerHeight / 2;

            float distToLeft   = layoutRect.xMin - edgeLeft;
            float distToRight  = edgeRight - layoutRect.xMax;
            float distToBottom = layoutRect.yMin - edgeBottom;
            float distToTop    = edgeTop - layoutRect.yMax;

            float distToLeftAbs = Mathf.Abs(distToLeft);
            float distToRightAbs = Mathf.Abs(distToRight);
            float distToBottomAbs = Mathf.Abs(distToBottom);
            float distToTopAbs = Mathf.Abs(distToTop);

            float horDistance = Mathf.Min(distToLeftAbs, distToRightAbs);
            float vertDistance = Mathf.Min(distToBottomAbs, distToTopAbs);

            if (horDistance < vertDistance)
            {
                if (distToLeftAbs < distToRightAbs)
                {
                    pos.x -= distToLeft;
                }
                else
                {
                    pos.x += distToRight;
                }

                if (distToBottom < 0 || distToTop < 0)
                {
                    if (distToBottomAbs < distToTopAbs)
                    {
                        pos.y -= distToBottom;
                    }
                    else
                    {
                        pos.y += distToTop;
                    }
                }
            }
            else
            {
                if (distToBottomAbs < distToTopAbs)
                {
                    pos.y -= distToBottom;
                }
                else
                {
                    pos.y += distToTop;
                }

                if (distToLeft < 0 || distToRight < 0)
                {
                    if (distToLeftAbs < distToRightAbs)
                    {
                        pos.x -= distToLeft;
                    }
                    else
                    {
                        pos.x += distToRight;
                    }
                }
            }

            // If another smooth movement animation is in progress, cancel it
            if (moveToPosCoroutine != null)
            {
                StopCoroutine(moveToPosCoroutine);
                moveToPosCoroutine = null;
            }

            if (immediately)
                layoutTransform.localPosition = pos;
            else
            {
                // Smoothly translate the popup to the specified position
                moveToPosCoroutine = MoveToPosAnimation(pos);
                StartCoroutine(moveToPosCoroutine);
            }
        }

        // A simple smooth movement animation
        private IEnumerator MoveToPosAnimation(Vector2 targetPos)
        {
            float modifier = 0f;
            Vector2 initialPos = layoutTransform.localPosition;

            while (modifier < 1f)
            {
                modifier += 4f * Time.unscaledDeltaTime;
                layoutTransform.localPosition = Vector2.Lerp(initialPos, targetPos, modifier);

                yield return null;
            }
        }
    }
}
