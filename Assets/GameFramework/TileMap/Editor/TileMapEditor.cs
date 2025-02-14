using UnityEditor;
using UnityEngine;
using System.Linq;

namespace GameFramework
{
    [CustomEditor(typeof(TileMap)), CanEditMultipleObjects]
    partial class TileMapEditor : Editor
    {
        SerializedProperty spMapWidth;
        SerializedProperty spMapHeight;
        SerializedProperty spGridSize;
        SerializedProperty spXRotation;
        SerializedProperty spYRotation;
        SerializedProperty spIsoWidth;
        SerializedProperty spIsoHeight;
        SerializedProperty spMapLayout;
        SerializedProperty spOuterRadius;
        SerializedProperty spOrientation;
        SerializedProperty spShowCoord;
        SerializedProperty spOnlyShowGridWhenSelected;
        SerializedProperty sp2DMode;

        private TileMap[] tileMaps;

        [MenuItem("GameObject/2D Object/TileMap")]
        private static void CreateTileMapGameObject()
        {
            new GameObject("New TileMap", typeof(TileMap));
        }

        private void OnEnable()
        {
            tileMaps = targets.Cast<TileMap>().ToArray();

            spMapWidth = serializedObject.FindProperty("mapWidth");
            spMapHeight = serializedObject.FindProperty("mapHeight");
            spIsoWidth = serializedObject.FindProperty("isoWidth");
            spXRotation = serializedObject.FindProperty("xRotation");
            spYRotation = serializedObject.FindProperty("yRotation");
            spGridSize = serializedObject.FindProperty("gridSize");
            spIsoHeight = serializedObject.FindProperty("isoHeight");
            spMapLayout = serializedObject.FindProperty("mapLayout");
            spOuterRadius = serializedObject.FindProperty("outerRadius");
            spOrientation = serializedObject.FindProperty("orientation");
            spShowCoord = serializedObject.FindProperty("showCoord");
            spOnlyShowGridWhenSelected = serializedObject.FindProperty("onlyShowGridWhenSelected");
            sp2DMode = serializedObject.FindProperty("is2DMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            GUILayout.Label("Basic Settings", TileMapGUIStyles.leftBoldLabel);

            EditorGUILayout.PropertyField(spShowCoord);
            EditorGUILayout.PropertyField(spOnlyShowGridWhenSelected);
            bool is2DMode = sp2DMode.boolValue;
            is2DMode = EditorGUILayout.Toggle(sp2DMode.displayName, is2DMode);
            if (is2DMode != sp2DMode.boolValue)
            {
                foreach (var tileMap in tileMaps)
                {
                    tileMap.ChangeMode(is2DMode);
                }
            }

            int mapWidth = spMapWidth.intValue;
            mapWidth = EditorGUILayout.IntField(spMapWidth.displayName, mapWidth);
            int mapHeight = spMapHeight.intValue;
            mapHeight = EditorGUILayout.IntField(spMapHeight.displayName, mapHeight);

            bool isMapSizeChanged = (mapWidth != spMapWidth.intValue || mapHeight != spMapHeight.intValue);
            if (isMapSizeChanged)
            {
                foreach (var tileMap in tileMaps)
                {
                    tileMap.ResizeMap(mapWidth, mapHeight);
                }
            }

            TileMap.Layout layout = (TileMap.Layout)spMapLayout.enumValueIndex;
            layout = (TileMap.Layout)EditorGUILayout.EnumPopup(spMapLayout.displayName, layout);
            foreach (var tileMap in tileMaps)
            {
                if (layout != tileMap.MapLayout)
                {
                    tileMap.ChangeLayout(layout);
                }
            }

            if (layout == TileMap.Layout.CartesianCoordinate)
            {
                float gridSize = spGridSize.floatValue;
                gridSize = EditorGUILayout.FloatField(spGridSize.displayName, gridSize);
                foreach (var tileMap in tileMaps)
                {
                    if (gridSize != tileMap.GridSize)
                    {
                        tileMap.ResizeGrid(gridSize);
                    }
                }
            }
            else if (layout == TileMap.Layout.Hexagonal)
            {
                float outerRadius = spOuterRadius.floatValue;
                outerRadius = EditorGUILayout.FloatField(spOuterRadius.displayName, outerRadius);
                foreach (var tileMap in tileMaps)
                {
                    if (outerRadius != tileMap.OuterRadius)
                    {
                        tileMap.ResizeHexGrid(outerRadius);
                    }
                }

                TileMap.HexOrientation orientation = (TileMap.HexOrientation)spOrientation.enumValueIndex;
                orientation = (TileMap.HexOrientation)EditorGUILayout.EnumPopup(spOrientation.displayName, orientation);
                foreach (var tileMap in tileMaps)
                {
                    if (orientation != tileMap.Orientation)
                        tileMap.ResizeHexOrientation(orientation);
                }
            }
            else
            {
                float isoWidth = spIsoWidth.floatValue;
                isoWidth = EditorGUILayout.FloatField(spIsoWidth.displayName, isoWidth);
                float isoHeight = spIsoHeight.floatValue;
                isoHeight = EditorGUILayout.FloatField(spIsoHeight.displayName, isoHeight);

                bool isIsoSizeChanged = (isoWidth != spIsoWidth.floatValue || isoHeight != spIsoHeight.floatValue);
                if (isIsoSizeChanged)
                {
                    foreach (var tileMap in tileMaps)
                    {
                        tileMap.ResizeIso(isoWidth, isoHeight);
                    }
                }
                if (layout == TileMap.Layout.IsometricDiamondFreestyle)
                {
                    float xRotation = spXRotation.floatValue;
                    xRotation = EditorGUILayout.FloatField(spXRotation.displayName, xRotation);
                    float yRotation = spYRotation.floatValue;
                    yRotation = EditorGUILayout.FloatField(spYRotation.displayName, yRotation);
                    foreach (var tileMap in tileMaps)
                    {
                        if (xRotation != tileMap.XRotation || yRotation != tileMap.YRotation)
                        {
                            tileMap.ChangeFreestyleRotation(xRotation, yRotation);
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}