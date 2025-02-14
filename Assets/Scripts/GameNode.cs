using GameFramework;
using UnityEngine;

public static class GameNode
{
    private static Transform poolRoot;
    public static Transform PoolRoot
    {
        get
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject("PoolRoot").transform;
            }
            return poolRoot;
        }
    }

    private static Camera mapCamera;
    public static Camera MapCamera
    {
        get
        {
            if (mapCamera == null)
            {
                mapCamera = GameObject.Find("MapCamera").GetComponent<Camera>();
            }
            return mapCamera;
        }
    }

    private static Camera uiCamera;
    public static Camera UICamera
    {
        get
        {
            if (uiCamera == null)
            {
                uiCamera = GameObject.Find("UICamera").GetComponent<Camera>();
            }
            return uiCamera;
        }
    }
}
