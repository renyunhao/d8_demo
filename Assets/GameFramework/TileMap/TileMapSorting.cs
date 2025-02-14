using UnityEngine;

namespace GameFramework
{
    [RequireComponent(typeof(TileMap))]
    public class TileMapSorting : MonoBehaviour
    {
        private TileMap tileMap;

        public int sortingLayer;
        public int orderInLayer;
        /// <summary>
        /// 层级之间的间隔，当一个物体由多个元素构成时，需要拉开层级差
        /// 这样避免物体的某些部分的层次超过其前后物体层级
        /// </summary>
        public int orderDelta = 100;

        public void Awake()
        {
            tileMap = this.GetComponent<TileMap>();
            tileMap.OnSetGameObject += OnTileMapSetGameObject;
        }

        private void OnTileMapSetGameObject(int x, int y, GameObject go)
        {
            CheckSoringOrderTag(go);
            ReOrder(x, y, go);
        }

        public void CheckSoringOrderTag(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                SortingOrderTag tag = renderer.GetComponent<SortingOrderTag>();
                if (tag == null)
                {
                    if (renderer.GetComponent<SortingOrderTagImmune>() == null)
                    {
                        tag = renderer.gameObject.AddComponent<SortingOrderTag>();
                        tag.sortingOrder = renderer.sortingOrder;
                        tag.zOrder = renderer.transform.localPosition.z;
                        tag.sortingLayer = sortingLayer;
                    }
                }
            }
        }

        public int GetSortingOrderValue(float x, float y, GameObject go)
        {
            if (go != null)
            {
                SortingOrderTag tag = go.GetComponent<SortingOrderTag>();
                int sortingOrder = 0;
                if (tag != null)
                {
                    sortingOrder = tag.sortingOrder;
                }
                return sortingOrder + (int)((orderInLayer - y - x) * orderDelta);
            }
            else
            {
                int sortingOrder = (int)((orderInLayer - y - x) * orderDelta);
                return sortingOrder;
            }
        }

        public virtual void ReOrder(float x, float y, GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                SortingOrderTag tag = renderer.GetComponent<SortingOrderTag>();
                if (tag != null)
                {
                    int sortingOrder = tag.sortingOrder;
                    int sortingLayerID = tag.sortingLayer;
                    renderer.sortingOrder = sortingOrder + (int)((orderInLayer - y - x) * orderDelta);
                    renderer.sortingLayerID = sortingLayerID;

                    //0 - 13 - 16
                }
            }
        }
    }
}