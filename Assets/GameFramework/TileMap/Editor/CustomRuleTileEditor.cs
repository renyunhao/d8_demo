using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor
{
    [CustomEditor(typeof(CustomRuleTile), true)]
    [CanEditMultipleObjects]
    public class CustomRuleTileEditor : Editor
    {
        private const string s_XIconString = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABoSURBVDhPnY3BDcAgDAOZhS14dP1O0x2C/LBEgiNSHvfwyZabmV0jZRUpq2zi6f0DJwdcQOEdwwDLypF0zHLMa9+NQRxkQ+ACOT2STVw/q8eY1346ZlE54sYAhVhSDrjwFymrSFnD2gTZpls2OvFUHAAAAABJRU5ErkJggg==";
        private const string s_Arrow0 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPzZExDoQwDATzE4oU4QXXcgUFj+YxtETwgpMwXuFcwMFSRMVKKwzZcWzhiMg91jtg34XIntkre5EaT7yjjhI9pOD5Mw5k2X/DdUwFr3cQ7Pu23E/BiwXyWSOxrNqx+ewnsayam5OLBtbOGPUM/r93YZL4/dhpR/amwByGFBz170gNChA6w5bQQMqramBTgJ+Z3A58WuWejPCaHQAAAABJRU5ErkJggg==";
        private const string s_Arrow1 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPxYzBDYAgEATpxYcd+PVr0fZ2siZrjmMhFz6STIiDs8XMlpEyi5RkO/d66TcgJUB43JfNBqRkSEYDnYjhbKD5GIUkDqRDwoH3+NgTAw+bL/aoOP4DOgH+iwECEt+IlFmkzGHlAYKAWF9R8zUnAAAAAElFTkSuQmCC";
        private const string s_Arrow2 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAC0SURBVDhPjVE5EsIwDMxPKFKYF9CagoJH8xhaMskLmEGsjOSRkBzYmU2s9a58TUQUmCH1BWEHweuKP+D8tphrWcAHuIGrjPnPNY8X2+DzEWE+FzrdrkNyg2YGNNfRGlyOaZDJOxBrDhgOowaYW8UW0Vau5ZkFmXbbDr+CzOHKmLinAXMEePyZ9dZkZR+s5QX2O8DY3zZ/sgYcdDqeEVp8516o0QQV1qeMwg6C91toYoLoo+kNt/tpKQEVvFQAAAAASUVORK5CYII=";
        private const string s_Arrow3 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAB2SURBVDhPzY1LCoAwEEPnLi48gW5d6p31bH5SMhp0Cq0g+CCLxrzRPqMZ2pRqKG4IqzJc7JepTlbRZXYpWTg4RZE1XAso8VHFKNhQuTjKtZvHUNCEMogO4K3BhvMn9wP4EzoPZ3n0AGTW5fiBVzLAAYTP32C2Ay3agtu9V/9PAAAAAElFTkSuQmCC";
        private const string s_Arrow5 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPnY3BCYBADASvFx924NevRdvbyoLBmNuDJQMDGjNxAFhK1DyUQ9fvobCdO+j7+sOKj/uSB+xYHZAxl7IR1wNTXJeVcaAVU+614uWfCT9mVUhknMlxDokd15BYsQrJFHeUQ0+MB5ErsPi/6hO1AAAAAElFTkSuQmCC";
        private const string s_Arrow6 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACaSURBVDhPxZExEkAwEEVzE4UiTqClUDi0w2hlOIEZsV82xCZmQuPPfFn8t1mirLWf7S5flQOXjd64vCuEKWTKVt+6AayH3tIa7yLg6Qh2FcKFB72jBgJeziA1CMHzeaNHjkfwnAK86f3KUafU2ClHIJSzs/8HHLv09M3SaMCxS7ljw/IYJWzQABOQZ66x4h614ahTCL/WT7BSO51b5Z5hSx88AAAAAElFTkSuQmCC";
        private const string s_Arrow7 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABQSURBVDhPYxh8QNle/T8U/4MKEQdAmsz2eICx6W530gygr2aQBmSMphkZYxqErAEXxusKfAYQ7XyyNMIAsgEkaYQBkAFkaYQBsjXSGDAwAAD193z4luKPrAAAAABJRU5ErkJggg==";
        private const string s_Arrow8 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPxZE9DoAwCIW9iUOHegJXHRw8tIdx1egJTMSHAeMPaHSR5KVQ+KCkCRF91mdz4VDEWVzXTBgg5U1N5wahjHzXS3iFFVRxAygNVaZxJ6VHGIl2D6oUXP0ijlJuTp724FnID1Lq7uw2QM5+thoKth0N+GGyA7IA3+yM77Ag1e2zkey5gCdAg/h8csy+/89v7E+YkgUntOWeVt2SfAAAAABJRU5ErkJggg==";
        private const string s_MirrorX = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG1JREFUOE+lj9ENwCAIRB2IFdyRfRiuDSaXAF4MrR9P5eRhHGb2Gxp2oaEjIovTXSrAnPNx6hlgyCZ7o6omOdYOldGIZhAziEmOTSfigLV0RYAB9y9f/7kO8L3WUaQyhCgz0dmCL9CwCw172HgBeyG6oloC8fAAAAAASUVORK5CYII=";
        private const string s_MirrorY = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG9JREFUOE+djckNACEMAykoLdAjHbPyw1IOJ0L7mAejjFlm9hspyd77Kk+kBAjPOXcakJIh6QaKyOE0EB5dSPJAiUmOiL8PMVGxugsP/0OOib8vsY8yYwy6gRyC8CB5QIWgCMKBLgRSkikEUr5h6wOPWfMoCYILdgAAAABJRU5ErkJggg==";
        private const string s_Rotated = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAHdJREFUOE+djssNwCAMQxmIFdgx+2S4Vj4YxWlQgcOT8nuG5u5C732Sd3lfLlmPMR4QhXgrTQaimUlA3EtD+CJlBuQ7aUAUMjEAv9gWCQNEPhHJUkYfZ1kEpcxDzioRzGIlr0Qwi0r+Q5rTgM+AAVcygHgt7+HtBZs/2QVWP8ahAAAAAElFTkSuQmCC";
        private static Texture2D[] s_Arrows;
        public static Texture2D[] arrows
        {
            get
            {
                if (s_Arrows == null)
                {
                    s_Arrows = new Texture2D[10];
                    s_Arrows[0] = Base64ToTexture(s_Arrow0);
                    s_Arrows[1] = Base64ToTexture(s_Arrow1);
                    s_Arrows[2] = Base64ToTexture(s_Arrow2);
                    s_Arrows[3] = Base64ToTexture(s_Arrow3);
                    s_Arrows[5] = Base64ToTexture(s_Arrow5);
                    s_Arrows[6] = Base64ToTexture(s_Arrow6);
                    s_Arrows[7] = Base64ToTexture(s_Arrow7);
                    s_Arrows[8] = Base64ToTexture(s_Arrow8);
                    s_Arrows[9] = Base64ToTexture(s_XIconString);
                }
                return s_Arrows;
            }
        }

        private static Texture2D[] s_AutoTransforms;
        public static Texture2D[] autoTransforms
        {
            get
            {
                if (s_AutoTransforms == null)
                {
                    s_AutoTransforms = new Texture2D[3];
                    s_AutoTransforms[0] = Base64ToTexture(s_Rotated);
                    s_AutoTransforms[1] = Base64ToTexture(s_MirrorX);
                    s_AutoTransforms[2] = Base64ToTexture(s_MirrorY);
                }
                return s_AutoTransforms;
            }
        }

        internal const float k_DefaultElementHeight = 90f;
        internal const float k_PaddingBetweenRules = 26f;
        internal const float k_SingleLineHeight = 16f;
        internal const float k_LabelWidth = 120f;
        private CustomRuleTile tile { get { return (target as CustomRuleTile); } }
        private ReorderableList m_ReorderableList;
        private GUIStyle fontStyle = new GUIStyle();

        public void OnEnable()
        {
            SerializedProperty tileRules = serializedObject.FindProperty("m_TilingRules");
            m_ReorderableList = new ReorderableList(serializedObject, tileRules, true, true, true, true);
            m_ReorderableList.drawHeaderCallback = OnDrawHeader;
            m_ReorderableList.drawElementCallback = OnDrawElement;
            m_ReorderableList.elementHeightCallback = GetElementHeight;
            m_ReorderableList.onReorderCallback = ListUpdated;
            m_ReorderableList.onAddCallback = OnAddElement;
            fontStyle.fontSize = 10;
            fontStyle.normal.textColor = Color.gray;
            fontStyle.alignment = TextAnchor.MiddleCenter;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            CustomRuleTile.TiledType tiledType = (CustomRuleTile.TiledType)EditorGUILayout.EnumPopup("Tiled Type", tile.m_TiledType);
            if (tiledType != tile.m_TiledType)
            {
                tile.m_TiledType = tiledType;
                m_ReorderableList.DoLayoutList();
            }
            var baseFields = typeof(CustomRuleTile).GetFields().Select(field => field.Name);
            var fields = target.GetType().GetFields().Select(field => field.Name).Where(field => !baseFields.Contains(field));
            foreach (var field in fields)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field), true);
            if (m_ReorderableList != null && tile.m_TilingRules != null)
                m_ReorderableList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
        }

        private void OnAddElement(ReorderableList list)
        {
            CustomRuleTile.TilingRule rule = new CustomRuleTile.TilingRule();
            var oldRules = tile.m_TilingRules;
            tile.m_TilingRules = new CustomRuleTile.TilingRule[oldRules.Length + 1];
            oldRules.CopyTo(tile.m_TilingRules, 0);
        }

        private void ListUpdated(ReorderableList list)
        {
            SaveTile();
        }

        private float GetElementHeight(int index)
        {
            if (tile.m_TilingRules != null && tile.m_TilingRules.Length > 0)
            {
                if (tile.m_TilingRules[index].isCheckerboardStyle)
                {
                    return k_DefaultElementHeight + k_PaddingBetweenRules + 32;
                }
                else
                {
                    return k_DefaultElementHeight + k_PaddingBetweenRules;
                }
            }
            return k_DefaultElementHeight + k_PaddingBetweenRules;
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            CustomRuleTile.TilingRule rule = tile.m_TilingRules[index];
            rule.m_TiledType = tile.m_TiledType;
            float yPos = rect.yMin + 2f;
            float height = rect.height - k_PaddingBetweenRules;
            float matrixWidth = k_DefaultElementHeight;

            Rect inspectorRect = new Rect(rect.xMin, yPos, rect.width - matrixWidth * 2f - 20f, height);
            Rect matrixRect = new Rect(rect.xMax - matrixWidth * 2f - 10f, yPos, matrixWidth, k_DefaultElementHeight);
            Rect spriteRect = new Rect(rect.xMax - matrixWidth - 5f, yPos, matrixWidth, k_DefaultElementHeight);

            EditorGUI.BeginChangeCheck();
            RuleInspectorOnGUI(inspectorRect, rule);
            RuleMatrixOnGUI(tile, matrixRect, rule);
            SpriteOnGUI(spriteRect, rule);
            if (EditorGUI.EndChangeCheck())
                SaveTile();
        }

        private void OnDrawHeader(Rect rect)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), "Tiling Rules");
        }

        internal static void RuleInspectorOnGUI(Rect rect, CustomRuleTile.TilingRule tilingRule)
        {
            float y = rect.yMin;
            EditorGUI.BeginChangeCheck();
            if (tilingRule.m_TiledType == CustomRuleTile.TiledType.Image)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Image");
                tilingRule.image = (Image)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", tilingRule.image, typeof(Image), true);
            }
            else if (tilingRule.m_TiledType == CustomRuleTile.TiledType.SpriteRenderer)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "SpriteRenderer");
                tilingRule.spriteRenderer = (SpriteRenderer)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", tilingRule.spriteRenderer, typeof(SpriteRenderer), true);
            }
            else if (tilingRule.m_TiledType == CustomRuleTile.TiledType.GameObject)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "GameObject");
                var gameObject = tilingRule.gameObject;
                if (gameObject == null && tilingRule.image != null)
                {
                    gameObject = tilingRule.image.gameObject;
                }
                if (gameObject == null && tilingRule.spriteRenderer != null)
                {
                    gameObject = tilingRule.spriteRenderer.gameObject;
                }
                tilingRule.gameObject = (GameObject)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", gameObject, typeof(GameObject), true);
            }
            y += k_SingleLineHeight;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "存在方向");
            tilingRule.existValueStr = EditorGUI.TextField(new Rect(rect.xMin + k_LabelWidth * 2 + 10, y, rect.width - k_LabelWidth * 2 - 10, k_SingleLineHeight), "", tilingRule.existValueStr);
            tilingRule.existValue = EditorGUI.IntField(new Rect(rect.xMin + k_LabelWidth, y, k_LabelWidth, k_SingleLineHeight), "", tilingRule.existValue);
            y += k_SingleLineHeight;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "不存在方向");
            tilingRule.noExistValueStr = EditorGUI.TextField(new Rect(rect.xMin + k_LabelWidth * 2 + 10, y, rect.width - k_LabelWidth * 2 - 10, k_SingleLineHeight), "", tilingRule.noExistValueStr);
            tilingRule.noExistValue = EditorGUI.IntField(new Rect(rect.xMin + k_LabelWidth, y, k_LabelWidth, k_SingleLineHeight), "", tilingRule.noExistValue);
            y += k_SingleLineHeight;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "与周围Tile的判断方式");
            tilingRule.m_NeighbourTileType = (CustomRuleTile.TilingRule.NeighbourTileType)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_NeighbourTileType);
            y += k_SingleLineHeight;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "明暗开关");
            tilingRule.isCheckerboardStyle = EditorGUI.Toggle(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.isCheckerboardStyle);
            if (tilingRule.isCheckerboardStyle)
            {
                y += k_SingleLineHeight;
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "SpriteRendererDark");

                if (tilingRule.m_TiledType == CustomRuleTile.TiledType.Image)
                {
                    GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Image");
                    tilingRule.image_Dark = (Image)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", tilingRule.image_Dark, typeof(Image), true);
                }
                else if (tilingRule.m_TiledType == CustomRuleTile.TiledType.SpriteRenderer)
                {
                    GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "SpriteRenderer");
                    tilingRule.spriteRenderer_Dark = (SpriteRenderer)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", tilingRule.spriteRenderer_Dark, typeof(SpriteRenderer), true);
                }
                else if (tilingRule.m_TiledType == CustomRuleTile.TiledType.GameObject)
                {
                    GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "GameObject");
                    var gameObject = tilingRule.gameObject_Dark;
                    if (gameObject == null && tilingRule.image_Dark != null)
                    {
                        gameObject = tilingRule.image_Dark.gameObject;
                    }
                    if (gameObject == null && tilingRule.spriteRenderer_Dark != null)
                    {
                        gameObject = tilingRule.spriteRenderer_Dark.gameObject;
                    }
                    tilingRule.gameObject = (GameObject)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", gameObject, typeof(GameObject), true);
                }
                y += k_SingleLineHeight;
                GUI.Label(new Rect(rect.xMin + 20, y, k_LabelWidth, k_SingleLineHeight), "明暗");
                tilingRule.m_brightOrDarkRelative = EditorGUI.Toggle(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_brightOrDarkRelative);
                y += k_SingleLineHeight;
                GUI.Label(new Rect(rect.xMin + 20, y, k_LabelWidth, k_SingleLineHeight), "主体");
                tilingRule.is_Important = EditorGUI.Toggle(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.is_Important);
                y += k_SingleLineHeight;
            }
        }

        internal virtual void RuleMatrixOnGUI(CustomRuleTile tile, Rect rect, CustomRuleTile.TilingRule tilingRule)
        {
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.2f);
            int index = 0;
            float w = rect.width / 3f;
            float h = rect.height / 3f;

            for (int y = 0; y <= 3; y++)
            {
                float top = rect.yMin + y * h;
                Handles.DrawLine(new Vector3(rect.xMin, top), new Vector3(rect.xMax, top));
            }
            for (int x = 0; x <= 3; x++)
            {
                float left = rect.xMin + x * w;
                Handles.DrawLine(new Vector3(left, rect.yMin), new Vector3(left, rect.yMax));
            }
            Handles.color = Color.white;

            for (int y = 0; y <= 2; y++)
            {
                for (int x = 0; x <= 2; x++)
                {
                    Rect r = new Rect(rect.xMin + x * w, rect.yMin + y * h, w - 1, h - 1);
                    //if (x != 1 || y != 1)
                    //{
                    RuleOnGUI(r, y * 3 + x, tilingRule.neighbors[index]);
                    RuleNeighborUpdate(r, tilingRule, index);

                    index++;
                    //}
                    //else
                    //{
                    //    RuleTransformOnGUI(r, tilingRule.m_Relationship);
                    //    RuleTransformUpdate(r, tilingRule);
                    //}
                }
            }
            tilingRule.GetSelfValue();
        }

        internal virtual void RuleOnGUI(Rect rect, int arrowIndex, int neighbor)
        {
            if (arrowIndex == 0)
            {
                GUI.Label(rect, "128", fontStyle);
            }
            else if (arrowIndex == 1)
            {
                GUI.Label(rect, "8", fontStyle);
            }
            else if (arrowIndex == 2)
            {
                GUI.Label(rect, "64", fontStyle);
            }
            else if (arrowIndex == 3)
            {
                GUI.Label(rect, "16", fontStyle);
            }
            else if (arrowIndex == 4)
            {
                GUI.Label(rect, "1", fontStyle);
            }
            else if (arrowIndex == 5)
            {
                GUI.Label(rect, "4", fontStyle);
            }
            else if (arrowIndex == 6)
            {
                GUI.Label(rect, "256", fontStyle);
            }
            else if (arrowIndex == 7)
            {
                GUI.Label(rect, "2", fontStyle);
            }
            else if (arrowIndex == 8)
            {
                GUI.Label(rect, "32", fontStyle);
            }

            switch (neighbor)
            {
                case CustomRuleTile.TilingRule.Neighbor.DontCare:
                    break;
                case CustomRuleTile.TilingRule.Neighbor.This:
                    if (arrowIndex == 4)
                    {
                        GUI.DrawTexture(rect, Base64ToTexture(s_MirrorX));
                    }
                    else
                    {
                        GUI.DrawTexture(rect, arrows[arrowIndex]);
                    }
                    break;
                case CustomRuleTile.TilingRule.Neighbor.NotThis:
                    GUI.DrawTexture(rect, arrows[9]);
                    break;
                default:
                    var style = new GUIStyle();
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 10;
                    GUI.Label(rect, neighbor.ToString(), style);
                    break;
            }
            var allConsts = tile.m_NeighborType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            foreach (var c in allConsts)
            {
                if ((int)c.GetValue(null) == neighbor)
                {
                    GUI.Label(rect, new GUIContent("", c.Name));
                    break;
                }
            }
        }

        internal static void SpriteOnGUI(Rect rect, CustomRuleTile.TilingRule tilingRule)
        {
            if (tilingRule.m_TiledType == CustomRuleTile.TiledType.Image)
            {
                EditorGUI.ObjectField(new Rect(rect.xMax - rect.height, rect.yMin, rect.height, rect.height), tilingRule.image.sprite, typeof(Sprite), false);
            }
            else if (tilingRule.m_TiledType == CustomRuleTile.TiledType.Image)
            {
                EditorGUI.ObjectField(new Rect(rect.xMax - rect.height, rect.yMin, rect.height, rect.height), tilingRule.spriteRenderer.sprite, typeof(Sprite), false);
            }
            else if (tilingRule.m_TiledType == CustomRuleTile.TiledType.GameObject)
            {
                EditorGUI.ObjectField(new Rect(rect.xMax - rect.height, rect.yMin, rect.height, rect.height), AssetPreview.GetAssetPreview(tilingRule.gameObject), typeof(Texture2D), false);
            }
        }

        internal void RuleNeighborUpdate(Rect rect, CustomRuleTile.TilingRule tilingRule, int index)
        {
            if (Event.current.type == EventType.MouseDown && ContainsMousePosition(rect))
            {
                var allConsts = tile.m_NeighborType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                var neighbors = allConsts.Select(c => (int)c.GetValue(null)).ToList();
                neighbors.Sort();

                int oldIndex = neighbors.IndexOf(tilingRule.neighbors[index]);
                int newIndex = (int)Mathf.Repeat(oldIndex + GetMouseChange(), neighbors.Count);
                tilingRule.neighbors[index] = neighbors[newIndex];
                GUI.changed = true;
                Event.current.Use();
            }
        }

        internal virtual bool ContainsMousePosition(Rect rect)
        {
            return rect.Contains(Event.current.mousePosition);
        }

        private static int GetMouseChange()
        {
            return Event.current.button == 1 ? -1 : 1;
        }

        private static Texture2D Base64ToTexture(string base64)
        {
            Texture2D t = new Texture2D(1, 1);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.LoadImage(System.Convert.FromBase64String(base64));
            return t;
        }

        //internal virtual void RuleTransformOnGUI(Rect rect, CustomRuleTile.TilingRule.Relationship relationship)
        //{
        //    //switch (relationship)
        //    //{
        //    //    case CustomRuleTile.TilingRule.Relationship.And:
        //    //        GUI.DrawTexture(rect, autoTransforms[0]);
        //    //        break;
        //    //    case CustomRuleTile.TilingRule.Relationship.Or:
        //    //        GUI.DrawTexture(rect, autoTransforms[1]);
        //    //        break;
        //    //}
        //}

        internal void RuleTransformUpdate(Rect rect, CustomRuleTile.TilingRule tilingRule)
        {
            if (Event.current.type == EventType.MouseDown && ContainsMousePosition(rect))
            {
                //tilingRule.m_Relationship = (CustomRuleTile.TilingRule.Relationship)(int)Mathf.Repeat((int)tilingRule.m_Relationship + GetMouseChange(), 4);
                GUI.changed = true;
                Event.current.Use();
            }
        }
        private void SaveTile()
        {
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }
    }
}
