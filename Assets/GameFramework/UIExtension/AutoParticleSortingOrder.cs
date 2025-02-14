using UnityEngine;

namespace GameFramework
{
    public class AutoParticleSortingOrder : MonoBehaviour
    {
        public int sortingOrderOffset = 1;

        private ParticleSystemRenderer[] prrs;
        private int[] orderOffset;

        private void Awake()
        {
            prrs = this.GetComponentsInChildren<ParticleSystemRenderer>(true);
            int minOrder = 0;
            foreach (var prr in prrs)
            {
                minOrder = Mathf.Min(minOrder, prr.sortingOrder);
            }
            orderOffset = new int[prrs.Length];
            for (int i = 0; i < prrs.Length; i++)
            {
                orderOffset[i] = prrs[i].sortingOrder - minOrder;
            }
        }

        private void OnEnable()
        {
            Canvas canvas = this.GetComponentInParent<Canvas>();
            for (int i = 0; i < prrs.Length; i++)
            {
                prrs[i].sortingLayerID = canvas.sortingLayerID;
                prrs[i].sortingOrder = canvas.sortingOrder + sortingOrderOffset + orderOffset[i];
            }
        }
    }
}