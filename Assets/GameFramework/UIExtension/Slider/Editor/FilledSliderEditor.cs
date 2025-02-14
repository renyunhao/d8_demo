using UnityEditor;

namespace GameFramework
{
    [CustomEditor(typeof(FilledSlider))]
    public class FilledSliderEditor : Editor
    {
        private SerializedProperty spImage;
        private SerializedProperty spHandler;
        private SerializedProperty spProgress;
        private SerializedProperty spTransition;
        private SerializedProperty spSpeed;
        private SerializedProperty spFillMethod;
        private SerializedProperty spOriginHorizontal;
        private SerializedProperty spOriginVertical;
        private SerializedProperty spOrigin90;
        private SerializedProperty spOrigin360;

        private void OnEnable()
        {
            spImage = serializedObject.FindProperty("image");
            spHandler = serializedObject.FindProperty("handler");
            spProgress = serializedObject.FindProperty("progress");
            spTransition = serializedObject.FindProperty("transition");
            spSpeed = serializedObject.FindProperty("speed");
            spFillMethod = serializedObject.FindProperty("fillMethod");
            spOriginHorizontal = serializedObject.FindProperty("originHorizontal");
            spOriginVertical = serializedObject.FindProperty("originVertical");
            spOrigin90 = serializedObject.FindProperty("origin90");
            spOrigin360 = serializedObject.FindProperty("origin360");
        }

        public override void OnInspectorGUI()
        {
            FilledSlider progressBar = (FilledSlider)target;
            EditorGUILayout.PropertyField(spImage);
            EditorGUILayout.PropertyField(spHandler);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(spProgress);
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                progressBar.SetProgressInstant(spProgress.floatValue);
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.PropertyField(spTransition);
            EditorGUILayout.PropertyField(spSpeed);
            EditorGUILayout.PropertyField(spFillMethod);
            if (progressBar.fillMethod == UnityEngine.UI.Image.FillMethod.Horizontal)
            {
                EditorGUILayout.PropertyField(spOriginHorizontal);
            }
            else if (progressBar.fillMethod == UnityEngine.UI.Image.FillMethod.Vertical)
            {
                EditorGUILayout.PropertyField(spOriginVertical);
            }
            else if (progressBar.fillMethod == UnityEngine.UI.Image.FillMethod.Radial90)
            {
                EditorGUILayout.PropertyField(spOrigin90);
            }
            else if (progressBar.fillMethod == UnityEngine.UI.Image.FillMethod.Radial180)
            {
                EditorGUILayout.PropertyField(spOrigin360);
            }
            else if (progressBar.fillMethod == UnityEngine.UI.Image.FillMethod.Radial360)
            {
                EditorGUILayout.PropertyField(spOrigin360);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
