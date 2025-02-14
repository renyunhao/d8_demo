using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

[CustomEditor(typeof(CustomGridLayout))]
public class CustomGridLayoutEditor : Editor
{
    private enum StartAxisPopup
    {
        从左到右,
        从右到左,
        从上到下,
        从下到上,
    }
    private enum HorizontalAxisPopup
    {
        从左到右,
        从右到左,
    }
    private enum VerticalAxisPopup
    {
        从上到下,
        从下到上,
    }
    private CustomGridLayout _gridLayout => (CustomGridLayout)target;
    private static Dictionary<CustomGridLayout.SingleLineDirAndAlign, Vector4> AlignScaleDic = CustomGridLayout.LineDirAndAlignDic;

    private Dictionary<StartAxisPopup, Vector2> startDirections = new()
    {
        { StartAxisPopup.从左到右, AlignScaleDic[CustomGridLayout.SingleLineDirAndAlign.LeftToRightAndCenter] },
        { StartAxisPopup.从右到左, AlignScaleDic[CustomGridLayout.SingleLineDirAndAlign.RightToLeftAndCenter] },
        { StartAxisPopup.从上到下, AlignScaleDic[CustomGridLayout.SingleLineDirAndAlign.TopToBottomAndCenter] },
        { StartAxisPopup.从下到上, AlignScaleDic[CustomGridLayout.SingleLineDirAndAlign.BottomToTopAndCenter] },
    };

    private StartAxisPopup currentStartAxis = StartAxisPopup.从左到右;
    private HorizontalAxisPopup currentHorizontalAxis = HorizontalAxisPopup.从左到右;
    private VerticalAxisPopup currentVerticalAxis = VerticalAxisPopup.从上到下;
    private int currentInsideAlignment = 0;
    
    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("开始方向:", GUILayout.Width(60));
        currentStartAxis = (StartAxisPopup)EditorGUILayout.EnumPopup(currentStartAxis, GUILayout.Width(70));
        var isHorizontal = currentStartAxis is StartAxisPopup.从左到右 or StartAxisPopup.从右到左;
        GUILayout.Label("换行方向:", GUILayout.Width(60));
        if (isHorizontal)
            currentVerticalAxis = (VerticalAxisPopup)EditorGUILayout.EnumPopup(currentVerticalAxis, GUILayout.Width(70));
        else
            currentHorizontalAxis = (HorizontalAxisPopup)EditorGUILayout.EnumPopup(currentHorizontalAxis, GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();
        GUILayout.Label("———————————————————————");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.Label("整体对齐位置", GUILayout.Width(75));
        GUILayout.Space(30 + 70);
        GUILayout.Label("行内对齐位置", GUILayout.Width(75));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("↖", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.LeftTop);
        if(GUILayout.Button("↑", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.CenterTop);
        if(GUILayout.Button("↗", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.RightTop);
        
        if(isHorizontal)
        {
            GUILayout.Space(43 + 70);
            if (GUILayout.Button("↑", GUILayout.Width(30), GUILayout.Height(30)))
                currentInsideAlignment = 1;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("←", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.LeftCenter);
        if(GUILayout.Button("〇", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.Center);
        if(GUILayout.Button("→", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.RightCenter);
        
        if(!isHorizontal)
        {
            GUILayout.Space(10 + 70);
            if (GUILayout.Button("←", GUILayout.Width(30), GUILayout.Height(30)))
                currentInsideAlignment = 1;
        }
        else
            GUILayout.Space(10 + 70 + 33);

        if (GUILayout.Button("〇", GUILayout.Width(30), GUILayout.Height(30)))
            currentInsideAlignment = 0;
        if(!isHorizontal)
        {
            if (GUILayout.Button("→", GUILayout.Width(30), GUILayout.Height(30)))
                currentInsideAlignment = -1;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("↙", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.LeftBottom);
        if(GUILayout.Button("↓", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.CenterBottom);
        if(GUILayout.Button("↘", GUILayout.Width(30), GUILayout.Height(30))) 
            _gridLayout.SetAnchor(CustomGridLayout.AllAlign.RightBottom);
        
        if(isHorizontal)
        {
            GUILayout.Space(43 + 70);
            if (GUILayout.Button("↓", GUILayout.Width(30), GUILayout.Height(30)))
                currentInsideAlignment = -1;
        }
        
        if(currentStartAxis == StartAxisPopup.从左到右 && currentInsideAlignment == 1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.LeftToRightAndTop);
        else if(currentStartAxis == StartAxisPopup.从左到右 && currentInsideAlignment == 0)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.LeftToRightAndCenter);
        else if(currentStartAxis == StartAxisPopup.从左到右 && currentInsideAlignment == -1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.LeftToRightAndBottom);
        else if(currentStartAxis == StartAxisPopup.从右到左 && currentInsideAlignment == 1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.RightToLeftAndTop);
        else if(currentStartAxis == StartAxisPopup.从右到左 && currentInsideAlignment == 0)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.RightToLeftAndCenter);
        else if(currentStartAxis == StartAxisPopup.从右到左 && currentInsideAlignment == -1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.RightToLeftAndBottom);
        else if(currentStartAxis == StartAxisPopup.从上到下 && currentInsideAlignment == 1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.TopToBottomAndLeft);
        else if(currentStartAxis == StartAxisPopup.从上到下 && currentInsideAlignment == 0)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.TopToBottomAndCenter);
        else if(currentStartAxis == StartAxisPopup.从上到下 && currentInsideAlignment == -1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.TopToBottomAndRight);
        else if(currentStartAxis == StartAxisPopup.从下到上 && currentInsideAlignment == 1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.BottomToTopAndLeft);
        else if(currentStartAxis == StartAxisPopup.从下到上 && currentInsideAlignment == 0)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.BottomToTopAndCenter);
        else if(currentStartAxis == StartAxisPopup.从下到上 && currentInsideAlignment == -1)
            _gridLayout.SetLayout(CustomGridLayout.SingleLineDirAndAlign.BottomToTopAndRight);
        EditorGUILayout.EndHorizontal();
        serializedObject.ApplyModifiedProperties();
    }
}
