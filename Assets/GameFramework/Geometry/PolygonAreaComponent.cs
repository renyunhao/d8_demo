using UnityEngine;
using System.Collections.Generic;

namespace GameFramework
{
    public class PolygonAreaComponent : MonoBehaviour
    {
        [OnChangedCall("OnSerializedPropertyChange")]
        public PolygonArea area = new PolygonArea(new List<Vector2>() { new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0)});

        private List<GameObject> testRandomObjs = new List<GameObject>(100);

        public void Awake()
        {
            area.MarkDirty();
        }

        public void Reset()
        {
            testRandomObjs.Clear();
        }

        public Vector3 GetRandomPoint()
        {
            return (Vector3)area.GetRandomPoint() + this.transform.position;
        }

        public void TestRandomPoint(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var p = area.GetRandomPoint();
                if (i < testRandomObjs.Count)
                {
                    var sphere = testRandomObjs[i];
                    sphere.transform.position = p + (Vector2)this.transform.position;
                }
                else
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.hideFlags = HideFlags.HideAndDontSave;
                    sphere.transform.position = p + (Vector2)this.transform.position;
                    sphere.transform.localScale = Vector3.one * 0.02f;
                    testRandomObjs.Add(sphere);
                }
            }

            while (testRandomObjs.Count > count)
            {
                var obj = testRandomObjs[testRandomObjs.Count - 1];
                if (obj != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(obj);
                    }
                    else
                    {
                        DestroyImmediate(obj);
                    }
                }
                testRandomObjs.RemoveAt(testRandomObjs.Count - 1);
            }
        }

        public void ClearTestRandomPoints()
        {
            foreach (var obj in testRandomObjs)
            {
                if (obj == null)
                {
                    continue;
                }
                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }
            testRandomObjs.Clear();
        }

        public void OnSerializedPropertyChange()
        {
            area.MarkDirty();
        }
    }
}
