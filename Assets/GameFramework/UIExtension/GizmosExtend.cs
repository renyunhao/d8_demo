using UnityEngine;

namespace GameFramework
{
    public static class GizmosExtend
    {
        public static void DrawPolyLine(Vector3[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                int j = i + 1;
                Gizmos.DrawLine(points[i], points[j]);
            }
        }
    }
}
