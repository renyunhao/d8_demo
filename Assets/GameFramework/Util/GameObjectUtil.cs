using UnityEngine;

namespace GameFramework
{
    static public class GameObjectUtil
    {
        static public T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            if (null == go)
            {
                return null;
            }
            T t = go.GetComponent<T>();
            if (null == t)
            {
                t = go.AddComponent<T>();
            }
            return t;
        }

        static public T GetOrAddComponent<T, U>(this U componet) where T : Component where U : Component
        {
            if (null == componet)
            {
                return null;
            }
            T t = componet.GetComponent<T>();
            if (null == t)
            {
                t = componet.gameObject.AddComponent<T>();
            }
            return t;
        }

        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            go.layer = layer;

            foreach (Transform child in go.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        public static void SetLayerRecursively(this GameObject go, string layerName)
        {
            int layerMask = LayerMask.NameToLayer(layerName);
            go.layer = layerMask;

            foreach (Transform child in go.transform)
            {
                child.gameObject.SetLayerRecursively(layerMask);
            }
        }
    }
}