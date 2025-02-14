using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 贝塞尔曲线工具类
    /// </summary>
    public static class BezierUtil
    {
        #region 二阶贝塞尔曲线

        /// <summary>
        /// 根据T值，计算贝塞尔曲线上面相对应的点
        /// </summary>
        /// <param name="t"></param>T值[0-1]
        /// <param name="p0"></param>起始点
        /// <param name="p1"></param>控制点1
        /// <param name="p2"></param>目标点
        /// <returns></returns>根据T值计算出来的贝赛尔曲线点
        public static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            return (1 - t) * (1 - t) * p0 + 2 * t * (1 - t) * p1 + t * t * p2;
        }

        /// <summary>
        /// 获取存储贝塞尔曲线点的数组
        /// </summary>
        /// <param name="p0"></param>起始点
        /// <param name="p1"></param>控制点
        /// <param name="p2"></param>目标点
        /// <param name="segmentNum"></param>采样点的数量
        /// <returns></returns>存储贝塞尔曲线点的数组
        public static Vector3[] GetBeizerList(Vector3 p0, Vector3 p1, Vector3 p2, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum];
            for (int i = 1; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateCubicBezierPoint(t, p0, p1, p2);

                path[i - 1] = pixel;
            }
            return path;
        }

        /// <summary>
        /// 计算贝塞尔曲线对应的切线斜率
        /// </summary>
        /// <returns></returns>
        public static Vector3 CalculateCubicBezuerAngle(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            Vector3 P = p0 * 2 * (1 - t) * (-1.0f);
            P += p1 * 2 * (1 - 2 * t);
            P += p2 * 2 * t;

            //返回单位向量
            return P;
        }

        #endregion

        #region 三阶贝塞尔曲线

        /// <summary>
        /// 根据T值，计算贝塞尔曲线上面相对应的点
        /// </summary>
        /// <param name="t"></param>T值[0-1]
        /// <param name="p0"></param>起始点
        /// <param name="p1"></param>控制点1
        /// <param name="p2"></param>控制点2
        /// <param name="p3"></param>目标点
        /// <returns></returns>根据T值计算出来的贝赛尔曲线点
        public static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (1 - t) * ((1 - t) * ((1 - t) * p0 + t * p1) + t * ((1 - t) * p1 + t * p2)) + t * ((1 - t) * ((1 - t) * p1 + t * p2) + t * ((1 - t) * p2 + t * p3));
        }

        /// <summary>
        /// 获取存储贝塞尔曲线点的数组
        /// </summary>
        /// <param name="startPoint"></param>起始点
        /// <param name="controlPoint"></param>控制点
        /// <param name="endPoint"></param>目标点
        /// <param name="segmentNum"></param>采样点的数量
        /// <returns></returns>存储贝塞尔曲线点的数组
        public static Vector3[] GetBeizerList(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int segmentNum)
        {
            Vector3[] path = new Vector3[segmentNum];
            for (int i = 1; i <= segmentNum; i++)
            {
                float t = i / (float)segmentNum;
                Vector3 pixel = CalculateCubicBezierPoint(t, p0, p1, p2, p3);

                path[i - 1] = pixel;
            }
            return path;
        }

        /// <summary>
        /// 计算贝塞尔曲线对应的切线斜率
        /// </summary>
        /// <returns></returns>
        public static Vector3 CalculateCubicBezuerAngle(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float uu = u * u;
            float tu = t * u;
            float tt = t * t;

            Vector3 P = p0 * 3 * uu * (-1.0f);
            P += p1 * 3 * (uu - 2 * tu);
            P += p2 * 3 * (2 * tu - tt);
            P += p3 * 3 * tt;

            //返回单位向量
            return P;
        }

        #endregion

        #region 列表连成三阶贝塞尔曲线
        public static List<Vector2> Generate3rdBezierCurvePoints(List<Vector2> controlPoints)
        {
            List<Vector2> bezierCurvePoints = new List<Vector2>();
            for (int i = 0; i < controlPoints.Count - 2; i++)
            {
                for (int j = 0; j <= 3; j++)
                {
                    Vector2 point = BezierCurvePoint(controlPoints, i, j / 3f);
                    bezierCurvePoints.Add(point);
                }
            }
            return bezierCurvePoints.Count == 0 ? controlPoints : bezierCurvePoints;
        }

        static Vector2 BezierCurvePoint(List<Vector2> controlPoints, int startIndex, float t)
        {
            Vector2 p0 = controlPoints[startIndex];
            Vector2 p1 = controlPoints[startIndex + 1];
            Vector2 p2 = controlPoints[startIndex + 2];

            float u = 1 - t;
            Vector2 p = u * u * p0; // (1-t)^2 * P0
            p += 2 * u * t * p1; // 2(1-t) * t * P1
            p += t * t * p2; // t^2 * P2

            return p;
        }
        #endregion

        //#region 不定次贝塞尔曲线
        //public static List<Vector2> GenerateBezierCurvePoints(List<Vector2> list)
        //{
        //    List<Vector2> bezierCurvePoints = new List<Vector2>();
        //    for (int i = 0; i <= list.Count; i++)
        //    {
        //        Vector2 point = BezierCurvePoint(i / (float)list.Count, list);
        //        bezierCurvePoints.Add(point);
        //    }
        //    return bezierCurvePoints;
        //}

        //static Vector2 BezierCurvePoint(float t, List<Vector2> list)
        //{
        //    Vector2 p = new Vector2();
        //    int n = list.Count - 1;
        //    for (int i = 0; i <= n; i++)
        //    {
        //        p += BinomialCoefficient(n, i) * Mathf.Pow(1 - t, n - i) * Mathf.Pow(t, i) * list[i];
        //    }
        //    return p;
        //}

        //static int BinomialCoefficient(int n, int k)
        //{
        //    int result = 1;
        //    if (k > n - k)
        //    {
        //        k = n - k;
        //    }
        //    for (int i = 0; i < k; i++)
        //    {
        //        result *= (n - i);
        //        result /= (i + 1);
        //    }
        //    return result;
        //}
        //#endregion
    }
}