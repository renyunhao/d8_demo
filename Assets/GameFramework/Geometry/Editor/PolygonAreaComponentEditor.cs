using UnityEditor;
using UnityEngine;

namespace GameFramework
{
    [CustomEditor(typeof(PolygonAreaComponent))]
    public class PolygonAreaComponentEditor : Editor
    {
        private PolygonAreaComponent areaComponent;
        private Transform handleTransform;
        private Quaternion handleRotation;

        private void OnEnable()
        {
            areaComponent = target as PolygonAreaComponent;
            handleTransform = areaComponent.transform;
        }

        private void OnDisable()
        {
            areaComponent.ClearTestRandomPoints();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("随机选点测试"))
            {
                areaComponent.TestRandomPoint(100);
                EditorWindow.GetWindow<SceneView>().Repaint();
            }
            if (GUILayout.Button("清除测试物体"))
            {
                areaComponent.ClearTestRandomPoints();
                EditorWindow.GetWindow<SceneView>().Repaint();
            }
            GUILayout.EndHorizontal();
        }

        private void OnSceneGUI()
        {
            handleRotation = Tools.pivotRotation == PivotRotation.Local ?
                handleTransform.rotation : Quaternion.identity;

            Vector3 p0 = ShowPoint(0);
            Vector3 p0Cache = p0;
            Handles.color = Color.blue;
            for (int i = 1; i < areaComponent.area.vertices.Count; i++)
            {
                Vector3 p1 = ShowPoint(i);
                Handles.DrawLine(p0, p1);
                p0 = p1;
                if (i + 1 < areaComponent.area.vertices.Count)
                {
                    Vector3 p2 = ShowPoint(i + 1);
                    Handles.DrawLine(p1, p2);
                    p0 = p2;
                }
            }
            //闭合
            Handles.DrawLine(p0, p0Cache);
        }

        private Vector3 ShowPoint(int index)
        {
            Vector3 point = handleTransform.TransformPoint(areaComponent.area.vertices[index]);
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(areaComponent, "Move Point");
                EditorUtility.SetDirty(areaComponent);
                areaComponent.area.MarkDirty();
                areaComponent.area.vertices[index] = handleTransform.InverseTransformPoint(point);
            }
            return point;
        }
    }
}
