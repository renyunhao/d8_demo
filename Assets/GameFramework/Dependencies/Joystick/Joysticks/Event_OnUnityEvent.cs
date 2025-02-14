using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Event_OnUnityEvent : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler, IPointerClickHandler, IDragHandler, IEndDragHandler
{
    public UnityAction<PointerEventData> OnPointerDownEvent;
    public UnityAction<PointerEventData> OnPointerEventUpEvent;
    public UnityAction<PointerEventData> OnPointerEnterEvent;
    public UnityAction<PointerEventData> OnPointerEventExitEvent;
    public UnityAction<PointerEventData> OnBeginDragEvent;
    public UnityAction<PointerEventData> OnPointerClickEvent;
    public UnityAction<PointerEventData> OnDragEvent;
    public UnityAction<PointerEventData> OnEndDragEvent;

    public static Event_OnUnityEvent Add(GameObject obj)
    {
        Event_OnUnityEvent event_OnUnityEvent = obj.GetComponent<Event_OnUnityEvent>();
        if (event_OnUnityEvent == null)
            event_OnUnityEvent = obj.AddComponent<Event_OnUnityEvent>();
        return event_OnUnityEvent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (OnBeginDragEvent != null)
        {
            OnBeginDragEvent(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (OnDragEvent != null)
        {
            OnDragEvent(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (OnEndDragEvent != null)
        {
            OnEndDragEvent(eventData);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnPointerClickEvent != null)
        {
            OnPointerClickEvent(eventData);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OnPointerEnterEvent != null)
        {
            OnPointerEnterEvent(eventData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (OnPointerEventExitEvent != null)
        {
            OnPointerEventExitEvent(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (OnPointerEventUpEvent != null)
        {
            OnPointerEventUpEvent(eventData);
        }
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        OnPointerDownEvent?.Invoke(eventData);
    }
}