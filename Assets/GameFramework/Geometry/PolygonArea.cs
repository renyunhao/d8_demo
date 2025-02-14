using System.Collections.Generic;
using TriangleNet.Topology;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 用点构成直线围成的多边形区域
    /// </summary>
    [System.Serializable]
    public class PolygonArea
    {
        public const int MIN_POINT_COUNT = 3;

        public List<Vector2> vertices;

        public List<Triangle> triangles;

        private bool isDirty;

        public PolygonArea()
        {
            vertices = new List<Vector2>(MIN_POINT_COUNT);
            isDirty = true;
        }

        public PolygonArea(IEnumerable<Vector2> points)
        {
            vertices = new List<Vector2>(points);
            if (vertices.Count < MIN_POINT_COUNT)
            {
                throw new System.Exception("Area need at least 3 points");
            }
            GenerateTriangle();
            isDirty = false;
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        public void GenerateTriangle()
        {
            triangles = new List<Triangle>(Triangulation.Triangulate(vertices));
        }

        public Vector2 GetRandomPoint()
        {
            if (isDirty)
            {
                GenerateTriangle();
                isDirty = false;
            }
            int triangleIndex = Random.Range(0, triangles.Count);
            Triangle triangle = triangles[triangleIndex];
            return RandomWithinTriangle(triangle);
        }

        private Vector2 RandomWithinTriangle(Triangle t)
        {
            var r1 = Mathf.Sqrt(Random.Range(0f, 1f));
            var r2 = Random.Range(0f, 1f);
            var m1 = 1 - r1;
            var m2 = r1 * (1 - r2);
            var m3 = r2 * r1;

            var p1 = t.GetVertex(0).ToVector2();
            var p2 = t.GetVertex(1).ToVector2();
            var p3 = t.GetVertex(2).ToVector2();
            return (m1 * p1) + (m2 * p2) + (m3 * p3);
        }
    }
}