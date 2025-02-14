using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ParticleSystemUtil
{
    public static void ChangeSortingLayerAndOrder(this GameObject gameObject, int sortingLayerID, int sortingOrder)
    {
        var list = gameObject.GetComponentsInChildren<ParticleSystemRenderer>(true);

        if (list.Length > 0)
        {
            int baseSortingOrder = list[0].sortingOrder;

            foreach (var pr in list)
            {
                int offset = pr.sortingOrder - baseSortingOrder;
                pr.sortingLayerID = sortingLayerID;
                pr.sortingOrder = sortingOrder + offset;
            }
        }
    }
}
