using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameFramework
{
    [CustomEditor(typeof(TileMapSorting))]
    [CanEditMultipleObjects]
    public class TileMapSortingEditor : Editor
    {
        SerializedProperty spSortingMethod;
        SerializedProperty spSortingLayer;
        SerializedProperty spOrderInLayer;
        SerializedProperty spOrderDelta;

        public virtual void OnEnable()
        {
            spSortingLayer = serializedObject.FindProperty("sortingLayer");
            spOrderInLayer = serializedObject.FindProperty("orderInLayer");
            spOrderDelta = serializedObject.FindProperty("orderDelta");
        }

        public virtual void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            var sortingLayerNames = SortingLayer.layers.Select(l => l.name).ToArray();

            if (sortingLayerNames != null)
            {
                string oldName = SortingLayer.IDToName(spSortingLayer.intValue);
                int oldLayerIndex = Array.IndexOf(sortingLayerNames, oldName);
                int newLayerIndex = EditorGUILayout.Popup(spSortingLayer.displayName, oldLayerIndex, sortingLayerNames);
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