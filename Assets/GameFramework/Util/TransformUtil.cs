using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public static class TransformUtil
    {
        /// <summary>
        /// 删除transform下的所有子对象
        /// </summary>
        public static void DestoryAllChildrenImmediate(this Transform transform)
        {
            if (transform != null)
            {
                while (transform.childCount > 0)
                {
                    UnityEngine.GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
                }
            }
        }

        public static Transform RecursiveFindChild(this Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                else
                {
                    if (child.name.Trim() == childName)
                    {
                        Debug.LogError("RecursiveFindChild 警告 " + child.name + "名称前后有空格！");
                        return child;
                    }
                    else
                    {
                        Transform found = RecursiveFindChild(child, childName);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }
            }
            return null;
        }

        public static List<Transform> RecursiveFindAllChild(this Transform parent, string childName, List<Transform> list = null)
        {
            if (list == null)
            {
                list = new List<Transform>();
            }
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    list.Add(child);
                }
                else
                {
                    if (child.name.Trim() == childName)
                    {
                        Debug.LogError("RecursiveFindChild 警告 " + child.name + "名称前后有空格！");
                        list.Add(child);
                    }
                    else
                    {
                        RecursiveFindAllChild(child, childName, list);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 世界坐标转Canvas坐标
        /// </summary>
        /// <param name="canvasTransform">Canvas的transform</param>
        /// <param name="camera">地图像机</param>
        /// <param name="position">世界物体坐标，CanvasOverLay模式下的世界坐标不适用</param>
        /// <returns></returns>
        public static Vector2 WorldToCanvasPosition(RectTransform canvasTransform, Camera camera, Vector3 position)
        {
            Vector2 temp = camera.WorldToViewportPoint(position);
            temp.x *= canvasTransform.sizeDelta.x;
            temp.y *= canvasTransform.sizeDelta.y;

            temp.x -= canvasTransform.sizeDelta.x * canvasTransform.pivot.x;
            temp.y -= canvasTransform.sizeDelta.y * canvasTransform.pivot.y;

            return temp;
        }

        public static Vector2 ScreenToCanvasPosition(RectTransform canvasTransform, Camera camera, Vector2 screenPos)
        {
            Vector2 temp = camera.ScreenToViewportPoint(screenPos);
            temp.x *= canvasTransform.sizeDelta.x;
            temp.y *= canvasTransform.sizeDelta.y;

            temp.x -= canvasTransform.sizeDelta.x * canvasTransform.pivot.x;
            temp.y -= canvasTransform.sizeDelta.y * canvasTransform.pivot.y;

            return temp;
        }

        /// <summary>
        /// Canvas坐标转世界坐标
        /// </summary>
        /// <param name="canvasTransform"></param>
        /// <param name="camera"></param>
        /// <param name="anchoredPosition"></param>
        /// <returns></returns>
        public static Vector3 CanvasToWorldPosition(RectTransform canvasTransform, Camera camera, Vector2 anchoredPosition)
        {
            Vector2 screenPoint = anchoredPosition;
            screenPoint.x += canvasTransform.sizeDelta.x * canvasTransform.pivot.x;
            screenPoint.y += canvasTransform.sizeDelta.y * canvasTransform.pivot.y;
            screenPoint.x /= canvasTransform.sizeDelta.x;
            screenPoint.y /= canvasTransform.sizeDelta.y;
            screenPoint.x *= Screen.width;
            screenPoint.y *= Screen.height;

            Vector3 worldPosition;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasTransform, screenPoint, camera, out worldPosition);
            return worldPosition;
        }
    }
}