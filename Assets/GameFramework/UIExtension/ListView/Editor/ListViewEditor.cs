using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    [CustomEditor(typeof(ListView))]
    public class ListViewEditor : Editor
    {
        private SerializedProperty spSpacing;
        private SerializedProperty spPadding;
        private SerializedProperty spDataCount;
        private SerializedProperty spItemPrefab;
        private SerializedProperty spRow;
        private SerializedProperty spColumn;
        private SerializedProperty spUseGalleryMode;
        private SerializedProperty spGalleryItemScaleCurve;
        private SerializedProperty spCenterOnChild;
        private SerializedProperty spAutoInitialize;
        private SerializedProperty spResortSibling;
        private SerializedProperty spAscendingOrder;
        private SerializedProperty spCalculatePositionWithItemPivot;
        private SerializedProperty spCenterLayout;

        private void OnEnable()
        {
            spSpacing = serializedObject.FindProperty("spacing");
            spPadding = serializedObject.FindProperty("padding");
            spDataCount = serializedObject.FindProperty("dataCount");
            spItemPrefab = serializedObject.FindProperty("itemPrefab");
            spRow = serializedObject.FindProperty("row");
            spColumn = serializedObject.FindProperty("column");
            spUseGalleryMode = serializedObject.FindProperty("useGalleryMode");
            spGalleryItemScaleCurve = serializedObject.FindProperty("galleryItemScaleCurve");
            spCenterOnChild = serializedObject.FindProperty("centerOnChild");
            spAutoInitialize = serializedObject.FindProperty("autoInitialize");
            spResortSibling = serializedObject.FindProperty("resortSibling");
            spAscendingOrder = serializedObject.FindProperty("ascendingOrder");
            spCalculatePositionWithItemPivot = serializedObject.FindProperty("calculatePositionWithItemPivot");
            spCenterLayout = serializedObject.FindProperty("centerLayout");
        }

        public override void OnInspectorGUI()
        {
            ListView listView = (ListView)target;
            listView.ScrollRect = listView.GetComponent<ScrollRect>();
            listView.Content = listView.ScrollRect.content;
            Slider.Direction direction = (Slider.Direction)EditorGUILayout.EnumPopup("Direction", listView.direction);
            if (direction != listView.direction)
            {
                listView.direction = direction;
                float width = listView.ScrollRect.GetComponent<RectTransform>().sizeDelta.x;
                float height = listView.ScrollRect.GetComponent<RectTransform>().sizeDelta.y;
                listView.InitializeContentAnchorAndPivot();
                listView.Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                listView.Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            if (listView.direction == Slider.Direction.LeftToRight || listView.direction == Slider.Direction.RightToLeft)
            {
                EditorGUILayout.PropertyField(spRow);
            }
            else if (listView.direction == Slider.Direction.TopToBottom || listView.direction == Slider.Direction.BottomToTop)
            {
                EditorGUILayout.PropertyField(spColumn);
            }
            EditorGUILayout.PropertyField(spSpacing);
            EditorGUILayout.PropertyField(spPadding, true);
            EditorGUILayout.PropertyField(spDataCount);
            EditorGUILayout.PropertyField(spItemPrefab);
            EditorGUILayout.PropertyField(spUseGalleryMode);
            EditorGUILayout.PropertyField(spGalleryItemScaleCurve);
            EditorGUILayout.PropertyField(spAutoInitialize);
            EditorGUILayout.PropertyField(spCenterOnChild);
            EditorGUILayout.PropertyField(spResortSibling);
            EditorGUILayout.PropertyField(spAscendingOrder);
            EditorGUILayout.PropertyField(spCalculatePositionWithItemPivot);
            EditorGUILayout.PropertyField(spCenterLayout);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview"))
            {
                listView.Preview();
            }
            if (GUILayout.Button("Clear"))
            {
                listView.Clear();
            }
            GUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }
    }
}