using UnityEngine;

namespace GameFramework
{
    public class SortingOrderTag : MonoBehaviour
    {
        public int sortingOrder;
        public float zOrder;
        public int sortingLayer;
        public string objectName;

        private void Awake()
        {
            objectName = this.name;
        }
    }
}