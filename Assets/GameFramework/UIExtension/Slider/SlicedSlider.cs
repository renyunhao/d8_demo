using System.Collections;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SlicedSlider : UIBehaviour
{
    public enum Direction
    {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom,
    }

    [SerializeField]
    private Direction direction = Direction.LeftToRight;

    public Direction StartDirection
    {
        get => direction;
        set
        {
            if (direction != value)
            {
                direction = value;
                Refresh();
            }
        }
    }
    [SerializeField]
    private float maxSize;

    public float MaxSize
    {
        get => maxSize;
        set
        {
            if (!Mathf.Approximately(maxSize, value))
            {
                maxSize = value;
                Refresh();
            }
        }
    }
    [SerializeField]
    private float minSize;

    public float MinSize
    {
        get => minSize;
        set
        {
            if (!Mathf.Approximately(minSize, value))
            {
                minSize = value;
                Refresh();
            }
        }
    }
    [SerializeField]
    private float value;

    public float Value
    {
        get => value;
        set
        {
            if (!Mathf.Approximately(this.value, value))
            {
                this.value = Mathf.Clamp(value, 0, 1);
                Refresh();
                onValueChanged?.Invoke(value);
            }
        }
    }

    public UnityAction<float> onValueChanged;
    private RectTransform _rectTransform;

    private RectTransform RectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        Refresh();
    }

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();
        direction = Direction.LeftToRight;
        minSize = 0;
        maxSize = RectTransform.sizeDelta.x;
    }
#endif

    private void Refresh()
    {
        maxSize = Mathf.Abs(maxSize);
        minSize = Mathf.Abs(minSize);
        if (maxSize < minSize + 2)
            maxSize = minSize + 2;
        SetPivot();
        var sizeDelta = RectTransform.sizeDelta;
        if (direction is Direction.LeftToRight or Direction.RightToLeft)
            RectTransform.sizeDelta = new Vector2(minSize + (maxSize - minSize) * value, sizeDelta.y);
        else
            RectTransform.sizeDelta = new Vector2(sizeDelta.x, minSize + (maxSize - minSize) * value);
    }

    private void SetPivot()
    {
        switch (direction)
        {
            case Direction.LeftToRight:
                RectTransform.pivot = new Vector2(0, RectTransform.pivot.y);
                break;
            case Direction.RightToLeft:
                RectTransform.pivot = new Vector2(1, RectTransform.pivot.y);
                break;
            case Direction.BottomToTop:
                RectTransform.pivot = new Vector2(RectTransform.pivot.x, 0);
                break;
            case Direction.TopToBottom:
                RectTransform.pivot = new Vector2(RectTransform.pivot.x, 1);
                break;
        }
    }

    public void SetValueWithoutNotify(float value)
    {
        if (!Mathf.Approximately(this.value, value))
        {
            this.value = Mathf.Clamp(value, 0, 1);
            Refresh();
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        float offset = 0;
        if (direction is Direction.LeftToRight or Direction.RightToLeft)
            offset = RectTransform.sizeDelta.x - minSize;
        else
            offset = RectTransform.sizeDelta.y - minSize;

        offset = Mathf.Clamp(offset, 0, maxSize - minSize);

        value = offset / (maxSize - minSize);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        value = Mathf.Clamp(value, 0, 1);
        if (Application.isPlaying)
        {
            Refresh();
        }
        else
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(DelayRefresh());
        }
    }

    private IEnumerator DelayRefresh()
    {
        yield return null;
        Refresh();
    }
#endif
}
