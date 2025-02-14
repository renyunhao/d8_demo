using System.Collections;
using System.Collections.Generic;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using UnityEngine;

namespace GameFramework
{
    public static class Triangulation
    {
        public static Vector2 ToVector2(this Vertex vertex)
        {
            return new Vector2((float)vertex.x, (float)vertex.y);
        }

        public static ICollection<Triangle> Triangulate(List<Vector2> points)
        {
            Polygon poly = new Polygon();

            for (int i = 0; i < points.Count; i++)
            {
                poly.Add(new Vertex(points[i].x, points[i].y));

                if (i == points.Count - 1)
                {
                    poly.Add(new Segment(new Vertex(points[i].x, points[i].y), new Vertex(points[0].x, points[0].y)));
                }
                else
                {
                    poly.Add(new Segment(new Vertex(points[i].x, points[i].y), new Vertex(points[i + 1].x, points[i + 1].y)));
                }
            }

            var mesh = poly.Triangulate();
            return mesh.Triangles;
        }

        public static bool Triangulate(List<Vector2> points, List<List<Vector2>> holes, out List<int> outIndices, out List<Vector3> outVertices)
        {
            outVertices = new List<Vector3>();
            outIndices = new List<int>();
            Polygon poly = new Polygon();

            for (int i = 0; i < points.Count; i++)
            {
                poly.Add(new Vertex(points[i].x, points[i].y));

                if (i == points.Count - 1)
                {
                    poly.Add(new Segment(new Vertex(points[i].x, points[i].y), new Vertex(points[0].x, points[0].y)));
                }
                else
                {
                    poly.Add(new Segment(new Vertex(points[i].x, points[i].y), new Vertex(points[i + 1].x, points[i + 1].y)));
                }
            }

            // Holes
            for (int i = 0; i < holes.Count; i++)
            {
                List<Vertex> vertices = new List<Vertex>();
                for (int j = 0; j < holes[i].Count; j++)
                {
                    vertices.Add(new Vertex(holes[i][j].x, holes[i][j].y));
                }
                poly.Add(new Contour(vertices), true);
            }

            var mesh = poly.Triangulate();

            foreach (ITriangle t in mesh.Triangles)
            {
                for (int j = 2; j >= 0; j--)
                {
                    bool found = false;
                    for (int k = 0; k < outVertices.Count; k++)
                    {
                        if ((outVertices[k].x == t.GetVertex(j).X) && (outVertices[k].z == t.GetVertex(j).Y))
                        {
                            outIndices.Add(k);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        outVertices.Add(new Vector3((float)t.GetVertex(j).X, 0, (float)t.GetVertex(j).Y));
                        outIndices.Add(outVertices.Count - 1);
                    }
                }
            }
            return true;
        }
    }
}