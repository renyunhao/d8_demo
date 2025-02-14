using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CustomGridLayout : UIBehaviour
{
    public enum Axis
    {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom,
    }
    public enum InsideAlign
    {
        LeftOrBottom,
        Center,
        RightOrTop,
    }

    public static Dictionary<Axis, Dictionary<Axis, Dictionary<InsideAlign, Vector4>>> directions = new()
    {
        [Axis.LeftToRight] = new()
        {
            [Axis.BottomToTop] = new()
            {
                [InsideAlign.LeftOrBottom] = new(1, 0, 1, 1),
                [InsideAlign.Center] = new(1, 0, 1, 0),
                [InsideAlign.RightOrTop] = new(1, 0, 1, -1),
            },
            [Axis.TopToBottom] = new()
            {
                [InsideAlign.LeftOrBottom] = new(1, 0, -1, 1),
                [InsideAlign.Center] = new(1, 0, -1, 0),
                [InsideAlign.RightOrTop] = new(1, 0, -1, -1),
            },
        },
        [Axis.RightToLeft] = new()
        {
            [Axis.BottomToTop] = new()
            {
                [InsideAlign.LeftOrBottom] = new(-1, 0, 1, 1),
                [InsideAlign.Center] = new(-1, 0, 1, 0),
                [InsideAlign.RightOrTop] = new(-1, 0, 1, -1),
            },
            [Axis.TopToBottom] = new()
            {
                [InsideAlign.LeftOrBottom] = new(-1, 0, -1, 1),
                [InsideAlign.Center] = new(-1, 0, -1, 0),
                [InsideAlign.RightOrTop] = new(-1, 0, -1, -1),
            },
        },
        [Axis.BottomToTop] = new()
        {
            [Axis.LeftToRight] = new()
            {
                [InsideAlign.LeftOrBottom] = new(0, 1, 1, 1),
                [InsideAlign.Center] = new(0, 1, 1, 0),
                [InsideAlign.RightOrTop] = new(0, 1, 1, -1),
            },
            [Axis.RightToLeft] = new()
            {
                [InsideAlign.LeftOrBottom] = new(0, 1, -1, 1),
                [InsideAlign.Center] = new(0, 1, -1, 0),
                [InsideAlign.RightOrTop] = new(0, 1, -1, -1),
            },
        },
        [Axis.TopToBottom] = new()
        {
            [Axis.LeftToRight] = new()
            {
                [InsideAlign.LeftOrBottom] = new(0, -1, 1, 1),
                [InsideAlign.Center] = new(0, -1, 1, 0),
                [InsideAlign.RightOrTop] = new(0, -1, 1, -1),
            },
            [Axis.RightToLeft] = new()
            {
                [InsideAlign.LeftOrBottom] = new(0, -1, -1, 1),
                [InsideAlign.Center] = new(0, -1, -1, 0),
                [InsideAlign.RightOrTop] = new(0, -1, -1, -1),
            },
        },
    };
    public enum SingleLineDirAndAlign
    {
        LeftToRightAndTop,
        LeftToRightAndCenter,
        LeftToRightAndBottom,
        RightToLeftAndTop,
        RightToLeftAndCenter,
        RightToLeftAndBottom,
        TopToBottomAndLeft,
        TopToBottomAndCenter,
        TopToBottomAndRight,
        BottomToTopAndLeft,
        BottomToTopAndCenter,
        BottomToTopAndRight,
    }

    public static Dictionary<SingleLineDirAndAlign, Vector4> LineDirAndAlignDic = new Dictionary<SingleLineDirAndAlign, Vector4>()
    {
        { SingleLineDirAndAlign.LeftToRightAndTop, new Vector4(1, 0, 0, -1) },
        { SingleLineDirAndAlign.LeftToRightAndCenter, new Vector4(1, 0, 0, 0) },
        { SingleLineDirAndAlign.LeftToRightAndBottom, new Vector4(1, 0, 0, 1) },
        { SingleLineDirAndAlign.RightToLeftAndTop, new Vector4(-1, 0, 0, -1) },
        { SingleLineDirAndAlign.RightToLeftAndCenter, new Vector4(-1, 0, 0, 0) },
        { SingleLineDirAndAlign.RightToLeftAndBottom, new Vector4(-1, 0, 0, 1) },
        { SingleLineDirAndAlign.TopToBottomAndLeft, new Vector4(0, -1, 1, 0) },
        { SingleLineDirAndAlign.TopToBottomAndCenter, new Vector4(0, -1, 0, 0) },
        { SingleLineDirAndAlign.TopToBottomAndRight, new Vector4(0, -1, -1, 0) },
        { SingleLineDirAndAlign.BottomToTopAndLeft, new Vector4(0, 1, 1, 0) },
        { SingleLineDirAndAlign.BottomToTopAndCenter, new Vector4(0, 1, 0, 0) },
        { SingleLineDirAndAlign.BottomToTopAndRight, new Vector4(0, 1, -1, 0) },
    };
    private class SingleLine: UnityEngine.Object
    {
        private List<RectTransform> _rectTransforms;
        private List<Vector3> _anchorPositions;
        private Vector2 _pivot;
        private Vector4 _direction;
        private Vector2 _size;
        private float _spaceInside;

        public SingleLine(LinkedList<RectTransform> allRectTransforms, Vector4 direction, float spaceInside, Vector2 maxSize)
        {
            _rectTransforms = new List<RectTransform>();
            _anchorPositions = new List<Vector3>();
            _direction = direction;
            _spaceInside = spaceInside;
            bool isHorizontal = direction.x != 0;
            float finalSpace = spaceInside * (isHorizontal ? direction.x : direction.y);
            //-1~1映射到1~0
            var childPivot = isHorizontal
                ? new Vector2(-(direction.x - 1), -(direction.w - 1)) * 0.5f
                : new Vector2(-(direction.z - 1), -(direction.y - 1)) * 0.5f;
            _pivot = childPivot;
            int index = 0;
            while (allRectTransforms.Count > 0)
            {
                var r = allRectTransforms.First.Value;
                if(!r.gameObject.activeInHierarchy)
                {
                    allRectTransforms.RemoveFirst();
                    index++;
                    continue;
                }
                var scale = r.localScale;
                var sizeDelta = new Vector2(r.sizeDelta.x * scale.x, r.sizeDelta.y * scale.y);
                if (isHorizontal)
                {
                    if (index == 0)
                        _anchorPositions.Add(Vector3.zero);
                    else
                    {
                        _size.x += finalSpace;
                        _anchorPositions.Add(new Vector3(_size.x, 0));
                    }
                    var offset = sizeDelta.x * direction.x;
                    _size.x += offset;
                    if(Mathf.Abs(_size.x) > maxSize.x)
                    {
                        _size.x -= offset;
                        break;
                    }
                    if(_size.y < sizeDelta.y)
                        _size.y = sizeDelta.y;
                }
                else
                {
                    if (index == 0)
                        _anchorPositions.Add(Vector3.zero);
                    else
                    {
                        _size.y += finalSpace;
                        _anchorPositions.Add(new Vector3(0, _size.y));
                    }
                    var offset = sizeDelta.y * direction.y;
                    _size.y += offset;
                    if (Mathf.Abs(_size.y) > maxSize.y)
                    {
                        _size.y -= offset;
                        break;
                    }
                    if(_size.x < sizeDelta.x)
                        _size.x = sizeDelta.x;
                }
                r.pivot = childPivot;
                r.anchoredPosition = _anchorPositions.Last();
                _rectTransforms.Add(r);
                allRectTransforms.RemoveFirst();
                index++;
            }
            _size = new Vector2(Mathf.Abs(_size.x), Mathf.Abs(_size.y));
        }

        public void MovePosition(Vector3 position, Vector2 pivot)
        {
            //-0.5~0.5映射到0~1
            pivot += Vector2.one * 0.5f;
            var offsetPivot = _pivot - pivot;
            var offset = position + new Vector3(offsetPivot.x * _size.x, offsetPivot.y * _size.y);
            int rectCount = _rectTransforms.Count;
            for (int i = 0; i < rectCount; i++)
            {
                _anchorPositions[i] += offset;
                _rectTransforms[i].anchoredPosition = _anchorPositions[i];
            }
        }
    }
    
    public enum AllAlign
    {
        LeftTop,
        CenterTop,
        RightTop,
        LeftCenter,
        Center,
        RightCenter,
        LeftBottom,
        CenterBottom,
        RightBottom,
    }

    public static Dictionary<AllAlign, Vector2> AllAlignDict = new Dictionary<AllAlign, Vector2>()
    {
        { AllAlign.LeftTop, new Vector2(-0.5f, 0.5f) },
        { AllAlign.CenterTop, new Vector2(0, 0.5f) },
        { AllAlign.RightTop, new Vector2(0.5f, 0.5f) },
        { AllAlign.LeftCenter, new Vector2(-0.5f, 0) },
        { AllAlign.Center, new Vector2(0, 0) },
        { AllAlign.RightCenter, new Vector2(0.5f, 0) },
        { AllAlign.LeftBottom, new Vector2(-0.5f, -0.5f) },
        { AllAlign.CenterBottom, new Vector2(0, -0.5f) },
        { AllAlign.RightBottom, new Vector2(0.5f, -0.5f) },
    };
    private RectTransform m_RectTransform;
    private RectTransform rectTransform
    {
        get{
            if (m_RectTransform == null) 
                m_RectTransform = GetComponent<RectTransform>();
            return m_RectTransform; 
        }
    }

    private AllAlign _allAlignCorner = AllAlign.Center;

    private Vector2 AllAlignPivot => AllAlignDict[_allAlignCorner];

    public SingleLineDirAndAlign alignment = SingleLineDirAndAlign.LeftToRightAndTop;
    private Vector4 LineDirAndAlign => LineDirAndAlignDic[alignment];
    public float spaceInLine = 100;
    public float spaceInSide = 10;
    private readonly LinkedList<SingleLine> _lines = new LinkedList<SingleLine>();

    protected override void OnRectTransformDimensionsChange()
    {
        ResetLayout();
    }

    private void ResetLayout()
    {
        _lines.Clear();
        var rects = GetChildrenRectTransforms();
        var size = rectTransform.sizeDelta;
        var targetCorner = new Vector3(size.x * AllAlignPivot.x, size.y * AllAlignPivot.y);
        int lineIndex = 0;
        while (rects.Count > 0)
        {
            int startCount = rects.Count;
            var line = new SingleLine(rects, LineDirAndAlign, spaceInSide, rectTransform.sizeDelta);
            targetCorner += new Vector3(LineDirAndAlign.z * spaceInLine * lineIndex,
                LineDirAndAlign.w * spaceInLine * lineIndex);
            line.MovePosition(targetCorner, AllAlignPivot);
            if(startCount == rects.Count)
                break;
            _lines.AddLast(line);
        }
    }
    
    private LinkedList<RectTransform> GetChildrenRectTransforms()
    {
        LinkedList<RectTransform> children = new LinkedList<RectTransform>();
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var child = rectTransform.GetChild(i) as RectTransform;
            if(child.gameObject.activeInHierarchy)
                children.AddLast(child);
        }

        return children;
    }

    public void SetLayout(SingleLineDirAndAlign dirAndAlign)
    {
        if (alignment == dirAndAlign) return;
        alignment = dirAndAlign;
        ResetLayout();
    }

    public void SetAnchor(AllAlign pos)
    {
        if(_allAlignCorner == pos)
            return;
        _allAlignCorner = pos;
        ResetLayout();
    }
}