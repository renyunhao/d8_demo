using UnityEngine;
using UnityEngine.Rendering;

namespace GameFramework
{
    public class WaypointsComponent : MonoBehaviour
    {
        public Vector3[] points;

        private Vector3[] worldWaypoints;

#if UNITY_EDITOR
        public bool showIndexes;
#endif

        public void AddPoint(Vector3 newPoint)
        {
            var newPoints = new Vector3[points.Length + 1];
            points.CopyTo(newPoints, 0);
            newPoints[points.Length] = newPoint;
            points = newPoints;
        }

        public Vector3 GetLocalWaypoint(int index)
        {
            return points[index];
        }

        public Vector3[] GetLocalWaypoints()
        {
            return points;
        }

        public Vector3[] GetLocalWaypoints(int from, int to)
        {
            from = Mathf.Clamp(from, 0, points.Length - 1);
            to = Mathf.Clamp(to, 0, points.Length - 1);
            if (from >= to)
            {
                return null;
            }

            var path = new Vector3[to - from + 1];
            for (int index = 0; index < path.Length; index++)
            {
                path[index] = points[from + index];
            }
            return path;
        }

        public Vector3[] GetWorldWaypoints()
        {
            if (worldWaypoints == null || worldWaypoints.Length != points.Length)
            {
                worldWaypoints = new Vector3[points.Length];
            }

            points.CopyTo(worldWaypoints, 0);

            for (int i = 0; i < worldWaypoints.Length; i++)
            {
                worldWaypoints[i] = this.transform.TransformPoint(worldWaypoints[i]);
            }

            return worldWaypoints;
        }
    }
}
