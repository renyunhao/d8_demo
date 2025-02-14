using DG.DOTweenEditor.UI;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace GameFramework
{
    [CustomEditor(typeof(WaypointsComponent))]
    public class WaypointsComponentEditor : Editor
    {
        private WaypointsComponent component;
        private Transform handleTransform;
        private Quaternion handleRotation;

        private bool isInsert = false;

        private void OnEnable()
        {
            component = target as WaypointsComponent;
            handleTransform = component.transform;
        }

        private void OnSceneGUI()
        {
            component.showIndexes = EditorGUILayout.Toggle("Show Indexes", component.showIndexes);

            handleRotation = Tools.pivotRotation == PivotRotation.Local ?
                handleTransform.rotation : Quaternion.identity;

            Vector3 p0 = ShowPoint(0);
            Handles.color = Color.blue;
            for (int i = 1; i < component.points.Length; i++)
            {
                Vector3 p1 = ShowPoint(i);
                Handles.DrawLine(p0, p1);
                p0 = p1;
                if (i + 1 < component.points.Length)
                {
                    Vector3 p2 = ShowPoint(i + 1);
                    Handles.DrawLine(p1, p2);
                    p0 = p2;
                }
            }

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.KeyDown:
                    {
                        if (e.keyCode == KeyCode.Insert)
                        {
                            isInsert = true;
                            e.Use();
                        }
                        break;
                    }
                case EventType.KeyUp:
                    {
                        if (e.keyCode == KeyCode.Insert)
                        {
                            isInsert = false;
                            e.Use();
                        }
                        break;
                    }
                case EventType.MouseDown:
                    {
                        if (isInsert)
                        {
                            Vector3 worldPosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
                            worldPosition.z = 0;
                            Vector3 localPoint = component.transform.InverseTransformPoint(worldPosition);
                            component.AddPoint(localPoint);
                            EditorUtility.SetDirty(component);
                            e.Use();
                        }
                    }
                    break;
            }
        }

        private Vector3 ShowPoint(int index)
        {
            Vector3 point = handleTransform.TransformPoint(component.points[index]);
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(component, "Move Point");
                EditorUtility.SetDirty(component);
                component.points[index] = handleTransform.InverseTransformPoint(point);
            }
            float handleSize = HandleUtility.GetHandleSize(point);

            if (component.showIndexes)
            {
                var textPos = point + new Vector3(handleSize * 0.1f, handleSize * 0.4f, 0);
                string text = index.ToString();
                Handles.Label(textPos, text);
            }
            return point;
        }
    }
}
