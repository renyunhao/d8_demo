using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class JoysticksPanel : MonoBehaviour
{
    public bool isMove;
    private Vector3 oldPos;

    public float Horizontal { set; get; }
    public float Vertical { set; get; }

    public float Angle { set; get; }

    public bool isEnter;

    public RectTransform handlerbarRect;
    public RectTransform barRect;
    public RectTransform rectTransform;

    //半径
    private float radius;

    //对外事件
    public Action<bool> BeginJoysticks;
    public Action<bool> OnTouchStart;

    public Action<float> UpdateHorizontal;
    public Action<float> UpdateVertical;
    public Action<float> UpdateAngle;
    public Action<Vector2> UpdateDir;

    public Action<Vector2> UpdateDrag;

    public Action<PointerEventData> OnPointEnter;

    public RectTransform mainCanvas;

    private RectTransform canvasRect;

    private float yScreenHRadio;
    private float xSreenWRadio;

    private void Start()
    {
        radius = GetComponent<RectTransform>().sizeDelta.x / 2.0f;
        oldPos = this.transform.GetComponent<RectTransform>().anchoredPosition;
        canvasRect = mainCanvas;

        xSreenWRadio = canvasRect.sizeDelta.x * 1.00f / Camera.main.pixelWidth * 1.00f;
        yScreenHRadio = canvasRect.sizeDelta.y * 1.00f / Camera.main.pixelHeight * 1.00f;

        if (isMove)
        {
            Event_OnUnityEvent.Add(transform.parent.gameObject).OnPointerDownEvent += TouchStart;
            Event_OnUnityEvent.Add(transform.parent.gameObject).OnPointerEventUpEvent += TouchEnd;
            Event_OnUnityEvent.Add(transform.parent.gameObject).OnBeginDragEvent += OnParentPanelPointEnter;
            Event_OnUnityEvent.Add(transform.parent.gameObject).OnDragEvent += OnDrag;
            Event_OnUnityEvent.Add(transform.parent.gameObject).OnEndDragEvent += OnEndDrag;
            Event_OnUnityEvent.Add(transform.parent.gameObject).OnPointerClickEvent += OnOnPointerClick;
        }
        else
        {
            Event_OnUnityEvent.Add(this.gameObject).OnPointerClickEvent += OnOnPointerClick;
            Event_OnUnityEvent.Add(this.gameObject).OnBeginDragEvent += OnBeginDragEvent;
            Event_OnUnityEvent.Add(this.gameObject).OnDragEvent += OnDrag;
            Event_OnUnityEvent.Add(this.gameObject).OnEndDragEvent += OnEndDrag;
        }
    }
    

    private void OnOnPointerClick(PointerEventData obj)
    {
        OnPointEnter?.Invoke(obj);
    }

    private void OnDestroy()
    {
        try
        {
            if (isMove)
            {
                Event_OnUnityEvent.Add(transform.parent.gameObject).OnBeginDragEvent -= OnParentPanelPointEnter;
                Event_OnUnityEvent.Add(transform.parent.gameObject).OnDragEvent -= OnDrag;
                Event_OnUnityEvent.Add(transform.parent.gameObject).OnEndDragEvent -= OnEndDrag;
            }
            else
            {
                Event_OnUnityEvent.Add(this.gameObject).OnBeginDragEvent -= OnBeginDragEvent;
                Event_OnUnityEvent.Add(this.gameObject).OnDragEvent -= OnDrag;
                Event_OnUnityEvent.Add(this.gameObject).OnEndDragEvent -= OnEndDrag;
            }
        }
        catch (Exception)
        {
            
        }
        
    }
    private void OnBeginDragEvent(PointerEventData arg0)
    {
        // EventDispatchSync.EventDispatch.Dispatch<float>(EventConst.UpdateJoySticksBegin, 1);
        BeginJoysticks?.Invoke(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 from = ((Vector2)handlerbarRect.localPosition - (Vector2)rectTransform.localPosition);
        Vector2 to = Vector2.up;

        //移动handlerbar 并计算其与圆心的角度s
        float angle = Vector2.Angle(from.normalized, to);
        angle = Vector3.Cross(from.normalized, to).z > 0 ? angle : (-(angle - 180)) + 180;
        //Vector3 dir = to - from;
        //Debug.LogError(from);
        TransfomToUguiPos(handlerbarRect);

        //计算距离移动bar与圆心的距离 加入限定
        float dis = Vector3.Distance(handlerbarRect.anchoredPosition, rectTransform.anchoredPosition);
        dis = dis >= radius ? radius : dis;
        //更新展示的bar位置
        barRect.anchoredPosition = rectTransform.anchoredPosition + new Vector2(dis * Mathf.Sin(Mathf.Deg2Rad * angle), dis * Mathf.Cos(Mathf.Deg2Rad * angle));

        #region 计算 水平 垂直的映射值 角度的映射
        Angle = angle;
        Horizontal = (barRect.transform.localPosition.x - rectTransform.localPosition.x) / radius;
        Vertical = (barRect.transform.localPosition.y - rectTransform.localPosition.y) / radius;

        #endregion 计算 水平 垂直的映射值 角度的映射

        //委托自定义入口
        UpdateAngle?.Invoke(Angle);
        UpdateVertical?.Invoke(Vertical);
        UpdateHorizontal?.Invoke(Horizontal);
        UpdateDir?.Invoke(from);

        UpdateDrag?.Invoke(eventData.delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        handlerbarRect.transform.localPosition = Vector3.zero;
        barRect.transform.localPosition = Vector3.zero;
        Horizontal = 0;
        Vertical = 0;

        if (isMove)
        {
            rectTransform.anchoredPosition = oldPos;
            handlerbarRect.anchoredPosition = oldPos;
            barRect.anchoredPosition = oldPos;
        }
        isEnter = false;

        //委托自定义入口
        UpdateVertical?.Invoke(0);
        UpdateHorizontal?.Invoke(0);
        BeginJoysticks?.Invoke(false);
    }

    public void OnParentPanelPointEnter(PointerEventData eventData)
    {
        //Debug.Log(eventData.position);
        if (isMove)
        {
            TransfomToUguiPos(rectTransform);
        }

        //委托自定义入口
        BeginJoysticks?.Invoke(true);
        isEnter = true;
    }

    public void TouchStart(PointerEventData eventData) {
        TransfomToUguiPos(rectTransform);
        TransfomToUguiPos(handlerbarRect);
        TransfomToUguiPos(barRect);
        OnTouchStart?.Invoke(true);
    }

    public void TouchEnd(PointerEventData eventData)
    {
        OnTouchStart?.Invoke(false);
    }

    private void TransfomToUguiPos(RectTransform tagret)
    {
        tagret.anchoredPosition = new Vector2(Input.mousePosition.x * xSreenWRadio, Input.mousePosition.y * yScreenHRadio);
    }
}