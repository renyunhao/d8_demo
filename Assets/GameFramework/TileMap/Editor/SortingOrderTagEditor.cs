using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace GameFramework
{
    [CustomEditor(typeof(SortingOrderTag)), CanEditMultipleObjects]
    public class SortingOrderTagEditor : Editor
    {
        SerializedProperty spSortingLayer;
        SerializedProperty spOrderInLayer;
        SerializedProperty spOrderDelta;

        public void OnEnable()
        {
            spSortingLayer = serializedObject.FindProperty("sortingLayer");
            spOrderInLayer = serializedObject.FindProperty("sortingOrder");
            spOrderDelta = serializedObject.FindProperty("zOrder");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            var sortingLayerNames = SortingLayer.layers.Select(l => l.name).ToArray();

            if (sortingLayerNames != null)
            {
                // Look up the layer name using the current layer id
                string oldName = SortingLayer.IDToName(spSortingLayer.intValue);

                // Use the name to look up our array index into the names list
                int oldLayerIndex = Array.IndexOf(sortingLayerNames, oldName);

                // Show the popup for the names
                int newLayerIndex = EditorGUILayout.Popup(spSortingLayer.displayName, oldLayerIndex, sortingLayerNames);

                // If the index changes, look up the id for the new index to store as the new id
                if (newLayerIndex != oldLayerIndex)
                {
                    spSortingLayer.intValue = SortingLayer.NameToID(sortingLayerNames[newLayerIndex]);
                }
            }
            else
            {
                int newValue = EditorGUILayout.IntField(spSortingLayer.displayName, spSortingLayer.intValue);
                if (newValue != spSortingLayer.intValue)
                {
                    spSortingLayer.intValue = newValue;
                }
                EditorGUI.EndProperty();
            }

            EditorGUILayout.PropertyField(spOrderInLayer);
            EditorGUILayout.PropertyField(spOrderDelta);

            serializedObject.ApplyModifiedProperties();
        }
    }
}