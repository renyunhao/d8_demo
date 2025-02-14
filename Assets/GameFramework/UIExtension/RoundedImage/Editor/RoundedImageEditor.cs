using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
using UnityEngine.UI;

namespace GameFramework
{
    [CustomEditor(typeof(RoundedImage), true)]
    public class RoundedImageEditor : ImageEditor
    {
        private SerializedProperty m_Sprite;
        private SerializedProperty m_Type;
        private SerializedProperty m_UseSpriteMesh;
        private SerializedProperty m_PreserveAspect;
        private AnimBool m_ShowType;
        private SerializedProperty m_Radius;
        private SerializedProperty m_TriangleNum;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Radius = serializedObject.FindProperty("Radius");
            m_TriangleNum = serializedObject.FindProperty("TriangleNum");

            m_Sprite = serializedObject.FindProperty("m_Sprite");
            m_Type = serializedObject.FindProperty("m_Type");
            m_UseSpriteMesh = serializedObject.FindProperty("m_UseSpriteMesh");
            m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");
            m_ShowType = new AnimBool(m_Sprite.objectReferenceValue != null);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SpriteGUI();
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();

            m_ShowType.target = m_Sprite.objectReferenceValue != null;
            if (EditorGUILayout.BeginFadeGroup(m_ShowType.faded))
                TypeGUI();
            EditorGUILayout.EndFadeGroup();

            SetShowNativeSize(false);
            if (EditorGUILayout.BeginFadeGroup(m_ShowNativeSize.faded))
            {
                EditorGUI.indentLevel++;

                if ((Image.Type)m_Type.enumValueIndex == Image.Type.Simple)
                    EditorGUILayout.PropertyField(m_UseSpriteMesh);

                EditorGUILayout.PropertyField(m_PreserveAspect);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            NativeSizeButtonGUI();

            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_TriangleNum);

            serializedObject.ApplyModifiedProperties();
        }

        private void SetShowNativeSize(bool instant)
        {
            Image.Type type = (Image.Type)m_Type.enumValueIndex;
            bool showNativeSize = (type == Image.Type.Simple || type == Image.Type.Filled) && m_Sprite.objectReferenceValue != null;
            base.SetShowNativeSize(showNativeSize, instant);
        }
    }
}