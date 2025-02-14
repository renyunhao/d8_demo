using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GameFramework
{
    public class BindableMonoBehaviour : MonoBehaviour
    {
        [ContextMenu("Bind Field")]
        void BindField()
        {
            Bind(this, false);
        }

        [ContextMenu("Bind Field OnlyActive")]
        void BindFieldOnlyActive()
        {
            Bind(this, true);
        }

        public static void Bind(MonoBehaviour target, bool onlyActive)
        {
            GameObjectHierachy goHierachy = new GameObjectHierachy(target, onlyActive);
            Type type = target.GetType();

            FieldInfo[] fieldList = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fieldList)
            {
                if (field.FieldType == typeof(float) ||
                    field.FieldType == typeof(int) ||
                    field.FieldType == typeof(bool) ||
                    field.FieldType == typeof(double) ||
                    field.FieldType == typeof(double) ||
                    field.FieldType == typeof(Vector2) ||
                    field.FieldType == typeof(Vector3) ||
                    field.FieldType == typeof(Vector4) ||
                    field.FieldType == typeof(Sprite) ||
                    field.FieldType == typeof(Enum) ||
                    field.FieldType == typeof(Quaternion) ||
                    field.FieldType == typeof(Rect) ||
                    field.FieldType == typeof(AnimationCurve) ||
                    field.FieldType == typeof(Action) ||
                    field.FieldType == typeof(string) ||
                    field.FieldType.IsArray)
                {
                    continue;
                }

                Transform go = goHierachy.GetChild(field.Name);

                if (go != null)
                {
                    if (field.FieldType == typeof(GameObject))
                    {
                        field.SetValue(target, go.gameObject);
                        continue;
                    }
                    Component component = null;
                    component = go.GetComponent(field.FieldType);
                    if (component != null)
                    {
                        field.SetValue(target, component);
                    }
                }
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(target.gameObject);
#endif
        }
    }

    public struct GameObjectHierachy
    {
        private Dictionary<string, Transform> children;

        public GameObjectHierachy(MonoBehaviour parent, bool onlyActive = true)
        {
            var childArray = parent.GetComponentsInChildren<Transform>(!onlyActive);
            children = new Dictionary<string, Transform>(childArray.Length);
            foreach (Transform child in childArray)
            {
                string lowerName = child.name.ToLower();
                if (!children.ContainsKey(lowerName))
                {
                    children[lowerName] = child;
                }
            }
        }

        public Transform GetChild(string name)
        {
            children.TryGetValue(name.ToLower(), out var child);
            return child;
        }
    }
}